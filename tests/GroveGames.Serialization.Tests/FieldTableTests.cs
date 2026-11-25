using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class FieldTableTests
{
    [Fact]
    public void IndexOf_Utf8Name_ReturnsDeclarationIndex()
    {
        var table = new FieldTable("id", "count", "ıd");

        Assert.Equal(1, table.IndexOf(Encoding.UTF8.GetBytes("count")));
        Assert.Equal(2, table.IndexOf(Encoding.UTF8.GetBytes("ıd")));
        Assert.Equal(-1, table.IndexOf(Encoding.UTF8.GetBytes("missing")));
    }

    [Fact]
    public void IndexOf_ExpectedIndex_ReturnsMatchOrFallsBackToSearch()
    {
        var table = new FieldTable("id", "count", "weight");

        Assert.Equal(1, table.IndexOf(Encoding.UTF8.GetBytes("count"), 1));
        Assert.Equal(0, table.IndexOf(Encoding.UTF8.GetBytes("id"), 2));
        Assert.Equal(2, table.IndexOf(Encoding.UTF8.GetBytes("weight"), 3));
        Assert.Equal(-1, table.IndexOf(Encoding.UTF8.GetBytes("missing"), 0));
        Assert.Equal(1, table.IndexOf("count", 0));
    }

    [Fact]
    public void Constructor_DuplicateName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FieldTable("id", "id"));
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FieldTable(""));
    }
}
