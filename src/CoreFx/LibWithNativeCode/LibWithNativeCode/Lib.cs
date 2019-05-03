// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

namespace LibWithNativeCode
{
    public static class Lib
    {
        public static int Mul(int a, int b)
        {
            return NativeLib.Mul(a, b);
        }
    }
}
