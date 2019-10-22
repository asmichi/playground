// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Client.hpp"
#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "MiscHelpers.hpp"
#include "Request.hpp"
#include "Service.hpp"
#include <algorithm>
#include <cstdio>
#include <cstring>
#include <fcntl.h>
#include <memory>
#include <pthread.h>
#include <stdexcept>
#include <unistd.h>

AncillaryDataSocket CreateSubchannel(AncillaryDataSocket* pMainChannel);

int DoClient(UniqueFd sockFd)
{
    try
    {
        auto pMainChannel = std::make_unique<AncillaryDataSocket>(std::move(sockFd));
        auto localSock = CreateSubchannel(pMainChannel.get());

        for (int i = 0; i < 3; i++)
        {
            char arg1[] = "hoge A";
            arg1[5] += i;

            Request r{};
            r.Token = 457;
            r.Flags = 0;
            if (i != 2)
            {
                r.WorkingDirectory = nullptr;
                r.ExecutablePath = "/bin/echo";
            }
            else
            {
                r.WorkingDirectory = "/tmp";
                r.ExecutablePath = "/bin/pwd";
            }
            r.Argv.push_back(r.ExecutablePath);
            r.Argv.push_back(arg1);
            r.Argv.push_back(nullptr);
            for (const char* const* p = environ; *p != nullptr; p++)
            {
                r.Envp.push_back(*p);
            }
            r.Envp.push_back(nullptr);

            auto message = SerializeRequest(r);
            if (message.size() > MaxMessageLength)
            {
                std::puts("client: message too long.");
                return 1;
            }

            auto messageBodyLength = static_cast<std::uint32_t>(message.size());
            if (!localSock.SendExactBytes(&messageBodyLength, sizeof(std::uint32_t))
                || !localSock.SendExactBytes(&message[0], messageBodyLength))
            {
                perror("client: send");
                return 1;
            }

            std::uint32_t response[2];
            if (!localSock.RecvExactBytes(response, sizeof(response)))
            {
                perror("client: recv");
                return 1;
            }

            std::printf("client: got response: %u, %u\n", response[0], response[1]);

            if (response[0] != 0)
            {
                continue;
            }

            ChildExitNotification data;
            if (!pMainChannel->RecvExactBytes(&data, sizeof(data)))
            {
                perror("client: recv");
                return 1;
            }

            std::printf("client: child %d exited: %u, %u\n", data.ProcessID, data.Code, data.Status);
        }

        return 0;
    }
    catch (const std::exception& exn)
    {
        std::fprintf(stderr, "error: %s\n", exn.what());
        return 1;
    }
}

AncillaryDataSocket CreateSubchannel(AncillaryDataSocket* pMainChannel)
{
    auto maybeSockerPair = CreateUnixStreamSocketPair();
    if (!maybeSockerPair)
    {
        perror("client: sockerpair");
        throw MyException("CreateSubchannel");
    }

    auto localSock = AncillaryDataSocket{std::move((*maybeSockerPair)[0])};
    auto remoteSock = std::move((*maybeSockerPair)[1]);

    const int fds[1]{remoteSock.Get()};
    if (!pMainChannel->SendExactBytesWithFd("", 1, fds, 1))
    {
        perror("client: SendExactBytesWithFd");
        throw MyException("CreateSubchannel");
    }

    remoteSock.Reset();

    std::int32_t err;
    if (!localSock.RecvExactBytes(&err, sizeof(err)))
    {
        if (errno != 0)
        {
            perror("client: recvmsg\n");
        }
        throw MyException("CreateSubchannel");
    }

    if (err != 0)
    {
        std::printf("client: subchannel creation failed : %d\n", err);
        throw MyException("CreateSubchannel");
    }

    return std::move(localSock);
}
