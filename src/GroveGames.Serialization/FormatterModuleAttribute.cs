namespace GroveGames.Serialization;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FormatterModuleAttribute : Attribute
{
    public FormatterModuleAttribute(Type type)
    {
        Type = type;
    }

    public Type Type { get; }
}
