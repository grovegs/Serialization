using GroveGames.Serialization.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GroveGames.Serialization.Generator;

internal static class MigrationParser
{
    private const string MigrationInterface = "GroveGames.Serialization.IMigration<T>";

    public static bool IsCandidate(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    public static MigrationModel? Parse(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol type)
        {
            return null;
        }

        INamedTypeSymbol? migration = null;

        foreach (var candidate in type.AllInterfaces)
        {
            if (candidate.OriginalDefinition.ToDisplayString() == MigrationInterface)
            {
                migration = candidate;
                break;
            }
        }

        if (migration == null)
        {
            return null;
        }

        var diagnostics = new List<DiagnosticInfo>();

        if (type.IsAbstract || type.IsGenericType || !IsAccessible(type) || !HasParameterlessConstructor(type))
        {
            diagnostics.Add(DiagnosticInfo.Create(SchemaDiagnostics.InvalidMigration, type.Locations.FirstOrDefault(), type.ToDisplayString()));
        }

        return new MigrationModel(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            migration.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            GetFromVersion(context, cancellationToken),
            new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    private static int? GetFromVersion(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        foreach (var member in ((ClassDeclarationSyntax)context.Node).Members)
        {
            if (member is not PropertyDeclarationSyntax { Identifier.Text: "FromVersion" } property)
            {
                continue;
            }

            var expression = property.ExpressionBody?.Expression;

            if (expression == null && property.AccessorList != null)
            {
                foreach (var accessor in property.AccessorList.Accessors)
                {
                    if (accessor.Keyword.Text == "get")
                    {
                        expression = accessor.ExpressionBody?.Expression ?? (accessor.Body is { Statements.Count: 1 } body && body.Statements[0] is ReturnStatementSyntax returnStatement ? returnStatement.Expression : null);
                    }
                }
            }

            if (expression != null && context.SemanticModel.GetConstantValue(expression, cancellationToken) is { HasValue: true, Value: int version })
            {
                return version;
            }

            return null;
        }

        return null;
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
