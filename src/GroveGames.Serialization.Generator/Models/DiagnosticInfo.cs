using Microsoft.CodeAnalysis;

namespace GroveGames.Serialization.Generator.Models;

internal sealed record DiagnosticInfo(string Id, LocationInfo? Location, EquatableArray<string> Arguments)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] arguments)
    {
        return new DiagnosticInfo(descriptor.Id, LocationInfo.From(location), new EquatableArray<string>(arguments));
    }

    public Diagnostic ToDiagnostic()
    {
        var arguments = new object[Arguments.Count];

        for (var i = 0; i < Arguments.Count; i++)
        {
            arguments[i] = Arguments[i];
        }

        return Diagnostic.Create(SchemaDiagnostics.Find(Id), Location?.ToLocation(), arguments);
    }
}
