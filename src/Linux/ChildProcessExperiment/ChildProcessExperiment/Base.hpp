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


void PutFatalError(int err, const char* str) noexcept;
[[noreturn]] void FatalErrorAbort(int err, const char* str) noexcept;
[[noreturn]] void FatalErrorExit(int err, const char* str) noexcept;

inline bool IsWouldBlockError(int err) { return err == EAGAIN || err == EWOULDBLOCK; }
inline bool IsConnectionClosedError(int err) noexcept { return err == ECONNRESET || err == EPIPE; }
