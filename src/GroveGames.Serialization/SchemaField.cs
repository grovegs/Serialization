namespace GroveGames.Serialization;

public readonly struct SchemaField
{
    private readonly string _name;
    private readonly FieldType _type;

    public string Name => _name;
    public FieldType Type => _type;

    public SchemaField(string name, FieldType type)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(type);

        _name = name;
        _type = type;
    }
}
