namespace GroveGames.Serialization.Tests;

public sealed class DataValueTests
{
    [Fact]
    public void Default_IsNull()
    {
        var value = default(DataValue);

        Assert.True(value.IsNull);
        Assert.Equal(DataValue.Null, value);
        Assert.Null(value.AsString);
    }

    [Fact]
    public void ImplicitConversions_StoreScalarsInline()
    {
        DataValue integer = 42;
        DataValue number = 1.5;
        DataValue flag = true;
        DataValue text = "hero";

        Assert.Equal(DataKind.Integer, integer.Kind);
        Assert.Equal(42, integer.AsInt64);
        Assert.Equal(1.5, number.AsDouble);
        Assert.True(flag.AsBool);
        Assert.Equal("hero", text.AsString);
    }

    [Fact]
    public void AsInt64_Text_ParsesInvariantNumber()
    {
        var value = DataValue.FromText("42");

        Assert.Equal(42, value.AsInt64);
        Assert.Equal("42", value.AsString);
    }

    [Fact]
    public void AsInt64_String_ThrowsFormatException()
    {
        DataValue value = "42";

        Assert.Throws<FormatException>(() => value.AsInt64);
    }

    [Fact]
    public void AsObject_Array_ThrowsFormatException()
    {
        DataValue value = new DataArray();

        Assert.Throws<FormatException>(() => value.AsObject);
    }

    [Fact]
    public void Equals_SameScalars_IsTrueAndHashesMatch()
    {
        DataValue left = "key";
        DataValue right = "key";

        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.NotEqual(DataValue.FromInteger(1), DataValue.FromFloat(1));
    }
}
