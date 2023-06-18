// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Xunit.Abstractions;
using Xunit.Sdk;

namespace XunitExtraParameter.XunitExtensions;

public sealed class ExtraParameterTheoryDiscoverer : TheoryDiscoverer
{
    public ExtraParameterTheoryDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink) { }

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
    {
        return new IXunitTestCase[]
        {
            new ExecutionErrorTestCase(
                DiagnosticMessageSink,
                discoveryOptions.MethodDisplayOrDefault(),
                discoveryOptions.MethodDisplayOptionsOrDefault(),
                testMethod,
                $"Theory data is not serializable, or data discovery failed."
            )
        };
    }

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo theoryAttribute,
        object[] dataRow)
    {
        if (dataRow.Length == 0 || dataRow[0] is null)
        {
            // This requires xUnit 2.4.2.
            // https://github.com/xunit/visualstudio.xunit/issues/266 Skip attribute on [InlineData] is not respected in Visual Studio
            return base.CreateTestCasesForSkippedDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, "No ExtraParameter");
        }
        else
        {
            return base.CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow);
        }
    }

    public override IEnumerable<IXunitTestCase> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod,
        IAttributeInfo theoryAttribute)
    {
        if (!(testMethod.Method.GetParameters().FirstOrDefault() is { } firstParameter && firstParameter.ParameterType.Name == typeof(ExtraParameter).FullName))
        {
            return new IXunitTestCase[]
            {
                new ExecutionErrorTestCase(
                    DiagnosticMessageSink,
                    discoveryOptions.MethodDisplayOrDefault(),
                    discoveryOptions.MethodDisplayOptionsOrDefault(),
                    testMethod,
                    $"A [{nameof(ExtraParameterTheoryAttribute)}] method must have {nameof(ExtraParameter)} as its first parameter."
                )
            };
        }

        return base.Discover(discoveryOptions, testMethod, theoryAttribute);
    }
}
