// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;

namespace LibraryUser
{
    internal static class Program
    {
        public static void Main()
        {
            Console.WriteLine("Invoking Mul(4,7)...");
            Console.WriteLine("    = {0}", LibWithNativeCode.Lib.Mul(4, 7));
        }
    }
}
