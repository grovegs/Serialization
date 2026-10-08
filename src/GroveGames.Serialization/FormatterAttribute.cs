namespace GroveGames.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class FormatterAttribute : Attribute
{
    public FormatterAttribute(int version = 1)
    {
        Version = version;
    }

    public int Version { get; }
}
