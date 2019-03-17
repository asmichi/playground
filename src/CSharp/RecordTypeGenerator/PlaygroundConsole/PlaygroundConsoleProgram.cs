// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Asmichi.RecordTypeGenerator
{
    internal static class PlaygroundConsoleProgram
    {
        public static async Task<int> Main(string[] args)
        {
            var targetDirectory = args[0];
            var targetSourceFiles = Directory.GetFiles(targetDirectory, "*.cs");

            using (var workspace = CreateTestWorkspace(targetSourceFiles))
            {
                var analyzer = new RecordTypeGeneratorAnalyzer();
                var codeFix = new RecordTypeGeneratorCodeFixProvider();

                foreach (var project in workspace.CurrentSolution.Projects)
                {
                    var diagnostics = await GetDiagnosticsAsync(analyzer, project);
                    foreach (var d in diagnostics)
                    {
                        Console.WriteLine(d.ToString());
                    }
                }
            }

            return 0;
        }

        private static async Task<IReadOnlyList<Diagnostic>> GetDiagnosticsAsync(DiagnosticAnalyzer analyzer, Project project)
        {
            var compilation = await project.GetCompilationAsync();
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }

        private static Workspace CreateTestWorkspace(string[] sourceFiles)
        {
            var workspace = new AdhocWorkspace();
            var newSolution = AddTestProject(workspace.CurrentSolution, sourceFiles);
            if (!workspace.TryApplyChanges(newSolution))
            {
                throw new Exception("unexpected");
            }
            return workspace;
        }

        private static Solution AddTestProject(Solution solution, string[] sourceFiles)
        {
            const string TestProjectName = "TestProject";
            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);
            var references = new[]
            {
                // mscorlib
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                // System.Core
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            };

            var newSolution = solution;

            newSolution = newSolution.AddProject(projectId, TestProjectName, "Asmichi.TestProject", LanguageNames.CSharp);

            foreach (var reference in references)
            {
                newSolution = newSolution.AddMetadataReference(projectId, reference);
            }

            foreach (var sourceFile in sourceFiles)
            {
                var sourceText = File.ReadAllText(sourceFile);
                var fileName = Path.GetFileName(sourceFile);
                var documentId = DocumentId.CreateNewId(projectId, debugName: fileName);

                newSolution = newSolution.AddDocument(documentId, fileName, SourceText.From(sourceText));
            }

            return newSolution;
        }
    }
}
