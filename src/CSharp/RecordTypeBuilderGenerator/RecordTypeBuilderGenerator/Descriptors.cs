// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using Microsoft.CodeAnalysis;

namespace RecordTypeBuilderGenerator
{
    internal static class Descriptors
    {
        public const string DiagnosticIdMutableProperty = "RecordTypeBuilder_MutableProperty";
        public const string DiagnosticIdField = "RecordTypeBuilder_Field";
        public const string DiagnosticIdInvalidCtors = "RecordTypeBuilder_Ctors";
        public const string DiagnosticIdGenerateCtors = "RecordTypeBuilder_GenerateCtor";
        public const string DiagnosticIdUpdateCtors = "RecordTypeBuilder_UpdateCtor";
        public const string DiagnosticIdGenerateBuilder = "RecordTypeBuilder_GenerateBuilder";
        public const string DiagnosticIdUpdateBuilder = "RecordTypeBuilder_UpdateBuilder";

        public const string Category = "RecordTypeBuilder.Design";

        public static DiagnosticDescriptor RuleField = new DiagnosticDescriptor(
            DiagnosticIdField, "(TBD)", "Record type have a field {0}.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "(TBD)");
        public static DiagnosticDescriptor RuleMutableProperty = new DiagnosticDescriptor(
            DiagnosticIdMutableProperty, "(TBD)", "Record type have a mutable property {0}.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "(TBD)");
        public static DiagnosticDescriptor RuleInvalidCtors = new DiagnosticDescriptor(
            DiagnosticIdInvalidCtors, "(TBD)", "Record type should not have ctors other than one copy-ctor and one ctor taking parameters matching its properties.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "(TBD)");
        public static DiagnosticDescriptor RuleGenerateCtors = new DiagnosticDescriptor(
            DiagnosticIdGenerateCtors, "(TBD)", "Constructors need to be generated. Use the code fix.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "(TBD)");
        public static DiagnosticDescriptor RuleUpdateCtors = new DiagnosticDescriptor(
            DiagnosticIdUpdateCtors, "(TBD)", "Constructors need to be updated. Use the code fix.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "(TBD)");
        public static DiagnosticDescriptor RuleGenerateBuilder = new DiagnosticDescriptor(
            DiagnosticIdGenerateBuilder, "(TBD)", "Builder needs to be generated. Use the code fix.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "(TBD)");
        public static DiagnosticDescriptor RuleUpdateBuilder = new DiagnosticDescriptor(
            DiagnosticIdUpdateBuilder, "(TBD)", "Builder needs to be updated. Use the code fix.", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "(TBD)");
    }
}
