using GroveGames.Serialization.Generator.Models;
using Microsoft.CodeAnalysis;

namespace GroveGames.Serialization.Generator;

internal static class FormatterParser
{
    private const string FormatterInterface = "GroveGames.Serialization.IFormatter<T>";

    public static FormatterModel? Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not INamedTypeSymbol type)
        {
            return null;
        }

        var location = type.Locations.FirstOrDefault();
        var name = type.ToDisplayString();
        var diagnostics = new List<DiagnosticInfo>();
        INamedTypeSymbol? formatter = null;
        var count = 0;

        foreach (var candidate in type.AllInterfaces)
        {
            if (candidate.OriginalDefinition.ToDisplayString() == FormatterInterface)
            {
                formatter = candidate;
                count++;
            }
        }

        if (count != 1 || type.IsAbstract || type.IsGenericType || !IsAccessible(type) || !HasParameterlessConstructor(type))
        {
            diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.InvalidFormatter, location, name));
        }

        var version = 1;
        var attribute = context.Attributes[0];

        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is int value)
        {
            version = value;
        }

        if (version < 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.InvalidVersion, location, name, version.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        return new FormatterModel(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            formatter?.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty,
            version,
            new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    private static bool IsAccessible(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                return false;
            }
        }

        return true;
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
}
