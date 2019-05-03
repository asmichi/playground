// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Runtime.InteropServices;

namespace LibWithNativeCode
{
    internal static class NativeLib
    {
        private const string DllName = "libNativeLib";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "MulInt32", SetLastError = false)]
        public static extern int Mul(int a, int b);
    }
}
