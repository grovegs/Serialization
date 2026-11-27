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
        var versions = new Dictionary<string, int>();
        var hasConflict = false;

        foreach (var schema in validSchemas)
        {
            versions[schema.FullName] = schema.Version;
        }

        foreach (var formatter in formatters)
        {
            if (formatter is not { IsValid: true })
            {
                continue;
            }

            // A second formatter for one type, including the list formatter generated for every schema, fails the whole registration.
            if (versions.ContainsKey(formatter.TargetFullName) || IsGeneratedList(formatter.TargetFullName, versions))
            {
                context.ReportDiagnostic(Diagnostic.Create(SchemaDiagnostics.DuplicateFormatter, null, formatter.TargetFullName.Replace("global::", string.Empty)));
                hasConflict = true;
                continue;
            }

            validFormatters.Add(formatter);
            schemaNames.Add(formatter.TargetFullName);
            versions[formatter.TargetFullName] = formatter.Version;
        }

        ReportMissingFormatters(context, validSchemas, versions);

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

        hasConflict |= ReportMigrationConflicts(context, validMigrations, versions);
        ReportMissingMigrations(context, versions, validMigrations, migrations);

        if (hasConflict)
        {
            return;
        }

        if (validSchemas.Count == 0 && validFormatters.Count == 0)
        {
            return;
        }

        validSchemas.Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));
        validFormatters.Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));
        validMigrations.Sort(static (left, right) => string.CompareOrdinal(left.FullName, right.FullName));
        context.AddSource("FormatterRegistration.g.cs", RegistrationEmitter.Emit(target, validSchemas, validFormatters, validMigrations));
    }

    private static bool IsGeneratedList(string target, Dictionary<string, int> versions)
    {
        const string ListPrefix = "global::System.Collections.Generic.List<";
        return target.StartsWith(ListPrefix, StringComparison.Ordinal) && target.EndsWith(">", StringComparison.Ordinal) && versions.ContainsKey(target.Substring(ListPrefix.Length, target.Length - ListPrefix.Length - 1));
    }

    private static void ReportMissingFormatters(SourceProductionContext context, List<SchemaTypeModel> schemas, Dictionary<string, int> versions)
    {
        foreach (var schema in schemas)
        {
            foreach (var member in schema.RegisteredMembers)
            {
                if (!versions.ContainsKey(member.TypeName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(SchemaDiagnostics.RegisteredMember, member.Location?.ToLocation(), member.MemberName, member.TypeName.Replace("global::", string.Empty)));
                }
            }
        }
    }

    private static bool ReportMigrationConflicts(SourceProductionContext context, List<MigrationModel> migrations, Dictionary<string, int> versions)
    {
        var hasConflict = false;
        var seen = new HashSet<(string, int)>();

        foreach (var migration in migrations)
        {
            if (migration.FromVersion is not int from || !versions.TryGetValue(migration.TargetFullName, out var version))
            {
                continue;
            }

            var target = migration.TargetFullName.Replace("global::", string.Empty);

            if (from < 1 || from >= version)
            {
                context.ReportDiagnostic(Diagnostic.Create(SchemaDiagnostics.MigrationOutOfRange, null, migration.FullName.Replace("global::", string.Empty), target, from, version));
                hasConflict = true;
            }
            else if (!seen.Add((migration.TargetFullName, from)))
            {
                context.ReportDiagnostic(Diagnostic.Create(SchemaDiagnostics.DuplicateMigration, null, target, from));
                hasConflict = true;
            }
        }

        return hasConflict;
    }

    private static void ReportMissingMigrations(SourceProductionContext context, Dictionary<string, int> versions, List<MigrationModel> migrations, ImmutableArray<MigrationModel?> allMigrations)
    {
        foreach (var pair in versions)
        {
            if (pair.Value <= 1)
            {
                continue;
            }

            var covered = new bool[pair.Value];
            var isKnown = true;

            foreach (var migration in allMigrations)
            {
                if (migration == null || migration.TargetFullName != pair.Key)
                {
                    continue;
                }

                if (migration.FromVersion is not int from)
                {
                    isKnown = false;
                    break;
                }

                // An invalid migration has already been reported, so the step it was meant to cover is not reported again.
                if (from >= 1 && from < pair.Value)
                {
                    covered[from] = true;
                }
            }

            if (!isKnown)
            {
                continue;
            }

            for (var version = 1; version < pair.Value; version++)
            {
                if (!covered[version])
                {
                    context.ReportDiagnostic(Diagnostic.Create(SchemaDiagnostics.MissingMigration, null, pair.Key.Replace("global::", string.Empty), pair.Value, version));
                }
            }
        }
    }

    private static string HintName(SchemaTypeModel model)
    {
        return model.FullName.Replace("global::", string.Empty).Replace('<', '_').Replace('>', '_') + "Formatter.g.cs";
    }
}
