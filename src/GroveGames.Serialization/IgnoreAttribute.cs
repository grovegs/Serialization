namespace GroveGames.Serialization;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class IgnoreAttribute : Attribute
{
}
