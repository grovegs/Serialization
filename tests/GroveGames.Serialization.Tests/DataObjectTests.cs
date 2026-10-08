namespace GroveGames.Serialization.Tests;

public sealed class DataObjectTests
{
    [Fact]
    public void Rename_ExistingField_KeepsValueAndPosition()
    {
        var obj = new DataObject();
        obj["a"] = 1;
        obj["b"] = 2;

        var renamed = obj.Rename("a", "c");

        Assert.True(renamed);
        Assert.Equal("c", obj[0].Name);
        Assert.Equal(1, obj["c"].AsInt64);
        Assert.False(obj.Contains("a"));
    }

    [Fact]
    public void Indexer_MissingField_ReturnsNull()
    {
        var obj = new DataObject();

        Assert.True(obj["missing"].IsNull);
        Assert.False(obj.TryGetValue("missing", out _));
    }

    [Fact]
    public void Indexer_ExistingField_ReplacesValue()
    {
        var obj = new DataObject();
        obj["level"] = 1;

        obj["level"] = 5;

        Assert.Equal(1, obj.Count);
        Assert.Equal(5, obj["level"].AsInt64);
    }

    [Fact]
    public void Remove_ExistingField_RemovesIt()
    {
        var obj = new DataObject();
        obj["xp"] = 10;

        Assert.True(obj.Remove("xp"));
        Assert.Equal(0, obj.Count);
    }
}
