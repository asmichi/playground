// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "Request.hpp"
#include "BinaryReader.hpp"
#include "BinaryWriter.hpp"
#include "ErrnoExceptions.hpp"
#include <cassert>
#include <cstdint>
#include <cstring>
#include <memory>
#include <vector>

namespace
{
    void ReadStringArray(BinaryReader& br, std::vector<const char*>* buf)
    {
        const auto count = br.Read<std::uint32_t>();
        if (count > MaxStringArrayCount)
        {
            throw BadRequestError(E2BIG);
        }

        for (std::uint32_t i = 0; i < count; i++)
        {
            buf->push_back(br.ReadString());
        }
    }

    void WriteStringArray(BinaryWriter& bw, const std::vector<const char*>& data)
    {
        if (data.size() > MaxStringArrayCount)
        {
            std::abort();
        }

        bw.Write(static_cast<std::uint32_t>(data.size()));
        for (auto s : data)
        {
            bw.WriteString(s);
        }
    }
} // namespace

void DeserializeRequest(Request* r, std::unique_ptr<const std::byte[]> data, std::size_t length)
{
    try
    {
        BinaryReader br{data.get(), length};
        r->Data = std::move(data);
        r->Token = br.Read<std::uint64_t>();
        r->Flags = br.Read<std::uint32_t>();
        r->ExecutablePath = br.ReadString();
        ReadStringArray(br, &r->Argv);
        ReadStringArray(br, &r->Envp);

        if (r->ExecutablePath == nullptr || r->Argv.back() != nullptr || r->Envp.back() != nullptr)
        {
            throw BadRequestError(EINVAL);
        }
    }
    catch (const BadBinaryError&)
    {
        throw BadRequestError(EINVAL);
    }
}

std::vector<std::byte> SerializeRequest(const Request& r)
{
    BinaryWriter bw;
    bw.Write(r.Token);
    bw.Write(r.Flags);
    bw.WriteString(r.ExecutablePath);
    WriteStringArray(bw, r.Argv);
    WriteStringArray(bw, r.Envp);

    return bw.Detach();
}
