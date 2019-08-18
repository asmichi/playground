// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "AncillaryDataSocket.hpp"
#include "Base.hpp"
#include "Client.hpp"
#include "Service.hpp"
#include "Wrappers.hpp"
#include <algorithm>
#include <cstdio>
#include <cstring>
#include <fcntl.h>
#include <pthread.h>
#include <unistd.h>

namespace
{
    ServiceArgs g_ServiceArgs;
}

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
