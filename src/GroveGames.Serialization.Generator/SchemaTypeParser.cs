using GroveGames.Serialization.Generator.Models;
using Microsoft.CodeAnalysis;

namespace GroveGames.Serialization.Generator;

internal static class SchemaTypeParser
{
    private const string IgnoreAttribute = "GroveGames.Serialization.IgnoreAttribute";

    public static SchemaTypeModel Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var compilation = context.SemanticModel.Compilation;
        var location = type.Locations.FirstOrDefault();
        var diagnostics = new List<DiagnosticInfo>();
        var members = new List<SchemaMemberModel>();
        var registered = new List<RegisteredMemberModel>();
        var ns = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
        var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var version = 1;

        if (context.Attributes[0].ConstructorArguments.Length > 0 && context.Attributes[0].ConstructorArguments[0].Value is int argument)
        {
            version = argument;
        }

        if (type.ContainingType != null || type.IsGenericType)
        {
            diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.UnsupportedType, location, type.ToDisplayString()));
            return Model(ns, type, fullName, version, members, diagnostics, registered);
        }

        if (version < 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.InvalidVersion, location, type.ToDisplayString(), version.ToString()));
        }

        if (!type.IsValueType && (type.IsAbstract || !HasParameterlessConstructor(type)))
        {
            diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.MissingConstructor, location, type.ToDisplayString()));
        }

        ReportInheritedMembers(type, location, diagnostics);

        foreach (var member in type.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member.IsStatic || member.IsImplicitlyDeclared || member.DeclaredAccessibility != Accessibility.Public || IsIgnored(member))
            {
                continue;
            }

            ITypeSymbol memberType;

            if (member is IFieldSymbol field)
            {
                if (field.IsConst)
                {
                    continue;
                }

                if (field.IsReadOnly)
                {
                    diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.MemberNotSaved, member.Locations.FirstOrDefault(), member.ToDisplayString(), "readonly fields cannot be read back; remove readonly or mark it [Ignore]"));
                    continue;
                }

                memberType = field.Type;
            }
            else if (member is IPropertySymbol property)
            {
                if (property.IsIndexer || property.GetMethod == null || property.GetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (property.SetMethod == null || property.SetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.MemberNotSaved, member.Locations.FirstOrDefault(), member.ToDisplayString(), "it has no public setter; add one or mark it [Ignore]"));
                    continue;
                }

                if (property.SetMethod.IsInitOnly)
                {
                    diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.UnsupportedMember, member.Locations.FirstOrDefault(), member.ToDisplayString(), "init-only properties are not supported"));
                    continue;
                }

                memberType = property.Type;
            }
            else
            {
                continue;
            }

            var shape = TypeShapes.Create(memberType, compilation, out var error);

            if (shape == null)
            {
                diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.UnsupportedMember, member.Locations.FirstOrDefault(), member.ToDisplayString(), error ?? "unsupported type"));
                continue;
            }

            AddRegisteredTypes(member, memberType, registered);
            ReportNestedVersion(member, memberType, diagnostics);

            var fieldName = FieldNames.ToCamelCase(member.Name);

            foreach (var existing in members)
            {
                if (existing.FieldName == fieldName)
                {
                    diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.DuplicateFieldName, member.Locations.FirstOrDefault(), type.ToDisplayString(), fieldName));
                }
            }

            members.Add(new SchemaMemberModel(member.Name, fieldName, shape));
        }

        return Model(ns, type, fullName, version, members, diagnostics, registered);
    }

    private static SchemaTypeModel Model(string ns, INamedTypeSymbol type, string fullName, int version, List<SchemaMemberModel> members, List<DiagnosticInfo> diagnostics, List<RegisteredMemberModel> registered)
    {
        return new SchemaTypeModel(ns, type.Name, fullName, type.IsValueType, version, new EquatableArray<SchemaMemberModel>([.. members]), new EquatableArray<DiagnosticInfo>([.. diagnostics]), LocationInfo.From(type.Locations.FirstOrDefault()), new EquatableArray<RegisteredMemberModel>([.. registered]));
    }

    private static void ReportInheritedMembers(INamedTypeSymbol type, Location? location, List<DiagnosticInfo> diagnostics)
    {
        for (var baseType = type.BaseType; baseType != null && baseType.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType); baseType = baseType.BaseType)
        {
            foreach (var member in baseType.GetMembers())
            {
                if (!member.IsStatic && !member.IsImplicitlyDeclared && member.DeclaredAccessibility == Accessibility.Public && member is IFieldSymbol or IPropertySymbol { IsIndexer: false } && !IsIgnored(member) && type.GetMembers(member.Name).IsEmpty)
                {
                    diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.MemberNotSaved, location, member.ToDisplayString(), $"members inherited from '{baseType.ToDisplayString()}' are not serialized; declare them on '{type.Name}'"));
                    return;
                }
            }
        }
    }

    // Types from packages that register their own formatters, and other assemblies' schema types, need no formatter here.
    private static void AddRegisteredTypes(ISymbol member, ITypeSymbol type, List<RegisteredMemberModel> registered)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                AddRegisteredTypes(member, array.ElementType, registered);
                return;
            case INamedTypeSymbol named when named.IsGenericType:
                foreach (var argument in named.TypeArguments)
                {
                    AddRegisteredTypes(member, argument, registered);
                }

                return;
            case INamedTypeSymbol named when named.SpecialType == SpecialType.None && named.TypeKind is TypeKind.Class or TypeKind.Struct && !TypeShapes.HasSchema(named) && !IsKnownFormatterNamespace(named):
                registered.Add(new RegisteredMemberModel(member.ToDisplayString(), named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), LocationInfo.From(member.Locations.FirstOrDefault())));
                return;
        }
    }

    private static bool IsKnownFormatterNamespace(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace.ToDisplayString();

        if (ns == "System")
        {
            return type.Name is "DateTime" or "DateTimeOffset" or "TimeSpan" or "Guid";
        }

        return ns.StartsWith("UnityEngine", StringComparison.Ordinal) || ns.StartsWith("Unity.Mathematics", StringComparison.Ordinal) || ns.StartsWith("GroveGames.", StringComparison.Ordinal);
    }

    private static void ReportNestedVersion(ISymbol member, ITypeSymbol type, List<DiagnosticInfo> diagnostics)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                ReportNestedVersion(member, array.ElementType, diagnostics);
                return;
            case INamedTypeSymbol named when named.IsGenericType:
                foreach (var argument in named.TypeArguments)
                {
                    ReportNestedVersion(member, argument, diagnostics);
                }

                return;
            case INamedTypeSymbol named:
                var version = SchemaVersion(named);

                if (version > 1)
                {
                    diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.NestedVersion, member.Locations.FirstOrDefault(), member.ToDisplayString(), named.ToDisplayString(), version.ToString()));
                }

                return;
        }
    }

    private static int SchemaVersion(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "GroveGames.Serialization.SchemaAttribute")
            {
                return attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is int version ? version : 1;
            }
        }

        return 0;
    }

    private static bool HasParameterlessConstructor(INamedTypeSymbol type)
    {
        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsIgnored(ISymbol member)
    {
        foreach (var attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == IgnoreAttribute)
            {
                return true;
            }
        }

        return false;
    }
}
