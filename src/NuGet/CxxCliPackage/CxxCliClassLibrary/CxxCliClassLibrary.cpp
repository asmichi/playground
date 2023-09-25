// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#include "CxxCliClassLibrary.h"

namespace
{
    int PointerSize = sizeof(void*);
}

namespace Asmichi::CxxCliClassLibrary
{
    void Class1::HelloWorld()
    {
        Console::WriteLine("Hello, world!");
        Console::WriteLine("PointerSize: {0}", PointerSize);
    }
} // namespace Asmichi::CxxCliClassLibrary
