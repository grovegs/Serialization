namespace GroveGames.Serialization.Tests;

public sealed class DataNodeTests
{
    [Fact]
    public void Rename_ExistingField_KeepsValueAndPosition()
    {
        var node = DataNode.NewObject();
        node["a"] = DataNode.FromInt(1);
        node["b"] = DataNode.FromInt(2);

        node.Rename("a", "c");

        Assert.Equal("c", node.Fields[0].Name);
        Assert.Equal(1, node["c"]!.AsInt64);
        Assert.False(node.Has("a"));
    }

    [Fact]
    public void AsInt64_Text_ParsesInvariantNumber()
    {
        var node = DataNode.FromText("42");

        Assert.Equal(42, node.AsInt64);
        Assert.Equal("42", node.AsString);
    }

    [Fact]
    public void AsInt64_String_ThrowsFormatException()
    {
        var node = DataNode.FromString("42");

        Assert.Throws<FormatException>(() => node.AsInt64);
    }

    [Fact]
    public void Indexer_OnArray_ThrowsFormatException()
    {
        var node = DataNode.NewArray();

        Assert.Throws<FormatException>(() => node["a"]);
    }
}
