// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Text;

namespace Asmichi.RecordTypeGenerator
{
    internal static class Utils
    {
        public static string ToInitialLowered(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            else
            {
                return char.ToLowerInvariant(s[0]) + s.Substring(1);
            }
        }
    }
}
