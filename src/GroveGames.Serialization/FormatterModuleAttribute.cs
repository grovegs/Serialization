using System.Diagnostics.CodeAnalysis;

namespace GroveGames.Serialization;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FormatterModuleAttribute : Attribute
{
    public FormatterModuleAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        Type = type;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type Type { get; }
}
