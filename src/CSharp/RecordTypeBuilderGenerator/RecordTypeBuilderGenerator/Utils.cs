// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;

namespace RecordTypeBuilderGenerator
{
    internal static class Utils
    {
        public static string MakeInitialLowerString(string s)
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
