// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "Wrappers.hpp"
#include <array>
#include <cassert>
#include <cerrno>
#include <cstddef>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <memory>
#include <optional>
#include <sys/socket.h>
#include <unistd.h>
#include <utility>

namespace
{
    struct CmsgFds
    {
        static const constexpr std::size_t BufferSize = CMSG_SPACE(sizeof(int) * AncillaryDataSocket::MaxFdsPerCall);
        alignas(cmsghdr) char Buffer[BufferSize];
    };

    constexpr int MakeSockFlags(bool nonblocking) noexcept
    {
        return (nonblocking ? MSG_DONTWAIT : 0) | MSG_NOSIGNAL;
    }

    void EnqueueRemainingBytes(WriteBuffer& b, const void* buf, std::size_t len, ssize_t bytesSent, int err)
    {
        if (bytesSent > 0)
        {
            auto const positiveBytesSent = static_cast<size_t>(bytesSent);
            if (positiveBytesSent < len)
            {
                b.Enqueue(static_cast<const std::byte*>(buf) + positiveBytesSent, len - positiveBytesSent);
            }
        }
        else if (bytesSent == 0)
        {
            // Connection closed.
        }
        else
        {
            if (IsWouldBlockError(err))
            {
                b.Enqueue(static_cast<const std::byte*>(buf), len);
            }
        }
    }
} // namespace

AncillaryDataSocket::AncillaryDataSocket(UniqueFd&& sockFd) noexcept
    : fd_(std::move(sockFd))
{
}

AncillaryDataSocket::AncillaryDataSocket(int sockFd) noexcept
    : AncillaryDataSocket(UniqueFd(sockFd))
{
}

bool AncillaryDataSocket::Send(const void* buf, std::size_t len, bool nonblocking) noexcept
{
    ssize_t bytesSent = send_restarting(fd_.Get(), buf, len, MakeSockFlags(nonblocking));
    int err = errno;
    EnqueueRemainingBytes(sendBuffer_, buf, len, bytesSent, err);
    errno = err;

    if (bytesSent > 0)
    {
        return true;
    }
    else if (bytesSent == 0)
    {
        // Connection closed.
        return false;
    }
    else
    {
        if (IsWouldBlockError(err))
        {
            return true;
        }
        else if (IsConnectionClosedError(err))
        {
            return false;
        }
        else
        {
            FatalErrorAbort(err, "send");
        }
    }
}

bool AncillaryDataSocket::SendExactBytes(const void* buf, std::size_t len) noexcept
{
    // NOTE: SendExactBytes is by definition a blocking operation.
    if (!Flush())
    {
        return false;
    }

    auto f = [this](const void* p, std::size_t partialLen) { return send_restarting(fd_.Get(), p, partialLen, MakeSockFlags(false)); };
    if (!WriteExactBytes(f, buf, len))
    {
        if (IsConnectionClosedError(errno))
        {
            return false;
        }
        else
        {
            FatalErrorAbort(errno, "send");
        }
    }

    return true;
}

bool AncillaryDataSocket::SendExactBytesWithFd(const void* buf, std::size_t len, const int* fds, std::size_t fdCount) noexcept
{
    // NOTE: SendExactBytes is by definition a blocking operation.
    if (!Flush())
    {
        return false;
    }

    // Make sure to send fds only once.
    ssize_t bytesSent = SendWithFdImpl(buf, len, fds, fdCount, false);
    if (bytesSent == 0)
    {
        // Connection closed.
        return false;
    }
    else if (bytesSent <= -1)
    {
        int err = errno;
        if (IsConnectionClosedError(err))
        {
            return false;
        }
        else
        {
            FatalErrorAbort(err, "sendmsg");
        }
    }

    // Send out remaining bytes.
    std::size_t positiveBytesSent = static_cast<std::size_t>(bytesSent);
    if (positiveBytesSent >= len)
    {
        return true;
    }
    else
    {
        return SendExactBytes(static_cast<const std::byte*>(buf) + positiveBytesSent, len - positiveBytesSent);
    }
}

ssize_t AncillaryDataSocket::SendWithFd(const void* buf, std::size_t len, const int* fds, std::size_t fdCount, bool nonblocking) noexcept
{
    ssize_t bytesSent = SendWithFdImpl(buf, len, fds, fdCount, nonblocking);
    int err = errno;
    EnqueueRemainingBytes(sendBuffer_, buf, len, bytesSent, errno);
    errno = err;
    return bytesSent;
}

ssize_t AncillaryDataSocket::SendWithFdImpl(const void* buf, std::size_t len, const int* fds, std::size_t fdCount, bool nonblocking) noexcept
{
    if (fds == nullptr || fdCount == 0)
    {
        return Send(buf, len, nonblocking);
    }

    if (fdCount > MaxFdsPerCall)
    {
        errno = EINVAL;
        return -1;
    }

    iovec iov;
    msghdr msg;
    CmsgFds cmsgFds;

    iov.iov_base = const_cast<void*>(buf);
    iov.iov_len = len;
    msg.msg_name = NULL;
    msg.msg_namelen = 0;
    msg.msg_iov = &iov;
    msg.msg_iovlen = 1;
    msg.msg_control = cmsgFds.Buffer;
    msg.msg_controllen = CmsgFds::BufferSize;
    msg.msg_flags = 0;

    struct cmsghdr* pcmsghdr = CMSG_FIRSTHDR(&msg);
    pcmsghdr->cmsg_len = CMSG_LEN(sizeof(int) * fdCount);
    pcmsghdr->cmsg_level = SOL_SOCKET;
    pcmsghdr->cmsg_type = SCM_RIGHTS;
    std::memcpy(CMSG_DATA(pcmsghdr), fds, sizeof(int) * fdCount);

    return sendmsg_restarting(fd_.Get(), &msg, 0);
}

bool AncillaryDataSocket::Flush() noexcept
{
    while (sendBuffer_.HasPendingData())
    {
        std::byte* p;
        std::size_t len;
        std::tie(p, len) = sendBuffer_.GetPendingData();

        const ssize_t bytesSent = send_restarting(fd_.Get(), p, len, false);
        if (bytesSent == 0)
        {
            // Connection closed.
            return false;
        }
        else if (bytesSent <= -1)
        {
            if (IsConnectionClosedError(errno))
            {
                return false;
            }
            else
            {
                FatalErrorAbort(errno, "send");
            }
        }

        sendBuffer_.Dequeue(static_cast<std::size_t>(bytesSent));
    }

    return true;
}

bool AncillaryDataSocket::RecvExactBytes(void* buf, std::size_t len) noexcept
{
    // NOTE: RecvExactBytes is by definition a blocking operation.
    auto f = [this](void* p, std::size_t partialLen) { return Recv(p, partialLen, false); };
    return ReadExactBytes(f, buf, len);
}

ssize_t AncillaryDataSocket::Recv(void* buf, std::size_t len, bool nonblocking) noexcept
{
    iovec iov;
    msghdr msg;
    CmsgFds cmsgFds;

    iov.iov_base = buf;
    iov.iov_len = len;
    msg.msg_name = NULL;
    msg.msg_namelen = 0;
    msg.msg_iov = &iov;
    msg.msg_iovlen = 1;
    msg.msg_control = cmsgFds.Buffer;
    msg.msg_controllen = CmsgFds::BufferSize;
    msg.msg_flags = 0;

    const ssize_t receivedBytes = recvmsg_restarting(fd_.Get(), &msg, MakeSockFlags(nonblocking) | MSG_CMSG_CLOEXEC);
    if (receivedBytes == -1)
    {
        return -1;
    }

    // Store received fds.
    bool shouldShutdown = false;

    for (cmsghdr* pcmsghdr = CMSG_FIRSTHDR(&msg); pcmsghdr != nullptr; pcmsghdr = CMSG_NXTHDR(&msg, pcmsghdr))
    {
        if (pcmsghdr->cmsg_level != SOL_SOCKET || pcmsghdr->cmsg_type != SCM_RIGHTS)
        {
            // Logic error: The counterpart has a bug or we are connected with an untrusted counterpart.
            std::fprintf(stderr, "Received unknown cmsg (cmsg_level: %d, cmsg_type: %d). Shutting down the connection.\n", pcmsghdr->cmsg_level, pcmsghdr->cmsg_type);
            shouldShutdown = true;
            // Continue to read so that we will not leak received fds.
            continue;
        }

        unsigned char* const cmsgdata = CMSG_DATA(pcmsghdr);
        const std::ptrdiff_t cmsgdataLen = pcmsghdr->cmsg_len - (cmsgdata - reinterpret_cast<unsigned char*>(pcmsghdr));
        const std::size_t fdCount = cmsgdataLen / sizeof(int);
        for (std::size_t i = 0; i < fdCount; i++)
        {
            int receivedFd;
            std::memcpy(&receivedFd, cmsgdata + sizeof(int) * i, sizeof(int));
            receivedFds_.push(UniqueFd(receivedFd));
        }
    }

    if (shouldShutdown)
    {
        shutdown(fd_.Get(), SHUT_RDWR);
        errno = ECONNRESET;
        return -1;
    }

    return receivedBytes;
}
