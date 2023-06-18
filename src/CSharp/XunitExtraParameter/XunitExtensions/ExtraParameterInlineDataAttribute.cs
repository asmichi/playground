// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Reflection;
using Xunit.Sdk;

namespace XunitExtraParameter.XunitExtensions;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[DataDiscoverer($"{Constants.ExtensionsNamespace}.{nameof(ExtraParameterInlineDataDiscoverer)}", Constants.AssemblyName)]
public sealed class ExtraParameterInlineDataAttribute : DataAttribute
{
    public ExtraParameterInlineDataAttribute(params object?[] data) { }

    public override IEnumerable<object?[]> GetData(MethodInfo testMethod) =>
        // Seemingly not called in the current version.
        throw new NotSupportedException();
}
