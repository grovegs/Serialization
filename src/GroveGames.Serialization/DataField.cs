namespace GroveGames.Serialization;

public readonly struct DataField
{
    private readonly string _name;
    private readonly DataValue _value;

    public string Name => _name;
    public DataValue Value => _value;

    public DataField(string name, DataValue value)
    {
        _name = name;
        _value = value;
    }
}
