// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Xunit;
using XunitExtraParameter.XunitExtensions;

namespace XunitExtraParameter;


public sealed class UnitTest1
{
    [ExtraParameterTheory]
    [ExtraParameterInlineData("a")]
    [ExtraParameterSet(1)]
    [ExtraParameterSet(2)]
    public void SampleTheory(ExtraParameter ep, string s)
    {
        Assert.Equal(1, ep.Foo);
        Assert.Equal("a", s);
    }
}
