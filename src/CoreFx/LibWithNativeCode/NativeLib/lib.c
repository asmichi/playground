// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include <stdint.h>

#if defined(__GNUC__)
#define EXPORT
#elif defined(_MSC_VER)
#define EXPORT __declspec(dllexport)
#endif

EXPORT int32_t MulInt32(int32_t a, int32_t b)
{
    return (int32_t)(a * b);
}

#if defined(_MSC_VER)
int32_t __stdcall DllMain(
    void* hModule,
    uint32_t reason,
    void* lpReserved)
{
    return 1;
}
#endif
