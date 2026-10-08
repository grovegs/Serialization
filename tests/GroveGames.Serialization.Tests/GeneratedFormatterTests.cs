using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class GeneratedFormatterTests
{
    [Fact]
    public void Serialize_EveryMemberKind_RoundTripsThroughEveryBinaryFormat()
    {
        var registry = CreateRegistry();
        var value = CreateSample();

        foreach (var serializer in new ISerializer[] { new JsonSerializer(registry), new MessagePackSerializer(registry) })
        {
            var result = serializer.Deserialize<GeneratedSample>(serializer.Serialize(value))!;

            Assert.Equal(value.Flag, result.Flag);
            Assert.Equal(value.Small, result.Small);
            Assert.Equal(value.Tiny, result.Tiny);
            Assert.Equal(value.Count, result.Count);
            Assert.Equal(value.Unsigned, result.Unsigned);
            Assert.Equal(value.Large, result.Large);
            Assert.Equal(value.Ratio, result.Ratio);
            Assert.Equal(value.Precise, result.Precise);
            Assert.Equal(value.Title, result.Title);
            Assert.Equal(value.Rarity, result.Rarity);
            Assert.Equal(value.Optional, result.Optional);
            Assert.Null(result.Missing);
            Assert.Equal(value.Position.X, result.Position.X);
            Assert.Equal(value.MaybePosition!.Value.Y, result.MaybePosition!.Value.Y);
            Assert.Equal(value.Child!.Name, result.Child!.Name);
            Assert.Equal(value.Children!.Select(c => c.Name), result.Children!.Select(c => c.Name));
            Assert.Equal(value.Scores, result.Scores);
            Assert.Equal(value.Grid![1], result.Grid![1]);
            Assert.Equal(value.Stats!["str"], result.Stats!["str"]);
            Assert.Equal("hard", result.Extra.AsObject["mode"].AsString);
            Assert.Equal(42, result.Extra.AsObject["seed"].AsInt64);
            Assert.Equal(value.Property, result.Property);
            Assert.Equal(0, result.Ignored);
        }
    }

    [Fact]
    public void Serialize_GeneratedType_UsesCamelCaseNamesInDeclarationOrder()
    {
        var json = new JsonSerializer(CreateRegistry());

        var text = Encoding.UTF8.GetString(json.Serialize(new GeneratedChild { Name = "a", URLPath = "b", HP = 3 }));

        Assert.Equal("{\"$v\":2,\"data\":{\"name\":\"a\",\"urlPath\":\"b\",\"hp\":3}}", text);
    }

    [Fact]
    public void Convert_GeneratedType_PreservesData()
    {
        var registry = CreateRegistry();
        var json = new JsonSerializer(registry);
        var messagePack = new MessagePackSerializer(registry);
        var original = json.Serialize(CreateSample());

        var packed = new Converter(json, messagePack).Convert<GeneratedSample>(original);
        var back = new Converter(messagePack, json).Convert<GeneratedSample>(packed);

        Assert.Equal(Encoding.UTF8.GetString(original), Encoding.UTF8.GetString(back));
    }

    [Fact]
    public void Deserialize_OlderGeneratedType_RunsGeneratedMigration()
    {
        var json = new JsonSerializer(CreateRegistry());

        var result = json.Deserialize<GeneratedChild>(Encoding.UTF8.GetBytes("{\"label\":\"old\"}"));

        Assert.Equal("old", result!.Name);
    }

    [Fact]
    public void Deserialize_OutOfRangeSmallInteger_ThrowsFormatException()
    {
        var json = new JsonSerializer(CreateRegistry());

        Assert.Throws<FormatException>(() => json.Deserialize<GeneratedSample>(Encoding.UTF8.GetBytes("{\"small\":40000}")));
    }

    [Fact]
    public void GetSchema_GeneratedType_DescribesFields()
    {
        var registry = CreateRegistry();

        var schema = registry.GetSchema<GeneratedSample>();

        Assert.Equal(1, schema.Version);
        Assert.Equal(FieldTypeKind.Int32, schema.Fields[schema.IndexOf("small")].Type.Kind);
        Assert.Equal(FieldTypeKind.Int64, schema.Fields[schema.IndexOf("unsigned")].Type.Kind);
        Assert.True(schema.Fields[schema.IndexOf("optional")].Type.IsNullable);
        Assert.False(schema.Fields[schema.IndexOf("count")].Type.IsNullable);
        Assert.Equal(FieldTypeKind.Array, schema.Fields[schema.IndexOf("children")].Type.Kind);
        Assert.Equal(typeof(GeneratedChild), schema.Fields[schema.IndexOf("children")].Type.ElementType!.ObjectType);
        Assert.Equal(FieldTypeKind.Map, schema.Fields[schema.IndexOf("stats")].Type.Kind);
        Assert.Equal(FieldTypeKind.Any, schema.Fields[schema.IndexOf("extra")].Type.Kind);
        Assert.Equal(-1, schema.IndexOf("ignored"));
        Assert.NotEqual(schema.Fingerprint, registry.GetSchema<GeneratedChild>().Fingerprint);
    }

    private static FormatterRegistry CreateRegistry()
    {
        return new FormatterRegistryBuilder().AddGroveGamesSerializationTestsFormatters().Build();
    }

    private static GeneratedSample CreateSample()
    {
        var extra = new DataObject();
        extra["mode"] = "hard";
        extra["seed"] = 42;

        return new GeneratedSample
        {
            Flag = true,
            Small = -12,
            Tiny = 250,
            Count = int.MaxValue,
            Unsigned = uint.MaxValue,
            Large = long.MinValue,
            Ratio = 0.5f,
            Precise = Math.PI,
            Title = "sample",
            Rarity = GeneratedRarity.Epic,
            Optional = 7,
            Missing = null,
            Position = new GeneratedPoint { X = 1, Y = 2 },
            MaybePosition = new GeneratedPoint { X = 3, Y = 4 },
            Child = new GeneratedChild { Name = "child" },
            Children = [new GeneratedChild { Name = "a" }, new GeneratedChild { Name = "b" }],
            Scores = [1.5, 2.5],
            Grid = [[1, 2], [3, 4]],
            Stats = new Dictionary<string, int> { ["str"] = 10, ["dex"] = 12 },
            Extra = extra,
            Property = "property",
            Ignored = 99
        };
    }
}

public enum GeneratedRarity : byte
{
    Common,
    Epic = 200
}

[Schema]
public struct GeneratedPoint
{
    public int X;
    public int Y;
}

[Schema(version: 2)]
public sealed class GeneratedChild
{
    public string? Name;
    public string? URLPath;
    public int HP;
}

public sealed class GeneratedChildRenameLabel : IMigration<GeneratedChild>
{
    public int FromVersion => 1;

    public void Apply(DataValue root)
    {
        root.AsObject.Rename("label", "name");
    }
}

[Schema]
public sealed class GeneratedSample
{
    public bool Flag;
    public short Small;
    public byte Tiny;
    public int Count;
    public uint Unsigned;
    public long Large;
    public float Ratio;
    public double Precise;
    public string? Title;
    public GeneratedRarity Rarity;
    public int? Optional;
    public int? Missing;
    public GeneratedPoint Position;
    public GeneratedPoint? MaybePosition;
    public GeneratedChild? Child;
    public List<GeneratedChild>? Children;
    public double[]? Scores;
    public List<int[]>? Grid;
    public Dictionary<string, int>? Stats;
    public DataValue Extra;

    public string? Property { get; set; }

    [Ignore]
    public int Ignored;
}
