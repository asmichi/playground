using EmbedFSharp.ScriptInterfaces;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.Interactive;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

// How do we expose an interface to our app to the script?
//
// - (Have the module expose a specific function and pass an accessor to it.)
//   - No longer a script, but may be suitable for extensions (plugins).
// - Expose static functions
//   - We will need AsyncLocal (LogicalCallContext) or separate AppDomain's to distinguish which script is calling the functions.
// - Inject an accesor into FsiEvaluationSession (see IFsiSession.Let of YaafFSharpScripting)
//   - Only works in an interactive situation.

namespace EmbedFSharp
{
    internal class Program
    {
        internal static AsyncLocal<ScriptContext> CurrentScriptContext = new AsyncLocal<ScriptContext>();

        private static readonly string[] commonCompileOptions = new string[]
        {
            "--define:DEBUG",
            "--define:TRACE",
            "--define:EMBEDFSHARP",
            "--define:EMBEDFSHARP_DEBUG",
            "--debug+",
            "--debug:full",
            // Disable optimization so that we can step through code.
            "--optimize-",
            "--tailcalls-",
            // Introduce MyAppAccessor by adding a reference to our assembly.
            "--reference:" + typeof(Program).Assembly.Location,
        };

        public static async Task Main(string[] args)
        {
            await PlayAsync();
        }

        private static async Task PlayAsync()
        {
            var scriptPath = @"..\..\Scripts\Script.fsx";

            var checker = FSharpChecker.Create(
                projectCacheSize: FSharpOption<int>.None,
                keepAssemblyContents: FSharpOption<bool>.None,
                keepAllBackgroundResolutions: FSharpOption<bool>.None,
                msbuildEnabled: FSharpOption<bool>.Some(true));

            // static assembly by fsc.exe
            Console.WriteLine("----------");
            {
                await ExecuteInScriptContextAsync(
                    "static assembly by fsc.exe",
                    () =>
                    {
                        var assemblyPath = Path.GetRandomFileName() + ".dll";
                        var assembly = CompileToAssemblyByFscExe(assemblyPath, scriptPath);
                        if (assembly != null)
                        {
                            ExecuteStartupCode(assembly);
                        }
                        return Task.CompletedTask;
                    });
            }

            // static assembly
            Console.WriteLine("----------");
            {
                await ExecuteInScriptContextAsync(
                    "static assembly",
                    async () =>
                    {
                        var assemblyPath = Path.GetRandomFileName() + ".dll";
                        var assembly = await CompileToAssembly(checker, assemblyPath, scriptPath);
                        ExecuteStartupCode(assembly);
                    });
            }

            // dynamic assembly
            Console.WriteLine("----------");
            {
                await ExecuteInScriptContextAsync(
                    "dynamic assembly",
                    async () =>
                    {
                        var assemblyName = "Script.dll"; 
                        var assembly = await CompileToDynamicAssembly(checker, assemblyName, scriptPath);
                        // var b = (AssemblyBuilder)assembly;
                        ExecuteStartupCode(assembly);
                    });
            }

            // interactive
            Console.WriteLine("----------");
            {
                await ExecuteInScriptContextAsync(
                    "interactive",
                    () =>
                    {
                        ExecuteInteractive(scriptPath);
                        return Task.CompletedTask;
                    });
            }
        }

        private static async Task ExecuteInScriptContextAsync(string contextName, Func<Task> action)
        {
            var context = new ScriptContext(contextName);
            CurrentScriptContext.Value = context;

            await action();

            // TODO: The script may have failed to wait tasks it spawned...

            CurrentScriptContext.Value = null;

            Console.WriteLine("strings from script:");
            foreach (var s in context.GetStrings())
            {
                Console.WriteLine(s);
            }
        }

        private static Assembly CompileToAssemblyByFscExe(string assemblyPath, string sourcePath)
        {
            var fscExePath = GetFscExePath();
            if (string.IsNullOrEmpty(fscExePath))
            {
                throw new ScriptCompilationErrorException();
            }

            var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
            var fscArgs = new List<string> {
                "--nologo",
                "--target:library",
                "--nocopyfsharpcore",
                Invariant($"--out:{assemblyPath}"),
                Invariant($"--pdb:{pdbPath}"),
                sourcePath
            };
            fscArgs.AddRange(commonCompileOptions);

            var psi = new ProcessStartInfo()
            {
                FileName = fscExePath,
                Arguments = string.Join(" ", fscArgs),
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardInput = true,
                RedirectStandardError = false,
                RedirectStandardOutput = false,
            };

            using (var p = Process.Start(psi))
            {
                p.StandardInput.Close();

                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    throw new ScriptCompilationErrorException();
                }

                return Assembly.LoadFrom(assemblyPath);
            }
        }

        private static async Task<Assembly> CompileToAssembly(
            FSharpChecker checker,
            string assemblyPath,
            string sourcePath)
        {
            var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
            var fscArgs = new List<string> {
                "fsc.exe",
                "--target:library",
                Invariant($"--out:{assemblyPath}"),
                Invariant($"--pdb:{pdbPath}"),
                sourcePath
            };
            fscArgs.AddRange(commonCompileOptions);

            var (compilationErrors, exitCode) = await checker.Compile(fscArgs.ToArray()).StartAsTask();

            WriteCompilationErrors(compilationErrors);

            if (exitCode != 0)
            {
                throw new ScriptCompilationErrorException();
            }

            return Assembly.LoadFrom(assemblyPath);
        }

        private static async Task<Assembly> CompileToDynamicAssembly(
            FSharpChecker checker,
            string assemblyName,
            string sourcePath)
        {
            var fscArgs = new List<string> {
                "fsc.exe",
                // This --out argument just specifies the name of the generated assembly. The generated assembly will not be written to the disk.
                Invariant($"--out:{assemblyName}"),
                sourcePath
            };
            fscArgs.AddRange(commonCompileOptions);

            // Avoid supplying the `execute` argument; it overwrites Console.Out and Console.Error.
            var (compilationErrors, exitCode, assembly) =
                await checker.CompileToDynamicAssembly(fscArgs.ToArray(), FSharpOption<Tuple<TextWriter, TextWriter>>.None).StartAsTask();

            WriteCompilationErrors(compilationErrors);

            if (exitCode != 0 || assembly.IsNone())
            {
                throw new ScriptCompilationErrorException();
            }

            return assembly.Value;
        }

        private static void ExecuteStartupCode(Assembly assembly)
        {
            // File-level code is compiled as class initializers (.cctor's), so we need to invoke them.

            // Such classes are generated under a namespace named `<StartupCode${module name}>'.
            var startupCodeTypes = assembly.GetTypes()
                .Where(x => x.Namespace?.Contains("StartupCode") ?? false)
                .ToArray();

            // This does not preserve the execution order of startup code (included modules -> the script).
            //
            // Since the startup code for the script is contained in class `${module name}$fsx`, we can execute it at the last or only execute it.
            // Anyway we should not produce visible side effects in module startup code or at least should not rely on the execution order.
            foreach (var t in startupCodeTypes)
            {
                // The F# compiler generates a special `init@` field for each 'startup' class.
                // Evaluating the field invokes the class initializer.
                var initField = t.GetField("init@", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (initField == null)
                {
                    continue;
                }
                if (!initField.IsDefined(typeof(CompilerGeneratedAttribute)))
                {
                    continue;
                }

                initField.GetValue(null);
            }
        }

        private static void ExecuteInteractive(string scriptPath)
        {
            // See https://fsharp.github.io/FSharp.Compiler.Service/interactive.html

            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();
            var inStream = new StringReader("");
            var outStream = new StringWriter(sbOut);
            var errStream = new StringWriter(sbErr);
            var fsiConfig = Shell.FsiEvaluationSession.GetDefaultConfiguration();

            var fsiArgs = new List<string> {
                "fsi.exe",
                "--noninteractive",
                scriptPath
            };
            fsiArgs.AddRange(commonCompileOptions);

            using (var fsiSession = Shell.FsiEvaluationSession.Create(
                fsiConfig,
                fsiArgs.ToArray(),
                inStream,
                outStream,
                errStream,
                collectible: new FSharpOption<bool>(true),
                msbuildEnabled: new FSharpOption<bool>(true)))
            {
                evalScript(fsiSession, scriptPath);
                eval(fsiSession, "printfn \"Inc1Value: %d\" Inc1Value");

                var moduleName = Path.GetFileNameWithoutExtension(scriptPath);
                eval(fsiSession, Invariant($"{moduleName}.xs"));
            }

            Console.WriteLine("Output from fsi: ");
            Console.WriteLine(sbOut.ToString());
            Console.WriteLine("Error from fsi: ");
            Console.WriteLine(sbErr.ToString());

            void evalScript(Shell.FsiEvaluationSession fsiSession, string path)
            {
                var (unit, compilationErrors) = fsiSession.EvalScriptNonThrowing(path).ToValueTuple();

                WriteCompilationErrors(compilationErrors);
            }

            void eval(Shell.FsiEvaluationSession fsiSession, string expr)
            {
                var (choice, compilationErrors) = fsiSession.EvalExpressionNonThrowing(expr);

                WriteCompilationErrors(compilationErrors);

                if (choice.IsChoice1Of2)
                {
                    var option = choice.GetChoice1();
                    if (option.IsSome())
                    {
                        var value = option.Value;
                        Console.WriteLine(value.ReflectionValue);
                    }
                    else
                    {
                        Console.WriteLine("(None)");
                    }
                }
                else
                {
                    var exception = choice.GetChoice2();
                    Console.WriteLine("Exception {0}: {1}", exception.GetType().FullName, exception.ToString());
                }
            }
        }

        private static void WriteCompilationErrors(FSharpErrorInfo[] compilationErrors)
        {
            foreach (var compilationError in compilationErrors)
            {
                Console.WriteLine(compilationError.ToString());
            }
        }

        private static string GetFscExePath()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (string.IsNullOrEmpty(programFiles))
            {
                return null;
            }

            var candidate = Path.Combine(programFiles, @"Microsoft SDKs\F#\4.1\Framework\v4.0\fsc.exe");
            if (!File.Exists(candidate))
            {
                return null;
            }

            return candidate;
        }
    }

    public static class FSharpChoiceExtensions
    {
        public static T1 GetChoice1<T1, T2>(this FSharpChoice<T1, T2> choice) => ((FSharpChoice<T1, T2>.Choice1Of2)choice).Item;
        public static T2 GetChoice2<T1, T2>(this FSharpChoice<T1, T2> choice) => ((FSharpChoice<T1, T2>.Choice2Of2)choice).Item;

        public static T1 GetChoice1<T1, T2, T3>(this FSharpChoice<T1, T2, T3> choice) => ((FSharpChoice<T1, T2, T3>.Choice1Of3)choice).Item;
        public static T2 GetChoice2<T1, T2, T3>(this FSharpChoice<T1, T2, T3> choice) => ((FSharpChoice<T1, T2, T3>.Choice2Of3)choice).Item;
        public static T3 GetChoice3<T1, T2, T3>(this FSharpChoice<T1, T2, T3> choice) => ((FSharpChoice<T1, T2, T3>.Choice3Of3)choice).Item;
    }

    public static class FSharpOptionExtensions
    {
        public static bool IsSome<T>(this FSharpOption<T> option) => FSharpOption<T>.get_IsSome(option);
        public static bool IsNone<T>(this FSharpOption<T> option) => FSharpOption<T>.get_IsNone(option);
    }

    public static class FSharpAsyncExtensions
    {
        public static Task<T> StartAsTask<T>(this FSharpAsync<T> fsharpAsync) =>
            FSharpAsync.StartAsTask(fsharpAsync, taskCreationOptions: FSharpOption<TaskCreationOptions>.None, cancellationToken: FSharpOption<CancellationToken>.None);
    }

    [Serializable]
    public class ScriptCompilationErrorException : Exception
    {
        public ScriptCompilationErrorException() { }
        public ScriptCompilationErrorException(string message) : base(message) { }
        public ScriptCompilationErrorException(string message, Exception inner) : base(message, inner) { }
        protected ScriptCompilationErrorException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
