using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RemoveBom
{
    internal static class Program
    {
        private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private static readonly byte[] Utf8Preamble = Utf8Bom.GetPreamble();

        /// <summary>
        /// Convert specified UTF-8 files to UTF-8 NoBOM. 
        /// </summary>
        /// <param name="args">Files</param>
        public static int Main(string[] args)
        {
            bool hasError = false;

            foreach (var path in args)
            {
                byte[] bytes;
                try
                {
                    bytes = File.ReadAllBytes(path);
                }
                catch (Exception e) when (IsIORelatedException(e))
                {
                    Console.WriteLine("error: {0} : Failed to read from the file ({1}).", path, e.Message);
                    hasError = true;
                    continue;
                }

                if (!HasPreamble(bytes))
                {
                    Console.WriteLine("info: {0} : File already has the UTF-8 preamble removed; skipping.", path);
                    continue;
                }

                var noPreambleSpan = bytes.AsSpan(Utf8Preamble.Length, bytes.Length - Utf8Preamble.Length);

                if (!AreValidUtf8Bytes(noPreambleSpan))
                {
                    Console.WriteLine("error: {0} : File is not UTF-8 encoded.", path);
                    hasError = true;
                    continue;
                }

                try
                {
                    using (var w = File.OpenWrite(path))
                    {
                        w.SetLength(noPreambleSpan.Length);
                        w.Write(noPreambleSpan);
                    }
                }
                catch (Exception e) when (IsIORelatedException(e))
                {
                    Console.WriteLine("error: {0} : Failed to write to the file ({1}).", path, e.Message);
                    hasError = true;
                    continue;
                }
            }

            return hasError ? 1 : 0;
        }

        private static bool IsIORelatedException(Exception e)
        {
            return e is IOException
                || e is UnauthorizedAccessException;
        }

        private static bool HasPreamble(byte[] bytes)
        {
            return bytes.Length >= Utf8Preamble.Length
                && bytes.Take(Utf8Preamble.Length).SequenceEqual(Utf8Preamble);
        }

        private static bool AreValidUtf8Bytes(ReadOnlySpan<byte> bytes)
        {
            try
            {
                Utf8NoBom.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            return true;
        }
    }
}
