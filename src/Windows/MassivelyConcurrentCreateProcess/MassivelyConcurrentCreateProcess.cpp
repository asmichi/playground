// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

// Massively concurrent invocation of CreateProcess within one process sometimes fails with ERROR_NOT_ENOUGH_MEMORY.
//
// WARNING: It is recommented that you execute this program in a disposable VM.
//          This program may render Windows unresponsive, forcing you to forcibly power off it.
//
// NOTE: Some AV software greatly slows down CreateProcess, which decreases the concurrency and thus "eliminates" this symptom.

#include "pch.h"
#include <cstdio>
#include <cstdlib>
#include <memory>

constexpr int ThreadCount = 512;
constexpr int IterationCount = 10;
constexpr size_t ChildCommandLineBufferSize = MAX_PATH + 3;

DWORD WINAPI ThreadFunc(LPVOID lpThreadParameter);

int wmain(int argc, wchar_t* argv[])
{
    // For safely, only execute this experiment when given the "launch" argument.
    const bool launchEnabled = (argc == 2 && (wcscmp(argv[1], L"launch") == 0));
    if (!launchEnabled)
    {
        return 0;
    }

    std::printf("launching...\n");

    wchar_t childCommandLine[ChildCommandLineBufferSize]{};
    wsprintf(childCommandLine, L"%ls", argv[0]);

    HANDLE threads[ThreadCount]{};
    for (int i = 0; i < ThreadCount; i++)
    {
        HANDLE hThread = CreateThread(nullptr, 0, ThreadFunc, childCommandLine, CREATE_SUSPENDED, nullptr);
        if (hThread == nullptr)
        {
            std::printf("CreateThread failed: %d\n", GetLastError());
            std::abort();
        }

        threads[i] = hThread;
    }

    for (int i = 0; i < ThreadCount; i++)
    {
        ResumeThread(threads[i]);
    }

    std::printf("launched\n");

    for (int i = 0; i < ThreadCount; i++)
    {
        WaitForSingleObject(threads[i], INFINITE);
        CloseHandle(threads[i]);
    }

    return 0;
}

DWORD WINAPI ThreadFunc(LPVOID lpThreadParameter)
{
    wchar_t childCommandLine[ChildCommandLineBufferSize]{};
    std::memcpy(childCommandLine, static_cast<const char*>(lpThreadParameter), ChildCommandLineBufferSize * sizeof(wchar_t));

    for (int i = 0; i < IterationCount; i++)
    {
        PROCESS_INFORMATION pi{};
        STARTUPINFOW si{};
        si.cb = sizeof(si);

        if (!CreateProcessW(
            nullptr,
            childCommandLine,
            nullptr,
            nullptr,
            FALSE,
            CREATE_NO_WINDOW,
            nullptr,
            nullptr,
            &si,
            &pi))
        {
            // sometimes results in "CreateProcess failed: 8"
            std::printf("CreateProcess failed: %d\n", GetLastError());
        }
        else
        {
            WaitForSingleObject(pi.hProcess, INFINITE);
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
        }
    }

    return 0;
}
