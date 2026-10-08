namespace GroveGames.Serialization.Generator.Models;

internal sealed record FormatterModel(string FullName, string TargetFullName, int Version, EquatableArray<DiagnosticInfo> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}
