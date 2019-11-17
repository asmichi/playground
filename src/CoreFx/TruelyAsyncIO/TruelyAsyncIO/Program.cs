// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace TruelyAsyncIO
{
    // Check if ReadAsync does not consume a thread-pool thread and instead issues an asynchronous IO operation...
    //
    // On .NET Core on Linux, NamedPipeServerStream and NamedPipeClientStream supports asynchronous IO.
    // They are implemented in terms of System.Net.Sockets.Socket, consequently epoll.
    internal static class Program
    {
        public static async Task<int> Main()
        {
            int N = Environment.ProcessorCount * 4;

            var writers = new List<Stream>(N);
            var readers = new List<Stream>(N);
            var readTasks = new List<Task<int>>(N);

            for (int i = 0; i < N; i++)
            {
                var (reader, writer) = CreateAsyncStreamPair();
                readers.Add(reader);
                writers.Add(writer);
            }

            // Issue more asynchronous reads then the number of thread-pool threads.
            for (int i = 0; i < N; i++)
            {
                var readerIndex = i;
                readTasks.Add(readers[readerIndex].ReadAsync(new byte[N + 1], 0, N + 1));
                Console.WriteLine("{0,02}: ReadAsync", readerIndex);
            }

            // Try to perform writes on thread-pool threads.
            // If asynchrnous reads above consumed exhausted thread pool threads, these tasks wouldn't start.
            for (int i = 0; i < N; i++)
            {
                var writerIndex = i;
                _ = Task.Run(() =>
                {
                    writers[writerIndex].Write(new byte[1 + writerIndex], 0, writerIndex + 1);
                    writers[writerIndex].Flush();
                    Console.WriteLine("{0,02}: Write on a ThreadPool thread", writerIndex);
                });
            }

            // Check that the above writes have completed and we can read from the stream.
            for (int i = 0; i < N; i++)
            {
                int n = await readTasks[i].ConfigureAwait(false);
                Console.WriteLine("{0,02}: await ReadAsync = {1}", i, n);
            }

            return 0;
        }

        private static int pipeSerialNumber;

        private static (Stream readEnd, Stream writeEnd) CreateAsyncStreamPair()
        {
            var thisPipeSerialNumber = Interlocked.Increment(ref pipeSerialNumber);
            var pipeName = string.Format(
                CultureInfo.InvariantCulture,
                @"\\.\pipe\Asmichi.ChildProcess.7785FB5A-AB05-42B2-BC02-A14769CC463E.{0}.{1}",
                System.Diagnostics.Process.GetCurrentProcess().Id,
                thisPipeSerialNumber);

            var serverStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            var clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            clientStream.Connect();
            serverStream.WaitForConnection();
            return (serverStream, clientStream);
        }
    }
}
