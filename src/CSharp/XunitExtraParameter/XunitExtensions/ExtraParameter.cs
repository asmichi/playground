// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Xunit.Abstractions;

namespace XunitExtraParameter.XunitExtensions;

public sealed record ExtraParameter : IXunitSerializable
{
    public int Foo { get; set; }
    public int Bar { get; set; }

    public ExtraParameter() { }

    public ExtraParameter(int foo, int bar)
    {
        Foo = foo;
        Bar = bar;
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Foo = info.GetValue<int>(nameof(Foo));
        Bar = info.GetValue<int>(nameof(Bar));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Foo), Foo);
        info.AddValue(nameof(Bar), Bar);
    }
}
