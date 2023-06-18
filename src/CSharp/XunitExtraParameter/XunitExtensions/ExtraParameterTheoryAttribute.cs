// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Xunit;
using Xunit.Sdk;

namespace XunitExtraParameter.XunitExtensions;

[XunitTestCaseDiscoverer($"{Constants.ExtensionsNamespace}.{nameof(ExtraParameterTheoryDiscoverer)}", Constants.AssemblyName)]
public sealed class ExtraParameterTheoryAttribute : TheoryAttribute { }
