// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;

namespace TestProject
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal sealed class RecordTypeAttribute : Attribute
    {
        public RecordTypeAttribute()
        {
        }
    }
}
