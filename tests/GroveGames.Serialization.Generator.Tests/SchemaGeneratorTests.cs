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
        Assert.Contains(result.GeneratedSources, source =>
            source.Contains("internal static class TestsFormatterRegistration")
            && source.Contains("[global::System.Runtime.CompilerServices.ModuleInitializer]")
            && source.Contains("new global::Game.SaveMigration()"));
    }

    [Fact]
    public void Generate_UnityCompilation_RegistersAtUnityStartup()
    {
        var result = Run("""
            using GroveGames.Serialization;

            namespace UnityEngine
            {
                public enum RuntimeInitializeLoadType { SubsystemRegistration }

                public sealed class RuntimeInitializeOnLoadMethodAttribute : System.Attribute
                {
                    public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType)
                    {
                    }
                }
            }

            namespace UnityEditor
            {
                public sealed class InitializeOnLoadMethodAttribute : System.Attribute
                {
                }
            }

            namespace Game
            {
                [Schema]
                public sealed class Save
                {
                    public int Level;
                }
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Empty(result.CompilationErrors);
        Assert.Contains(result.GeneratedSources, source =>
            source.Contains("[global::UnityEngine.RuntimeInitializeOnLoadMethod(global::UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]")
            && source.Contains("[global::UnityEditor.InitializeOnLoadMethod]")
            && !source.Contains("ModuleInitializer"));
    }

    [Fact]
    public void Generate_MarkedFormatter_IsRegisteredWithVersionListAndMigrations()
    {
        var result = Run("""
            using GroveGames.Serialization;

            namespace Game;

            public struct Point
            {
                public int X;
            }

            [Formatter(version: 2)]
            internal sealed class PointFormatter : IFormatter<Point>
            {
                public void Write<TWriter>(ref TWriter writer, Point value) where TWriter : struct, IFormatWriter
                {
                    writer.WriteInt32(value.X);
                }

                public Point Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
                {
                    return new Point { X = reader.ReadInt32() };
                }

                public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
                    where TReader : struct, IFormatReader
                    where TWriter : struct, IFormatWriter
                {
                    writer.WriteInt32(reader.ReadInt32());
                }
            }

            public sealed class PointMigration : IMigration<Point>
            {
                public int FromVersion => 1;

                public void Apply(DataValue root)
                {
                }
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Empty(result.CompilationErrors);
        Assert.Contains(result.GeneratedSources, source =>
            source.Contains("registrar.AddFormatter(new global::Game.PointFormatter(), 2);")
            && source.Contains("new global::GroveGames.Serialization.ListFormatter<global::Game.Point>()")
            && source.Contains("new global::Game.PointMigration()"));
    }

    [Theory]
    [InlineData("public sealed class Outer { [Schema] public sealed class Inner { } }", "GGS001")]
    [InlineData("[Schema] public sealed class Box<T> { }", "GGS001")]
    [InlineData("[Schema] public sealed class Twin { public int Name; public int name; }", "GGS002")]
    [InlineData("[Schema] public sealed class Big { public ulong Value; }", "GGS003")]
    [InlineData("[Schema] public sealed class Grid { public int[,] Cells; }", "GGS003")]
    [InlineData("[Schema] public sealed class Keyed { public System.Collections.Generic.Dictionary<int, int> Map; }", "GGS003")]
    [InlineData("[Schema] public sealed class Frozen { public int Value { get; init; } }", "GGS003")]
    [InlineData("[Schema] public sealed class Located { public System.Uri Address; }", "GGS004")]
    [InlineData("[Schema] public sealed class Target { } public sealed class Bad : IMigration<Target> { public Bad(int value) { } public int FromVersion => 1; public void Apply(DataValue root) { } }", "GGS005")]
    [InlineData("[Schema] public sealed class NoDefault { public NoDefault(int value) { } }", "GGS006")]
    [InlineData("[Schema(version: 0)] public sealed class Zero { }", "GGS007")]
    [InlineData("[Formatter] public sealed class NotAFormatter { }", "GGS008")]
    [InlineData("public sealed class Outer { [Formatter] private sealed class Hidden : IFormatter<int> { public void Write<TWriter>(ref TWriter writer, int value) where TWriter : struct, IFormatWriter { } public int Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader => 0; public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer) where TReader : struct, IFormatReader where TWriter : struct, IFormatWriter { } } }", "GGS008")]
    public void Generate_InvalidSchema_ReportsDiagnostic(string declarations, string id)
    {
        var result = Run("using GroveGames.Serialization;\nnamespace Game;\n" + declarations);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == id);
    }

    [Fact]
    public void Generate_BuiltinMembers_NeedNoFormatter()
    {
        var result = Run("""
            using System;
            using System.Collections.Generic;
            using GroveGames.Serialization;

            namespace Game;

            [Schema]
            public sealed class Session
            {
                public Guid Id;
                public DateTime Start;
                public DateTimeOffset At;
                public TimeSpan Length;
                public Guid? Parent;
                public List<DateTime> Dates;
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Empty(result.CompilationErrors);
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
