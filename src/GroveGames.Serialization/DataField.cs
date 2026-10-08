namespace GroveGames.Serialization;

public readonly struct DataField
{
    public readonly string Name;
    public readonly DataValue Value;

    public DataField(string name, DataValue value)
    {
        Name = name;
        Value = value;
    }
}
