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
            return Model(ns, type, fullName, version, members, diagnostics);
        }

        if (version < 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.InvalidVersion, location, type.ToDisplayString(), version.ToString()));
        }

        if (!type.IsValueType && (type.IsAbstract || !HasParameterlessConstructor(type)))
        {
            diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.MissingConstructor, location, type.ToDisplayString()));
        }

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
                if (field.IsConst || field.IsReadOnly)
                {
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

            if (TypeShapes.HasRegisteredMember(shape))
            {
                diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.RegisteredMember, member.Locations.FirstOrDefault(), member.ToDisplayString(), memberType.ToDisplayString()));
            }

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

        return Model(ns, type, fullName, version, members, diagnostics);
    }

    private static SchemaTypeModel Model(string ns, INamedTypeSymbol type, string fullName, int version, List<SchemaMemberModel> members, List<DiagnosticInfo> diagnostics)
    {
        return new SchemaTypeModel(ns, type.Name, fullName, type.IsValueType, version, new EquatableArray<SchemaMemberModel>([.. members]), new EquatableArray<DiagnosticInfo>([.. diagnostics]));
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
