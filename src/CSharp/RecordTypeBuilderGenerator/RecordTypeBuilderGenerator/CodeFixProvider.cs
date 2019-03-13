// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RecordTypeBuilderGenerator
{
    // TODO:
    // - What should we do if the order of properties changes?

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RecordTypeBuilderGeneratorCodeFixProvider)), Shared]
    public class RecordTypeBuilderGeneratorCodeFixProvider : CodeFixProvider
    {
        private const string title = "RecordTypeBuilderGenerator";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.DiagnosticIdGenerateCtors,
                Descriptors.DiagnosticIdUpdateCtors
                //RecordTypeBuilderGeneratorAnalyzer.DiagnosticIdGenerateBuilder,
                //RecordTypeBuilderGeneratorAnalyzer.DiagnosticIdUpdateBuilder
            );

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the type declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
                // Register a code action that will invoke the fix.
                switch (diagnostic.Id)
                {
                    case Descriptors.DiagnosticIdGenerateCtors:
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Generate ctors",
                                createChangedDocument: c => GenerateCtors(context.Document, semanticModel, declaration, c),
                                equivalenceKey: Descriptors.DiagnosticIdGenerateCtors),
                            diagnostic);
                        break;
                    case Descriptors.DiagnosticIdUpdateCtors:
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Update ctors",
                                createChangedDocument: c => UpdateCtors(context.Document, semanticModel, declaration, c),
                                equivalenceKey: Descriptors.DiagnosticIdUpdateCtors),
                            diagnostic);
                        break;
                }
            }
        }

        private async Task<Document> GenerateCtors(Document document, SemanticModel semanticModel, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var namedTypeSymbol = semanticModel.GetDeclaredSymbol(typeDecl);
            var adaptor = new StructOrClassDeclarationSyntaxAdaptor(typeDecl);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newCtor1 = GenerateCtorSyntax(typeDecl, namedTypeSymbol, cancellationToken);
            var newCtor2 = GenerateCopyCtorSyntax(typeDecl, namedTypeSymbol, cancellationToken);
            var newTypeDecl = adaptor.AddMembers(newCtor1, newCtor2).InnerTypeDeclarationSyntax;
            var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> UpdateCtors(Document document, SemanticModel semanticModel, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var namedTypeSymbol = semanticModel.GetDeclaredSymbol(typeDecl);
            var adaptor = new StructOrClassDeclarationSyntaxAdaptor(typeDecl);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newCtor1 = GenerateCtorSyntax(typeDecl, namedTypeSymbol, cancellationToken);
            var oldCtor1 = adaptor.Members.OfType<ConstructorDeclarationSyntax>().ElementAt(0);
            var newMembers1 = adaptor.Members.Replace(oldCtor1, newCtor1);

            var newCtor2 = GenerateCopyCtorSyntax(typeDecl, namedTypeSymbol, cancellationToken);
            var oldCtor2 = newMembers1.OfType<ConstructorDeclarationSyntax>().ElementAt(1);
            var newMembers2 = newMembers1.Replace(oldCtor2, newCtor2);

            var newTypeDecl = adaptor.WithMembers(newMembers2).InnerTypeDeclarationSyntax;
            var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

            return document.WithSyntaxRoot(newRoot);
        }

        private ConstructorDeclarationSyntax GenerateCtorSyntax(TypeDeclarationSyntax typeDecl, INamedTypeSymbol namedTypeSymbol, CancellationToken cancellationToken)
        {
            var explicitInstanceMembers = namedTypeSymbol.GetMembers().Where(x => !x.IsStatic && !x.IsImplicitlyDeclared).ToArray();
            var readOnlyProperties = explicitInstanceMembers.OfType<IPropertySymbol>().Where(x => x.IsReadOnly).ToArray();
            var readOnlyPropertiesSyntax = readOnlyProperties.Select(x => (PropertyDeclarationSyntax)x.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken)).ToArray();

            var parameters = SyntaxFactory.ParameterList().AddParameters(
                readOnlyPropertiesSyntax.Select(x => MakeParameterSyntaxFromProperty(x)).ToArray());

            var body = SyntaxFactory.Block().AddStatements(
                readOnlyProperties.Select(x =>
                    MakePropertyAssignmentStatement(x)).ToArray());

            return SyntaxFactory.ConstructorDeclaration(namedTypeSymbol.Name)
                .WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(parameters)
                .WithBody(body);
        }

        private static ParameterSyntax MakeParameterSyntaxFromProperty(PropertyDeclarationSyntax x)
        {
            return SyntaxFactory.Parameter(
                default(SyntaxList<AttributeListSyntax>),
                default(SyntaxTokenList),
                x.Type,
                SyntaxFactory.Identifier(Utils.MakeInitialLowerString(x.Identifier.ValueText)),
                default(EqualsValueClauseSyntax));
        }

        private static ExpressionStatementSyntax MakePropertyAssignmentStatement(IPropertySymbol x)
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(x.Name),
                    SyntaxFactory.IdentifierName(Utils.MakeInitialLowerString(x.Name))));
        }

        private ConstructorDeclarationSyntax GenerateCopyCtorSyntax(TypeDeclarationSyntax typeDecl, INamedTypeSymbol namedTypeSymbol, CancellationToken cancellationToken)
        {
            var explicitInstanceMembers = namedTypeSymbol.GetMembers().Where(x => !x.IsStatic && !x.IsImplicitlyDeclared).ToArray();
            var readOnlyProperties = explicitInstanceMembers.OfType<IPropertySymbol>().Where(x => x.IsReadOnly).ToArray();
            var readOnlyPropertiesSyntax = readOnlyProperties.Select(x => (PropertyDeclarationSyntax)x.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken)).ToArray();

            var parameters = SyntaxFactory.ParameterList().AddParameters(
                MakeParameterSyntaxForCopyCtor(typeDecl));

            var body = SyntaxFactory.Block().AddStatements(
                readOnlyProperties.Select(x =>
                    MakePropertyAssignmentStatementForCopyCtor(x)).ToArray());

            return SyntaxFactory.ConstructorDeclaration(namedTypeSymbol.Name)
                .WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(parameters)
                .WithBody(body);
        }

        private static ParameterSyntax MakeParameterSyntaxForCopyCtor(TypeDeclarationSyntax x)
        {
            return SyntaxFactory.Parameter(
                default(SyntaxList<AttributeListSyntax>),
                default(SyntaxTokenList),
                SyntaxFactory.ParseTypeName(x.Identifier.ValueText),
                SyntaxFactory.Identifier("other"),
                default(EqualsValueClauseSyntax));
        }

        private static ExpressionStatementSyntax MakePropertyAssignmentStatementForCopyCtor(IPropertySymbol x)
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(x.Name),
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, 
                        SyntaxFactory.IdentifierName("other"),
                        SyntaxFactory.IdentifierName(x.Name))));
        }

        private class StructOrClassDeclarationSyntaxAdaptor
        {
            private TypeDeclarationSyntax _typeDecl;
            private bool _isClass;

            public StructOrClassDeclarationSyntaxAdaptor(TypeDeclarationSyntax typeDecl)
            {
                _typeDecl = typeDecl;
                if (typeDecl is ClassDeclarationSyntax)
                {
                    _isClass = true;
                }
                else if (typeDecl is StructDeclarationSyntax)
                {
                    _isClass = false;
                }
                else
                {
                    throw new ArgumentException(nameof(typeDecl));
                }
            }

            public StructOrClassDeclarationSyntaxAdaptor(ClassDeclarationSyntax classDecl)
            {
                _isClass = true;
                _typeDecl = classDecl;
            }

            public StructOrClassDeclarationSyntaxAdaptor(StructDeclarationSyntax structDecl)
            {
                _isClass = false;
                _typeDecl = structDecl;
            }

            public TypeDeclarationSyntax InnerTypeDeclarationSyntax => _typeDecl;

            public SyntaxList<MemberDeclarationSyntax> Members =>
                _isClass
                ? ((ClassDeclarationSyntax)_typeDecl).Members
                : ((StructDeclarationSyntax)_typeDecl).Members;

            public StructOrClassDeclarationSyntaxAdaptor AddMembers(params MemberDeclarationSyntax[] items) => 
                _isClass
                ? new StructOrClassDeclarationSyntaxAdaptor(((ClassDeclarationSyntax)_typeDecl).AddMembers(items))
                : new StructOrClassDeclarationSyntaxAdaptor(((StructDeclarationSyntax)_typeDecl).AddMembers(items));

            public StructOrClassDeclarationSyntaxAdaptor WithMembers(SyntaxList<MemberDeclarationSyntax> members) =>
                _isClass
                ? new StructOrClassDeclarationSyntaxAdaptor(((ClassDeclarationSyntax)_typeDecl).WithMembers(members))
                : new StructOrClassDeclarationSyntaxAdaptor(((StructDeclarationSyntax)_typeDecl).WithMembers(members));
        }
    }
}
