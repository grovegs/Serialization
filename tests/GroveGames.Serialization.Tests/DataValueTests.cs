using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class DataValueTests
{
    [Fact]
    public void ListMigration_NullRootAndItems_SkipsThem()
    {
        var applied = 0;
        var migration = new ListMigration<int>(new TestCountingMigration(() => applied++));
        var items = new DataArray();
        items.Add(DataValue.Null);
        items.Add(DataValue.FromInteger(1));

        migration.Apply(DataValue.Null);
        migration.Apply(items);

        Assert.Equal(1, applied);
    }

    [Fact]
    public void Deserialize_ObjectWithEmptyKey_KeepsTheField()
    {
        var serializer = new JsonSerializer();

        var value = serializer.Deserialize<DataValue>(Encoding.UTF8.GetBytes("{\"\":1}"));

        Assert.Equal(1, value.AsObject[""].AsInt64);
    }

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

    private sealed class TestCountingMigration : IMigration<int>
    {
        private readonly Action _applied;

        public TestCountingMigration(Action applied)
        {
            _applied = applied;
        }

        public int FromVersion => 1;

        public void Apply(DataValue root)
        {
            _applied();
        }
    }
}
