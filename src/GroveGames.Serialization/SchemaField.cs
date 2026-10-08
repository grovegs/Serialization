namespace GroveGames.Serialization;

public readonly struct SchemaField
{
    public readonly string Name;
    public readonly FieldType Type;

    public SchemaField(string name, FieldType type)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(type);

        Name = name;
        Type = type;
    }
}
