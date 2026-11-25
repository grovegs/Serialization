namespace GroveGames.Serialization.Generator.Models;

internal sealed record MigrationModel(string FullName, string TargetFullName, int? FromVersion, EquatableArray<DiagnosticInfo> Diagnostics);
