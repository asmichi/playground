// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using Microsoft.CodeAnalysis;

namespace Asmichi.RecordTypeGenerator
{
    internal static class Descriptors
    {
        public const string DiagnosticIdImplementRecordType = "RecordTypeGenerator_ImplementRecordType";
        public const string DiagnosticIdImplementIEquatable = "RecordTypeGenerator_ImplementIEquatable";
        public const string DiagnosticIdImplementIComparable = "RecordTypeGenerator_ImplementIComparable";

        public const string Category = "RecordTypeGenerator.Design";

        public static DiagnosticDescriptor RuleRecordType = new DiagnosticDescriptor(
            DiagnosticIdImplementRecordType,
            "Implement RecordType",
            "Provide RecordType implementation: a ctor taking all elements, Deconstruct and ToString.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "(TBD)");
        public static DiagnosticDescriptor RuleIEquatable = new DiagnosticDescriptor(
            DiagnosticIdImplementIEquatable,
            "Implement IEquatable<T>",
            "Provide IEquatable<T> implentation.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "(TBD)");
        public static DiagnosticDescriptor RuleIComparable = new DiagnosticDescriptor(
            DiagnosticIdImplementIEquatable,
            "Implement IComparable<T>",
            "Provide IComparable<T> implentation.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "(TBD)");
    }
}
