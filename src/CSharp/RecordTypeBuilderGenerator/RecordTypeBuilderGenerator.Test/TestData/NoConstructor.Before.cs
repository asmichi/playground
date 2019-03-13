// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;

namespace Test
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    sealed class RecordTypeAttribute : Attribute
    {
        public RecordTypeAttribute()
        {
        }
    }

    /*!*/[RecordType]
    class TypeName
    {
        public int A { get; }
        public double B { get; }
    }
}
