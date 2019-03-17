// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Asmichi.RecordTypeGenerator
{
    // https://github.com/dotnet/csharplang/blob/master/proposals/records.md

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RecordTypeGeneratorAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsValue =
            ImmutableArray.Create(
                Descriptors.RuleRecordType,
                Descriptors.RuleIEquatable,
                Descriptors.RuleIComparable
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsValue;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            // context.EnableConcurrentExecution();
            // TODO: Load configurations at the beginning of compilation.
            // context.RegisterCompilationStartAction()

            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            AnalyzerImpl.AnalyzeSymbol(context);
        }
    }

    internal sealed class AnalyzerImpl
    {
        private const string TargetAttributeName = "RecordTypeAttribute";
        private readonly SymbolAnalysisContext _context;
        private readonly INamedTypeSymbol _namedTypeSymbol;

        public AnalyzerImpl(SymbolAnalysisContext context, INamedTypeSymbol namedTypeSymbol)
        {
            this._context = context;
            this._namedTypeSymbol = namedTypeSymbol;
        }

        public static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            if (!(context.Symbol is INamedTypeSymbol namedTypeSymbol))
            {
                return;
            }

            if (!IsTargetType(namedTypeSymbol))
            {
                return;
            }

            new AnalyzerImpl(context, namedTypeSymbol).Analyze();
        }

        private static bool IsTargetType(INamedTypeSymbol namedTypeSymbol)
        {
            // Operate on a named type that:
            // - is user-defined
            // - and is a class or a struct
            // - and is not a static class
            // - and has a RecordType attribute
            return !namedTypeSymbol.IsImplicitlyDeclared
                && (namedTypeSymbol.TypeKind == TypeKind.Class || namedTypeSymbol.TypeKind == TypeKind.Struct)
                && !namedTypeSymbol.IsStatic
                && HasRecordTypeAttribute(namedTypeSymbol);

            static bool HasRecordTypeAttribute(INamedTypeSymbol namedTypeSymbol)
            {
                // TODO: Make the attribute name configurable.
                var attribute = namedTypeSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == TargetAttributeName);
                return attribute != null;
            }
        }

        public void Analyze()
        {
            var characteristics = new RecordTypeCharacteristics(_context.Compilation, _namedTypeSymbol);

            _context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.RuleRecordType,
                _namedTypeSymbol.Locations[0]));

            if (characteristics.ImplementsIEquatable1)
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.RuleIEquatable,
                    _namedTypeSymbol.Locations[0]));

                // All elements must be equatable.
            }

            if (characteristics.ImplementsIComparable1)
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.RuleIComparable,
                    _namedTypeSymbol.Locations[0]));

                // All elements must be comparable.
            }

            foreach (var x in characteristics.Elements)
            {
                switch (x)
                {
                    case IFieldSymbol field:
                        Debug.WriteLine("{0} : {1} of type {2}", x.ToDisplayString(), x.Kind, field.Type);
                        break;
                    case IPropertySymbol prop:
                        Debug.WriteLine("{0} : {1} of type {2}", x.ToDisplayString(), x.Kind, prop.Type);
                        break;
                    default:
                        Debug.WriteLine("{0} : {1} (unexpected)", x.ToDisplayString(), x.Kind);
                        break;
                }
            }
        }

        // NOTE: All types are equatable. "Reference equality vs. value equality" is out of our responsibility.

        public bool IsComparable(ITypeSymbol typeSymbol)
        {
            switch (typeSymbol.SpecialType)
            {
                // primitive types
                case SpecialType.System_Enum:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return true;
                case SpecialType.System_Nullable_T:
                    // depends on the underlying type.
                    throw new NotImplementedException();
                case SpecialType.None:
                default:
                    // Just go with IComparable, IComparable<T>.
                    throw new NotImplementedException();
            }
        }

        private sealed class RecordTypeCharacteristics
        {
            public ISymbol[] Elements { get; }
            public bool ImplementsIEquatable1 { get; }
            public bool ImplementsIComparable1 { get; }

            public RecordTypeCharacteristics(Compilation compilation, INamedTypeSymbol namedTypeSymbol)
            {
                // Collect elements: fields and auto-properties that have associated backing fields.
                this.Elements = CollectElements(namedTypeSymbol);

                // Collect interfaces to implement.
                var iequatableSymbol = compilation.GetTypeByMetadataName("System.IEquatable`1");
                var icomparableSymbol = compilation.GetTypeByMetadataName("System.IComparable`1");
                foreach (var x in namedTypeSymbol.Interfaces)
                {
                    if (x.ConstructedFrom == iequatableSymbol)
                    {
                        this.ImplementsIEquatable1 = true;
                    }
                    else if (x.ConstructedFrom == icomparableSymbol)
                    {
                        this.ImplementsIComparable1 = true;
                    }
                }

                static ISymbol[] CollectElements(INamedTypeSymbol namedTypeSymbol)
                {
                    var elements = new List<ISymbol>();
                    foreach (var x in namedTypeSymbol.GetMembers())
                    {
                        if (!(x.Kind == SymbolKind.Field && !x.IsStatic))
                        {
                            continue;
                        }

                        var field = (IFieldSymbol)x;
                        if (field.IsImplicitlyDeclared)
                        {
                            if (!(field.AssociatedSymbol is IPropertySymbol))
                            {
                                // Unknown type of a symbol with a backing field; cannot handle this new language feature.
                                // TODO: Proper error handling
                                throw new Exception();
                            }

                            elements.Add(field.AssociatedSymbol);
                        }
                        else
                        {
                            elements.Add(field);
                        }
                    }
                    return elements.ToArray();
                }
            }
        }
    }
}
