// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#pragma once

#include <cstddef>
#include <cstdint>
#include <cstring>
#include <numeric>
#include <stdexcept>
#include <string>
#include <vector>

class BinaryWriter final
{
public:
    template<typename T>
    void Write(T value)
    {
        Write(&value, sizeof(T));
    }

    void Write(const void* data, std::size_t len)
    {
        std::memcpy(ExtendAndGetCurrent(len), data, len);
    }

    void WriteString(const char* s)
    {
        if (s == nullptr)
        {
            Write<std::uint32_t>(0);
            return;
        }

        auto bytes = std::char_traits<char>::length(s) + 1;
        if (bytes > std::numeric_limits<std::uint32_t>::max())
        {
            throw std::out_of_range("string too long");
        }

        Write<std::uint32_t>(static_cast<std::uint32_t>(bytes));
        Write(s, bytes);
    }

    std::vector<std::byte> Detach()
    {
        return std::move(buf_);
    }

private:
    std::byte* ExtendAndGetCurrent(std::size_t bytesToWrite)
    {
        auto cur = buf_.size();
        buf_.resize(cur + bytesToWrite);
        return &buf_[cur];
    }

    std::vector<std::byte> buf_;
};
