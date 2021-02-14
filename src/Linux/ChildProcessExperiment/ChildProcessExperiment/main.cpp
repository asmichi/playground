// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "Client.hpp"
#include "MiscHelpers.hpp"
#include "Service.hpp"
#include <algorithm>
#include <cstdio>
#include <cstring>
#include <fcntl.h>
#include <pthread.h>
#include <unistd.h>

namespace
{
    struct ServiceArgs final
    {
        UniqueFd ControlSocket;
        int ExitCode;
    };

    ServiceArgs g_ServiceArgs;
} // namespace

pthread_t StartService(ServiceArgs* args);
void* ServiceThreadFunc(void* arg);

int main(int argc, const char** argv)
{
    auto maybeSocketPair = CreateUnixStreamSocketPair();
    if (!maybeSocketPair)
    {
        PutFatalError(errno, "socketpair");
        return 1;
    }

    g_ServiceArgs.ControlSocket = std::move((*maybeSocketPair)[1]);

    pthread_t serviceThreadId = StartService(&g_ServiceArgs);

    const int clientExitCode = DoClient(std::move((*maybeSocketPair)[0]));
    std::printf("client exited with: %d\n", clientExitCode);

    int err = pthread_join(serviceThreadId, nullptr);
    if (err != 0)
    {
        PutFatalError(err, "pthread_join");
        return 1;
    }

    std::printf("service exited with: %d\n", g_ServiceArgs.ExitCode);

    return clientExitCode;
}

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
    auto sock = std::move(pArgs->ControlSocket);
    pArgs->ExitCode = ServiceMain(sock.Get());
    std::printf("service: closing\n");
    return nullptr;
}
