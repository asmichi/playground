// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

// Common implementations.

#include <errno.h>
#include <exception>

class MyException : public std::exception
{
public:
    MyException(const char* description) : description_(description) {}
    virtual const char* what() const noexcept override { return description_; }

private:
    const char* const description_;
};

enum class BlockingFlag : bool
{
    Blocking = false,
    NonBlocking = true,
};

void PutFatalError(const char* str) noexcept;
void PutFatalError(int err, const char* str) noexcept;
[[noreturn]] void FatalErrorAbort(int err, const char* str) noexcept;
[[noreturn]] void FatalErrorExit(int err, const char* str) noexcept;

[[noreturn]] inline bool IsWouldBlockError(int err) { return err == EAGAIN || err == EWOULDBLOCK; }
[[noreturn]] inline bool IsConnectionClosedError(int err) noexcept { return err == ECONNRESET || err == EPIPE; }
