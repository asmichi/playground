// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RecordTypeBuilderGenerator
{
    // Note: In order to avoid runtime dependency on this extension, the RecordType attribute should be defined in the assembly using this extension.
    //
    // TODO: Should be able to operate on derived types though IMO in many cases composition will be preferred.
    // TODO: Configuration via properties of the RecordType attribute.

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RecordTypeBuilderGeneratorAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                Descriptors.RuleField,
                Descriptors.RuleMutableProperty,
                Descriptors.RuleInvalidCtors,
                Descriptors.RuleGenerateCtors,
                Descriptors.RuleUpdateCtors,
                Descriptors.RuleGenerateBuilder,
                Descriptors.RuleUpdateBuilder
            );

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Load configurations at the beginning of compilation.
            // context.RegisterCompilationStartAction()

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            new Analyzer(context).Analyze();
        }
    }

    internal class Analyzer
    {
        private SymbolAnalysisContext _context;

        public Analyzer(SymbolAnalysisContext context)
        {
            this._context = context;
        }

        public void Analyze()
        {
            // Operate on a named type that:
            var namedTypeSymbol = (INamedTypeSymbol)_context.Symbol;

            // - is a class or a struct
            if (!(namedTypeSymbol.IsReferenceType || namedTypeSymbol.IsValueType))
            {
                return;
            }

            // - and has a RecordType attribute
            // TODO: Make the attribute name configurable.
            var attribute = namedTypeSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "RecordTypeAttribute");
            if (attribute == null)
            {
                return;
            }

            var explicitInstanceMembers = namedTypeSymbol.GetMembers().Where(x => !x.IsStatic && !x.IsImplicitlyDeclared).ToArray();
            var readOnlyProperties = explicitInstanceMembers.OfType<IPropertySymbol>().Where(x => x.IsReadOnly).ToArray();

            CheckNoField(explicitInstanceMembers);
            CheckImmutablity(explicitInstanceMembers);
            CheckConstrutors(namedTypeSymbol, readOnlyProperties);
            CheckBuilderType(namedTypeSymbol, readOnlyProperties);
        }

        /// <summary>
        /// Check if the type has no field.
        /// </summary>
        private void CheckNoField(IReadOnlyCollection<ISymbol> explicitInstanceMembers)
        {
            foreach (var field in explicitInstanceMembers.OfType<IFieldSymbol>())
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.RuleField,
                    field.DeclaringSyntaxReferences.First().GetSyntax(_context.CancellationToken).GetLocation(),
                    field.Name));
            }
        }

        /// <summary>
        /// Check if all properties are read-only.
        /// </summary>
        private void CheckImmutablity(IReadOnlyCollection<ISymbol> explicitInstanceMembers)
        {
            var properties = explicitInstanceMembers.OfType<IPropertySymbol>();
            foreach (var mutableProperty in properties.Where(x => !x.IsReadOnly))
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.RuleMutableProperty,
                    mutableProperty.DeclaringSyntaxReferences.First().GetSyntax(_context.CancellationToken).GetLocation(),
                    mutableProperty.Name));
            }
        }

        /// <summary>
        /// Check if the type has proper ctors.
        /// </summary>
        private void CheckConstrutors(INamedTypeSymbol namedTypeSymbol, IReadOnlyCollection<IPropertySymbol> readOnlyProperties)
        {
            // Not generated yet (default ctor)
            if (namedTypeSymbol.Constructors.All(x => x.IsImplicitlyDeclared))
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.RuleGenerateCtors,
                    GetLocation(namedTypeSymbol, _context.CancellationToken)));
                return;
            }

            // The type should have two ctors.
            if (namedTypeSymbol.Constructors.Length != 2)
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.RuleInvalidCtors,
                    namedTypeSymbol.DeclaringSyntaxReferences.First().GetSyntax(_context.CancellationToken).GetLocation()));
                return;
            }

            // The ctor should have as many parameters as properties or be a copy-ctor.
            foreach (var ctor in namedTypeSymbol.Constructors)
            {
                bool isUpToDate = IsCopyConstructor(ctor, namedTypeSymbol)
                    ? IsCopyConstructorUpToDate(_context, ctor, namedTypeSymbol, readOnlyProperties)
                    : IsConstructorTakingPropertyValuesUpToDate(_context, ctor, readOnlyProperties);

                if (!isUpToDate)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.RuleUpdateCtors,
                        GetLocation(ctor, _context.CancellationToken)));
                }
            }
        }

        private static bool IsConstructorTakingPropertyValuesUpToDate(SymbolAnalysisContext context, IMethodSymbol ctor, IReadOnlyCollection<IPropertySymbol> readOnlyProperties)
        {
            // The types and names of the parameters should match those of the properties.
            if (ctor.Parameters.Length != readOnlyProperties.Count)
            {
                return false;
            }

            bool parametersMatchProperties = ctor.Parameters.Zip(readOnlyProperties,
                (parameter, property) => parameter.Type.Equals(property.Type) && parameter.Name == Utils.MakeInitialLowerString(property.Name))
                .All(x => x);
            if (!parametersMatchProperties)
            {
                return false;
            }

            // The ctor should have proper assignments.
            var assignments = GetAssignmentsInMethod(ctor, context.CancellationToken).ToArray();
            if (readOnlyProperties.Count != assignments.Length)
            {
                return false;
            }

            bool assignmentsMatchProperties = assignments.Zip(readOnlyProperties,
                (assignment, property) => IsAssignmentForConstructorTakingPropertyValues(assignment, property))
                .All(x => x);
            if (!assignmentsMatchProperties)
            {
                return false;
            }

            return true;
        }

        private static bool IsAssignmentForConstructorTakingPropertyValues(AssignmentExpressionSyntax assignment, IPropertySymbol property)
        {
            return GetIdenfitierNameOrDefault(assignment.Left) == property.Name
                && GetIdenfitierNameOrDefault(assignment.Right) == Utils.MakeInitialLowerString(property.Name);
        }

        private static bool IsCopyConstructorUpToDate(SymbolAnalysisContext context, IMethodSymbol ctor, INamedTypeSymbol namedTypeSymbol, IReadOnlyCollection<IPropertySymbol> readOnlyProperties)
        {
            Debug.Assert(IsCopyConstructor(ctor, namedTypeSymbol));

            // The ctor should take one parameter named `other`
            if (ctor.Parameters.Single().Name != "other")
            {
                return false;
            }

            // The ctor should have proper assignments.
            var assignments = GetAssignmentsInMethod(ctor, context.CancellationToken).ToArray();
            if (readOnlyProperties.Count != assignments.Length)
            {
                return false;
            }

            bool assignmentsMatchProperties = assignments.Zip(readOnlyProperties,
                (assignment, property) => IsAssignmentForCopyConstructor(assignment, property))
                .All(x => x);
            if (!assignmentsMatchProperties)
            {
                return false;
            }

            return true;
        }

        private static bool IsAssignmentForCopyConstructor(AssignmentExpressionSyntax assignment, IPropertySymbol property)
        {
            var memberAccess = assignment.Right as MemberAccessExpressionSyntax;
            if (memberAccess == null)
            {
                return false;
            }

            return GetIdenfitierNameOrDefault(assignment.Left) == property.Name
                && GetIdenfitierNameOrDefault(memberAccess.Expression) == "other"
                && GetIdenfitierNameOrDefault(memberAccess.Name) == property.Name;
        }

        /// <summary>
        /// Check if the type has a proper builder type.
        /// TODO: Make the builder name configurable.
        /// </summary>
        private void CheckBuilderType(INamedTypeSymbol namedTypeSymbol, IReadOnlyCollection<IPropertySymbol> readOnlyProperties)
        {
            var builderType = namedTypeSymbol.GetTypeMembers().FirstOrDefault(x => x?.Name == "Builder");
            if (builderType == null)
            {
                //// No builder type.
                //context.ReportDiagnostic(Diagnostic.Create(
                //    RuleGenerateBuilder,
                //    GetLocation(namedTypeSymbol, context.CancellationToken)));
            }
            else
            {
                // Check the ctor taking the record type.
                // Check the Build method.
            }
        }

        private static IEnumerable<AssignmentExpressionSyntax> GetAssignmentsInMethod(IMethodSymbol ctor, CancellationToken cancellationToken)
        {
            var syntax = ctor.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken);
            return syntax.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        }

        private static Location GetLocation(ISymbol symbol, CancellationToken cancellationToken)
        {
            return symbol.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken).GetLocation();
        }

        private static bool IsCopyConstructor(IMethodSymbol ctor, INamedTypeSymbol type)
        {
            return ctor.Parameters.Length == 1 && ctor.Parameters.Single().Type.Equals(type);
        }

        private static string GetIdenfitierNameOrDefault(ExpressionSyntax expr)
        {
            return (expr as IdentifierNameSyntax)?.Identifier.ValueText;
        }
    }
}
