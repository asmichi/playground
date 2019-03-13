// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TestHelper;

namespace RecordTypeBuilderGenerator.Test
{
    internal static class TestUtils
    {
        private static readonly Regex DiagnosticsLocationRegex = new Regex(@"\/\*\!\*\/");

        /// <summary>
        /// Get source locations marked with /*!*/ from source text.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <returns></returns>
        public static DiagnosticResultLocation[] GetMarkedLocations(string sourceText)
        {
            return sourceText.Split('\n').SelectMany((s, lineIndex) =>
                GetMarkedLocationsFromLine(lineIndex + 1, s)).ToArray();
        }

        private static IEnumerable<DiagnosticResultLocation> GetMarkedLocationsFromLine(int lineNumber, string line)
        {
            return DiagnosticsLocationRegex.Matches(line).Cast<Match>().Select(m =>
                new DiagnosticResultLocation("Test0.cs", lineNumber, m.Index + m.Length + 1));
        }
    }
}
