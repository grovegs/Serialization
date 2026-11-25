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

        var formatters = context.SyntaxProvider.ForAttributeWithMetadataName(
                "GroveGames.Serialization.FormatterAttribute",
                static (node, _) => node is TypeDeclarationSyntax,
                static (context, cancellationToken) => FormatterParser.Parse(context, cancellationToken))
            .Where(static model => model != null);

        context.RegisterSourceOutput(formatters, static (context, model) =>
        {
            foreach (var diagnostic in model!.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });

        var migrations = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => MigrationParser.IsCandidate(node),
                static (context, cancellationToken) => MigrationParser.Parse(context, cancellationToken))
            .Where(static model => model != null);

        var registration = schemas.Collect()
            .Combine(formatters.Collect())
            .Combine(migrations.Collect())
            .Combine(context.CompilationProvider.Select(static (compilation, _) => CreateTarget(compilation)));

        context.RegisterSourceOutput(registration, static (context, data) => Register(context, data.Left.Left.Left, data.Left.Left.Right!, data.Left.Right!, data.Right));
    }

    private static TargetModel CreateTarget(Compilation compilation)
    {
        var alwaysLinkAssembly = compilation.GetTypeByMetadataName("UnityEngine.Scripting.AlwaysLinkAssemblyAttribute");
        var isAlwaysLinked = false;

        if (alwaysLinkAssembly != null)
        {
            foreach (var attribute in compilation.Assembly.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, alwaysLinkAssembly))
                {
                    isAlwaysLinked = true;
                    break;
                }
            }
        }

        return new TargetModel(
            compilation.AssemblyName ?? "Assembly",
            compilation.GetTypeByMetadataName("UnityEngine.RuntimeInitializeOnLoadMethodAttribute") != null,
            compilation.GetTypeByMetadataName("UnityEditor.InitializeOnLoadMethodAttribute") != null,
            compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") != null,
            alwaysLinkAssembly != null && !isAlwaysLinked,
            compilation.GetTypeByMetadataName("UnityEngine.Scripting.PreserveAttribute") != null);
    }

    private static void Register(SourceProductionContext context, ImmutableArray<SchemaTypeModel> schemas, ImmutableArray<FormatterModel?> formatters, ImmutableArray<MigrationModel?> migrations, TargetModel target)
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

        var validFormatters = new List<FormatterModel>();

        foreach (var formatter in formatters)
        {
            if (formatter is { IsValid: true })
            {
                validFormatters.Add(formatter);
                schemaNames.Add(formatter.TargetFullName);
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

        ReportMissingMigrations(context, validSchemas, validMigrations);

        if (validSchemas.Count == 0 && validFormatters.Count == 0)
        {
            return;
        }

        validSchemas.Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));
        validFormatters.Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));
        validMigrations.Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));
        context.AddSource("FormatterRegistration.g.cs", RegistrationEmitter.Emit(target, validSchemas, validFormatters, validMigrations));
    }

    private static void ReportMissingMigrations(SourceProductionContext context, List<SchemaTypeModel> schemas, List<MigrationModel> migrations)
    {
        foreach (var schema in schemas)
        {
            if (schema.Version <= 1)
            {
                continue;
            }

            var covered = new bool[schema.Version];
            var isKnown = true;

            foreach (var migration in migrations)
            {
                if (migration.TargetFullName != schema.FullName)
                {
                    continue;
                }

                if (migration.FromVersion is not int from)
                {
                    isKnown = false;
                    break;
                }

                if (from >= 1 && from < schema.Version)
                {
                    covered[from] = true;
                }
            }

            if (!isKnown)
            {
                continue;
            }

            for (var version = 1; version < schema.Version; version++)
            {
                if (!covered[version])
                {
                    context.ReportDiagnostic(Diagnostic.Create(SchemaDiagnostics.MissingMigration, schema.Location?.ToLocation(), schema.FullName.Replace("global::", string.Empty), schema.Version, version));
                }
            }
        }
    }

    private static string HintName(SchemaTypeModel model)
    {
        return model.FullName.Replace("global::", string.Empty).Replace('<', '_').Replace('>', '_') + "Formatter.g.cs";
    }
}
