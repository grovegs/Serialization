using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class RowSerializerTests
{
    [Fact]
    public void Serialize_Record_WritesValuesWithoutFieldNames()
    {
        var registry = CreateRegistry();
        var rows = new RowSerializer(registry);
        var record = CreateRecord();
        var output = new ByteBuffer();

        rows.Serialize(record, output);
        var messagePack = new MessagePackSerializer(registry).Serialize(record);

        Assert.DoesNotContain("level", Encoding.UTF8.GetString(output.WrittenSpan));
        Assert.True(output.Length < messagePack.Length);
    }

    [Fact]
    public void Deserialize_SameLayout_RoundTrips()
    {
        var rows = new RowSerializer(CreateRegistry());
        var record = CreateRecord();
        var output = new ByteBuffer();

        rows.Serialize(record, output);
        var result = rows.Deserialize<RowRecord>(output.WrittenMemory, rows.GetLayout<RowRecord>())!;

        Assert.Equal(record.Id, result.Id);
        Assert.Equal(record.Name, result.Name);
        Assert.Equal(record.Level, result.Level);
        Assert.Equal(record.Child!.Name, result.Child!.Name);
        Assert.Equal(record.Tags, result.Tags);
        Assert.Equal(record.Stats!["str"], result.Stats!["str"]);
    }

    [Fact]
    public void Deserialize_OlderLayout_MapsColumnsByName()
    {
        var rows = new RowSerializer(CreateRegistry());
        var output = new ByteBuffer();
        rows.Serialize(new RowOldShape { Removed = 9, Level = 7, Name = "old" }, output);

        var result = rows.Deserialize<RowRecord>(output.WrittenMemory, rows.GetLayout<RowOldShape>())!;

        Assert.Equal("old", result.Name);
        Assert.Equal(7, result.Level);
        Assert.Equal(0, result.Id);
    }

    [Fact]
    public void Deserialize_OlderVersion_RunsMigrations()
    {
        var rows = new RowSerializer(CreateRegistry());
        var output = new ByteBuffer();
        rows.Serialize(new RowOldShape { Removed = 120, Level = 3, Name = "veteran" }, output);

        var result = rows.Deserialize<RowSave>(output.WrittenMemory, rows.GetLayout<RowOldShape>(), version: 1)!;

        Assert.Equal("veteran", result.Name);
        Assert.Equal(120, result.Gold);
    }

    [Fact]
    public void Deserialize_NewerVersion_ThrowsNotSupportedException()
    {
        var rows = new RowSerializer(CreateRegistry());
        var output = new ByteBuffer();
        rows.Serialize(CreateRecord(), output);

        Assert.Throws<NotSupportedException>(() => rows.Deserialize<RowRecord>(output.WrittenMemory, rows.GetLayout<RowRecord>(), version: 2));
    }

    [Fact]
    public void Deserialize_EveryTruncation_ThrowsFormatException()
    {
        var rows = new RowSerializer(CreateRegistry());
        var layout = rows.GetLayout<RowRecord>();
        var output = new ByteBuffer();
        rows.Serialize(CreateRecord(), output);
        var bytes = output.ToArray();

        for (var length = 0; length < bytes.Length; length++)
        {
            var truncated = bytes.AsMemory(0, length);
            Assert.Throws<FormatException>(() => rows.Deserialize<RowRecord>(truncated, layout));
        }
    }

    [Fact]
    public void DeserializeLayout_SerializedLayout_RoundTrips()
    {
        var rows = new RowSerializer(CreateRegistry());
        var layout = rows.GetLayout<RowRecord>();
        var output = new ByteBuffer();

        rows.SerializeLayout(layout, output);
        var result = rows.DeserializeLayout(output.WrittenMemory);

        Assert.Equal(layout.Count, result.Count);

        for (var i = 0; i < layout.Count; i++)
        {
            Assert.Equal(layout.NameOf(i), result.NameOf(i));
        }
    }

    [Fact]
    public void DeserializeLayout_DuplicateColumns_ThrowsFormatException()
    {
        var rows = new RowSerializer(CreateRegistry());
        byte[] layout = [0x92, 0xa1, (byte)'a', 0xa1, (byte)'a'];

        Assert.Throws<FormatException>(() => rows.DeserializeLayout(layout));
    }

    private static FormatterRegistry CreateRegistry()
    {
        return new FormatterRegistryBuilder().AddGroveGamesSerializationTestsFormatters().Build();
    }

    private static RowRecord CreateRecord()
    {
        return new RowRecord
        {
            Id = 42,
            Name = "hero",
            Level = 12,
            Child = new GeneratedChild { Name = "pet" },
            Tags = ["a", "b"],
            Stats = new Dictionary<string, int> { ["str"] = 10 }
        };
    }
}

[Schema]
public sealed class RowRecord
{
    public int Id;
    public string? Name;
    public int Level;
    public GeneratedChild? Child;
    public List<string>? Tags;
    public Dictionary<string, int>? Stats;
}

[Schema]
public sealed class RowOldShape
{
    public int Removed;
    public int Level;
    public string? Name;
}

[Schema(version: 2)]
public sealed class RowSave
{
    public string? Name;
    public long Gold;
}

public sealed class RowSaveRemovedToGold : IMigration<RowSave>
{
    public int FromVersion => 1;

    public void Apply(DataValue root)
    {
        root.AsObject.Rename("removed", "gold");
    }
}
