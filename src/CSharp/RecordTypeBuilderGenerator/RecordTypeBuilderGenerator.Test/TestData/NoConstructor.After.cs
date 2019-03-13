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

    [RecordType]
    class TypeName
    {
        public int A { get; }
        public double B { get; }

        public TypeName(int a, double b)
        {
            this.A = a;
            this.B = b;
        }

        public TypeName(TypeName other)
        {
            this.A = other.A;
            this.B = other.B;
        }

        public override string ToString()
        {
            return $"TypeName(A: {A}, B: {B})";
        }

        public class Builder
        {
            public int A { get; set; }

            public Builder(TypeName other)
            {
                this.A = other.A;
                this.B = other.B;
            }

            public TypeName Build()
            {
                return new TypeName(A, B);
            }

            public override string ToString()
            {
                return $"TypeName(A: {A}, B: {B})";
            }
        }
    }
}
