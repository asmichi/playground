// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "SignalHandler.hpp"
#include "Base.hpp"
#include "MiscHelpers.hpp"
#include "UniqueResource.hpp"
#include <cassert>
#include <cstdint>
#include <signal.h>
#include <unistd.h>

int g_SignalDataPipeWriteEnd;
int g_SignalDataPipeReadEnd;

bool IsSignalIgnored(int signum);
void SetSignalAction(int signum, int extraFlags);
void SignalHandler(int signum, siginfo_t* siginfo, void* context);

void SetupSignalHandlers()
{
    // Preserve the ignored state as far as possible so that our children will inherit the state.
    if (!IsSignalIgnored(SIGINT))
    {
        SetSignalAction(SIGINT, 0);
    }
    if (!IsSignalIgnored(SIGQUIT))
    {
        SetSignalAction(SIGQUIT, 0);
    }
    if (!IsSignalIgnored(SIGPIPE))
    {
        SetSignalAction(SIGPIPE, 0);
    }

    SetSignalAction(SIGCHLD, SA_NOCLDSTOP);

    auto maybePipe = CreatePipe();
    if (!maybePipe)
    {
        FatalErrorAbort(errno, "pipe2");
    }

    g_SignalDataPipeReadEnd = maybePipe->ReadEnd.Release();
    g_SignalDataPipeWriteEnd = maybePipe->WriteEnd.Release();
}

bool IsSignalIgnored(int signum)
{
    struct sigaction oldact;
    [[maybe_unused]] int isError = sigaction(signum, nullptr, &oldact);
    assert(isError == 0);
    return oldact.sa_handler == SIG_IGN;
}

void SetSignalAction(int signum, int extraFlags)
{
    struct sigaction act = {};
    act.sa_flags = SA_RESTART | SA_SIGINFO | extraFlags;
    sigemptyset(&act.sa_mask);
    act.sa_sigaction = SignalHandler;

    [[maybe_unused]] int isError = sigaction(signum, &act, nullptr);
    assert(isError == 0);
}

void WriteToSignalDataProducerPipe(const void* buf, size_t len)
{
    if (!WriteExactBytes(g_SignalDataPipeWriteEnd, buf, len)
        && errno != EPIPE)
    {
        // Just abort; almost nothing can be done in a signal handler.
        abort();
    }
}

void SignalHandler(int signum, siginfo_t* siginfo, void* context)
{
    // Avoid doing the real work in the signal handler.
    // Dispatch the real work to the service thread.
    switch (signum)
    {
    case SIGINT:
    case SIGQUIT:
        // Do some cleanup and exit.
        WriteToSignalDataProducerPipe(&signum, sizeof(signum));
        break;

    case SIGCHLD:
    {
        // Handle termination of child.
        if (siginfo->si_code == CLD_EXITED || siginfo->si_code == CLD_KILLED || siginfo->si_code == CLD_DUMPED)
        {
            pid_t pid = siginfo->si_pid;
            WriteToSignalDataProducerPipe(&signum, sizeof(signum));
            WriteToSignalDataProducerPipe(&pid, sizeof(pid));
        }

        break;
    }

    case SIGPIPE:
    default:
        // Ignored
        break;
    }
}
