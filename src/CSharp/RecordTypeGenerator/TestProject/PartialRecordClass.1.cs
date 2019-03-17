// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;

namespace TestProject
{
    [RecordType]
    internal sealed partial class PartialRecordClass : IEquatable<PartialRecordClass>, IComparable<PartialRecordClass>
    {
        public int A { get; }
        public double B { get; }
    }
}
