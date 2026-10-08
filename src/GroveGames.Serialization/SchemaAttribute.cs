namespace GroveGames.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class SchemaAttribute : Attribute
{
    public SchemaAttribute(int version = 1)
    {
        Version = version;
    }

    public int Version { get; }
}
