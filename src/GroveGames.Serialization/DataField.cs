namespace GroveGames.Serialization;

public readonly struct DataField
{
    public readonly string Name;
    public readonly DataNode Value;

    public DataField(string name, DataNode value)
    {
        Name = name;
        Value = value;
    }
}
