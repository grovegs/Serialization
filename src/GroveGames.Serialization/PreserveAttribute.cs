namespace GroveGames.Serialization;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
public sealed class PreserveAttribute : Attribute
{
}
