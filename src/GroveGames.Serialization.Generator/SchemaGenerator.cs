using System.Collections.Immutable;
using GroveGames.Serialization.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GroveGames.Serialization.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class SchemaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var schemas = context.SyntaxProvider.ForAttributeWithMetadataName(
            "GroveGames.Serialization.SchemaAttribute",
            static (node, _) => node is TypeDeclarationSyntax,
            static (context, cancellationToken) => SchemaTypeParser.Parse(context, cancellationToken));

        context.RegisterSourceOutput(schemas, static (context, model) =>
        {
            foreach (var diagnostic in model.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            if (model.IsValid)
            {
                context.AddSource(HintName(model), FormatterEmitter.Emit(model));
            }
        });

        var migrations = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => MigrationParser.IsCandidate(node),
                static (context, cancellationToken) => MigrationParser.Parse(context, cancellationToken))
            .Where(static model => model != null);

        var registration = schemas.Collect()
            .Combine(migrations.Collect())
            .Combine(context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? "Assembly"));

        context.RegisterSourceOutput(registration, static (context, data) => Register(context, data.Left.Left, data.Left.Right!, data.Right));
    }

    private static void Register(SourceProductionContext context, ImmutableArray<SchemaTypeModel> schemas, ImmutableArray<MigrationModel?> migrations, string assemblyName)
    {
        var validSchemas = new List<SchemaTypeModel>();
        var schemaNames = new HashSet<string>();

        foreach (var schema in schemas)
        {
            if (schema.IsValid)
            {
                validSchemas.Add(schema);
                schemaNames.Add(schema.FullName);
            }
        }

        var validMigrations = new List<MigrationModel>();

        foreach (var migration in migrations)
        {
            if (migration == null)
            {
                continue;
            }

            if (!schemaNames.Contains(migration.TargetFullName))
            {
                continue;
            }

            foreach (var diagnostic in migration.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            if (migration.Diagnostics.Count > 0)
            {
                continue;
            }

            validMigrations.Add(migration);
        }

        if (validSchemas.Count == 0)
        {
            return;
        }

        validSchemas.Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));
        validMigrations.Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));
        context.AddSource("FormatterRegistryBuilderExtensions.g.cs", RegistrationEmitter.Emit(assemblyName, validSchemas, validMigrations));
    }

    private static string HintName(SchemaTypeModel model)
    {
        return model.FullName.Replace("global::", string.Empty).Replace('<', '_').Replace('>', '_') + "Formatter.g.cs";
    }
}
