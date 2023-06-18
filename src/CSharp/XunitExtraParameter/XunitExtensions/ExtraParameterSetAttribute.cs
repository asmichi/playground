// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

namespace XunitExtraParameter.XunitExtensions;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ExtraParameterSetAttribute : Attribute
{
    public int Index { get; }

    public ExtraParameterSetAttribute(int index) => Index = index;
}
