using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NormalizeEol
{
    internal static class Program
    {
        private static readonly IReadOnlyList<string> Patterns = new[] { "*.cs", "*.cpp" };
        private static readonly Encoding UTF8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static void Main(string[] args)
        {
            foreach (var d in args)
            {
                foreach (var pattern in Patterns)
                {
                    foreach (var f in Directory.EnumerateFiles(d, pattern, SearchOption.AllDirectories))
                    {
                        Fix(f);
                    }
                }
            }
        }

        private static void Fix(string f)
        {
            var text = File.ReadAllText(f);
            var newText = text.Replace("\r\n", "\n");
            if (text != newText)
            {
                Console.WriteLine("Fixing {0}...", f);
                File.WriteAllText(f, newText, UTF8NoBom);
            }
        }
    }
}
