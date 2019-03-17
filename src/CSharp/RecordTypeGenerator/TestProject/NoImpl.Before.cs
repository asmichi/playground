// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;

namespace TestProject
{
    [RecordType]
    internal sealed class RecordClass : IEquatable<RecordClass>, IComparable<RecordClass>
    {
        public int A { get; }
        public double B { get; }

        // This is not a record element.
        public int Property => A;
    }

    [RecordType]
    internal readonly struct RecordStruct : IEquatable<RecordStruct>, IComparable<RecordStruct>
    {
        public readonly int A;
        public readonly double B;
    }
}
