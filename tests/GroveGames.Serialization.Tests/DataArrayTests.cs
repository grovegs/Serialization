namespace GroveGames.Serialization.Tests;

public sealed class DataArrayTests
{
    [Fact]
    public void Add_Values_KeepsOrder()
    {
        var array = new DataArray();

        array.Add(1);
        array.Add("two");
        array.Insert(0, true);

        Assert.Equal(3, array.Count);
        Assert.True(array[0].AsBool);
        Assert.Equal(1, array[1].AsInt64);
        Assert.Equal("two", array[2].AsString);
    }

    [Fact]
    public void RemoveAt_Index_RemovesValue()
    {
        var array = new DataArray();
        array.Add(1);
        array.Add(2);

        array.RemoveAt(0);

        Assert.Equal(2, array[0].AsInt64);
    }
}
