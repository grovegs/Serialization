using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GroveGames.Serialization.Generator.Tests;

public sealed class SchemaGeneratorTests
{
    [Fact]
    public void Generate_ValidSchema_EmitsFormatterAndRegistrationThatCompile()
    {
        var result = Run("""
            using System.Collections.Generic;
            using GroveGames.Serialization;

            namespace Game;

            [Schema(version: 2)]
            public sealed class Save
            {
                public string Name;
                public List<Item> Items;
                public Dictionary<string, int> Stats;
                public int? Optional;
            }

            [Schema]
            public struct Item
            {
                public int Id;
            }

            public sealed class SaveMigration : IMigration<Save>
            {
                public int FromVersion => 1;

                public void Apply(DataValue root)
                {
                }
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Empty(result.CompilationErrors);
        Assert.Contains(result.GeneratedSources, source => source.Contains("internal sealed class SaveFormatter"));
        Assert.Contains(result.GeneratedSources, source => source.Contains("[assembly: global::GroveGames.Serialization.FormatterModule(typeof(global::GroveGames.Serialization.TestsFormatterModule))]") && source.Contains("internal sealed class TestsFormatterModule") && source.Contains("new global::Game.SaveMigration()"));
    }

    [Theory]
    [InlineData("public sealed class Outer { [Schema] public sealed class Inner { } }", "GGS001")]
    [InlineData("[Schema] public sealed class Box<T> { }", "GGS001")]
    [InlineData("[Schema] public sealed class Twin { public int Name; public int name; }", "GGS002")]
    [InlineData("[Schema] public sealed class Big { public ulong Value; }", "GGS003")]
    [InlineData("[Schema] public sealed class Grid { public int[,] Cells; }", "GGS003")]
    [InlineData("[Schema] public sealed class Keyed { public System.Collections.Generic.Dictionary<int, int> Map; }", "GGS003")]
    [InlineData("[Schema] public sealed class Frozen { public int Value { get; init; } }", "GGS003")]
    [InlineData("[Schema] public sealed class Identified { public System.Guid Id; }", "GGS004")]
    [InlineData("[Schema] public sealed class Target { } public sealed class Bad : IMigration<Target> { public Bad(int value) { } public int FromVersion => 1; public void Apply(DataValue root) { } }", "GGS005")]
    [InlineData("[Schema] public sealed class NoDefault { public NoDefault(int value) { } }", "GGS006")]
    [InlineData("[Schema(version: 0)] public sealed class Zero { }", "GGS007")]
    public void Generate_InvalidSchema_ReportsDiagnostic(string declarations, string id)
    {
        var result = Run("using GroveGames.Serialization;\nnamespace Game;\n" + declarations);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == id);
    }

    [Fact]
    public void Generate_MigrationForTypeWithoutSchema_IsIgnored()
    {
        var result = Run("""
            using GroveGames.Serialization;

            namespace Game;

            public sealed class Manual
            {
            }

            public sealed class ManualMigration : IMigration<Manual>
            {
                public ManualMigration(int value)
                {
                }

                public int FromVersion => 1;

                public void Apply(DataValue root)
                {
                }
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Empty(result.GeneratedSources);
    }

    private static GeneratorResult Run(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(SchemaAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create(
            "Tests",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SchemaGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        var run = driver.GetRunResult();

        return new GeneratorResult(
            diagnostics,
            output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToImmutableArray(),
            run.GeneratedTrees.Select(tree => tree.ToString()).ToImmutableArray());
    }

    private sealed record GeneratorResult(ImmutableArray<Diagnostic> GeneratorDiagnostics, ImmutableArray<Diagnostic> CompilationErrors, ImmutableArray<string> GeneratedSources);
}
