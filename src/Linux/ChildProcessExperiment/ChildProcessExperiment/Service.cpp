// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Service.hpp"
#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "SignalHandler.hpp"
#include "Subchannel.hpp"
#include "UniqueResource.hpp"
#include "Wrappers.hpp"
#include "WriteBuffer.hpp"
#include <algorithm>
#include <array>
#include <cassert>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <memory>
#include <poll.h>
#include <signal.h>
#include <sys/wait.h>
#include <unistd.h>

static_assert(sizeof(pid_t) == sizeof(int32_t));

namespace
{
    enum
    {
        PollIndexSignalData = 0,
        PollIndexMainChannel = 1,
    };

    std::unique_ptr<AncillaryDataSocket> g_MainChannel;
} // namespace

void* ServiceThreadFunc(void* arg);
int ServiceMain(UniqueFd sockFd);
bool HandleSignalDataInput();
bool HandleMainChannelInput();
bool HandleMainChannelOutput();

pthread_t StartService(ServiceArgs* args)
{
    auto maybeServiceThreadId = CreateThreadWithMyDefault(ServiceThreadFunc, args, 0);
    if (!maybeServiceThreadId)
    {
        FatalErrorAbort(errno, "pthread_create");
    }

    return *maybeServiceThreadId;
}

void* ServiceThreadFunc(void* arg)
{
    const auto pArgs = reinterpret_cast<ServiceArgs*>(arg);
    pArgs->ExitCode = ServiceMain(std::move(pArgs->ControlSocket));
    std::printf("service: closing\n");
    return nullptr;
}

int ServiceMain(UniqueFd sockFd)
{
    g_MainChannel = std::make_unique<AncillaryDataSocket>(std::move(sockFd));

    SetupSignalHandlers();

    // Main service loop
    pollfd fds[2]{};
    fds[PollIndexSignalData].fd = g_SignalDataPipeReadEnd;
    fds[PollIndexMainChannel].fd = g_MainChannel->GetFd();

    while (true)
    {
        fds[PollIndexSignalData].events = POLLIN;
        fds[PollIndexMainChannel].events = POLLIN | (g_MainChannel->HasPendingData() ? POLLOUT : 0);

        int count = poll_restarting(fds, 2, -1);
        if (count == -1)
        {
            FatalErrorAbort(errno, "poll");
        }

        if (fds[PollIndexSignalData].revents & POLLIN)
        {
            if (!HandleSignalDataInput())
            {
                return 1;
            }
        }

        if (fds[PollIndexMainChannel].revents & POLLIN)
        {
            if (!HandleMainChannelInput())
            {
                return 1;
            }
        }

        if (fds[PollIndexMainChannel].revents & POLLOUT)
        {
            if (!HandleMainChannelOutput())
            {
                return 1;
            }
        }

        if ((fds[PollIndexMainChannel].revents & (POLLHUP | POLLIN)) == POLLHUP)
        {
            // Connection closed.
            return 1;
        }
    }
}

bool HandleSignalDataInput()
{
    int signum;
    ssize_t readBytes;

    readBytes = ReadExactBytes(g_SignalDataPipeReadEnd, &signum, sizeof(int));
    if (readBytes == -1)
    {
        FatalErrorAbort(errno, "read");
    }

    switch (signum)
    {
    case SIGINT:
    case SIGQUIT:
        return false;

    case SIGCHLD:
    {
        // FIXME: This sends exit statuses of children that failed to execve.
        pid_t pid;
        if (!ReadExactBytes(g_SignalDataPipeReadEnd, &pid, sizeof(pid)))
        {
            FatalErrorAbort(errno, "read");
        }

        siginfo_t siginfo{};
        assert(siginfo.si_pid == 0);
        int ret = waitid(P_PID, pid, &siginfo, WEXITED | WNOHANG);
        if (ret < 0)
        {
            FatalErrorAbort(errno, "waitpid");
        }

        if (siginfo.si_pid == 0)
        {
            std::fprintf(stderr, "fatal error: logic error: attempted to waitid for a child that has not exited yet.\n");
            std::abort();
        }

        ChildExitNotification data{};
        data.Token = 0; // TODO: Also send the token.
        data.ProcessID = siginfo.si_pid;
        data.Code = siginfo.si_code;
        data.Status = siginfo.si_status;
        if (!g_MainChannel->Send(&data, sizeof(data), true))
        {
            return false;
        }
    }

    default:
        // Ignored
        break;
    }

    return true;
}

bool HandleMainChannelInput()
{
    std::byte dummy;
    const ssize_t readBytes = g_MainChannel->Recv(&dummy, 1, false);
    if (readBytes == -1)
    {
        if (IsConnectionClosedError(errno))
        {
            return false;
        }

        FatalErrorAbort(errno, "recvmsg");
    }
    else if (readBytes == 0)
    {
        // Connection closed.
        return false;
    }

    auto maybeSubchannelFd = g_MainChannel->PopReceivedFd();
    if (!maybeSubchannelFd)
    {
        std::fprintf(stderr, "The counterpart sent a subchannel creation request but dit not sent any fd.\n");
        return false;
    }

    StartSubchannelHandler(std::move(*maybeSubchannelFd));
    return true;
}

bool HandleMainChannelOutput()
{
    if (!g_MainChannel->Flush())
    {
        return false;
    }

    return true;
}
