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
