// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace RecordTypeBuilderGenerator.Test
{
    public class AnalyzerTest : CodeFixVerifier
    {
        //No diagnostics expected to show up
        [Fact]
        public void EmptyCodeGeneratesNoDiagnostics()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        public static IEnumerable<object[]> H => new[]
        {
            new object[] { 1, 1 },
            new object[] { 2, 2 }
        };

        [Theory]
        [MemberData(nameof(H))]
        public void Hoge(int x, int y)
        {
            Assert.Equal(x, y);
        }

        [Fact]
        public void GenerateCtorErrorWhenNoConstructor()
        {
            var source = File.ReadAllText("TestData/NoConstructor.Before.cs");
            var locations = TestUtils.GetMarkedLocations(source);

            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "RecordTypeBuilder_GenerateCtor",
                    Message = String.Format("Constructors need to be generated. Use the code fix."),
                    Severity = DiagnosticSeverity.Error,
                    Locations = new [] { locations[0] }
                },
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new RecordTypeBuilderGeneratorCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new RecordTypeBuilderGeneratorAnalyzer();
        }
    }
}
