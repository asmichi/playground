// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Asmichi.StreamJsonRpcAdapters;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using StreamJsonRpc;

namespace TryStreamJsonRpc
{
    internal static class Program
    {
        public static async Task Main()
        {
            var (streamA, streamB) = Nerdbank.Streams.FullDuplexStream.CreatePair();

            using (var sideA = CreateRpc("A", streamA))
            using (var sideB = CreateRpc("B", streamB))
            {
                {
                    var ret = await sideA.InvokeWithParameterObjectAsync<int>("add", new { a = 1, b = 2 });
                    Console.WriteLine(ret);
                }
                {
                    var ret = await sideA.InvokeAsync<double>("transform", 4);
                    Console.WriteLine(ret);
                }
            }
        }

        private static JsonRpc CreateRpc(string name, Stream duplexStream)
        {
            var options = new JsonRpcMessagePackFormatterOptions()
            {
                Resolver = MyCompositeResolver.Instance,
                AllowParameterObject = true,
            };
            var formatter = new JsonRpcMessagePackFormatter(options);
            var handler = new LengthHeaderMessageHandler(duplexStream, duplexStream, formatter);
            var rpc = new JsonRpc(handler)
            {
                TraceSource = new TraceSource(name, SourceLevels.Verbose),
            };

            rpc.AddLocalRpcMethod("add", (Func<int, int, int>)Add);
            rpc.AddLocalRpcMethod("transform", (Func<int, double>)Transform);
            rpc.StartListening();
            return rpc;
        }

        private static int Add(int a, int b)
        {
            return a + b;
        }

        private static double Transform(int x)
        {
            return 1 / (double)x;
        }
    }

    internal sealed class MyCompositeResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new MyCompositeResolver();

        static readonly IFormatterResolver[] Resolvers = new[]
        {
            JsonRpcMessagePackResolver.Instance,
            StandardResolver.Instance
        };

        public IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter = CompositeResolverHelper.GetFormatter<T>(Resolvers);
        }
    }
}
