// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using Xunit.Abstractions;
using Xunit.Sdk;

namespace XunitExtraParameter.XunitExtensions;

public sealed class ExtraParameterInlineDataDiscoverer : InlineDataDiscoverer
{
    public override IEnumerable<object?[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
    {
        // InlineData(null) will be interpreted as `data == null` instead of `data == new object?[] { null }`
        var data = (object?[])dataAttribute.GetConstructorArguments().Single() ?? new object?[] { null };
        var eps = ExtraParameterDiscoverer.GetData(testMethod);
        return eps.Select(x => new object?[] { x }.Concat(data).ToArray()).ToArray();
    }
}

public static class ExtraParameterDiscoverer
{
    public static IEnumerable<ExtraParameter?> GetData(IMethodInfo testMethod)
    {
        var results = new List<ExtraParameter?>();
        var indexes = testMethod.GetCustomAttributes(typeof(ExtraParameterSetAttribute))
            .Select(x => (int)x.GetConstructorArguments().Single());

        foreach (var index in indexes)
        {
            switch (index)
            {
                case 1:
                    results.Add(new ExtraParameter(1, 2));
                    results.Add(new ExtraParameter(1, 3));
                    break;
                case 2:
                    {
                        // Visual Studio の Test Explorer から実行する場合、runsettings を更新しても discoverer は再実行されない。
                        // リビルド等で再実行させる必要がある。
                        if (!int.TryParse(Environment.GetEnvironmentVariable("EXTRA_PARAMETER_FOO") ?? "300", out int value))
                        {
                            value = 300;
                        }
                        results.Add(new ExtraParameter(value, 2));
                        results.Add(new ExtraParameter(value, 3));
                    }
                    break;
                default:
                    break;
            }
        }

        // 現在の構成では ExtraParameter が列挙されないなら、 null でそれを伝える
        if (results.Count == 0)
        {
            results.Add(null);
        }

        return results;
    }
}
