using Microsoft.CodeAnalysis;

namespace GroveGames.Serialization.Generator;

internal static class SchemaDiagnostics
{
    private const string Category = "GroveGames.Serialization";

    public static readonly DiagnosticDescriptor UnsupportedType = new(
        "GGS001", "Unsupported schema type", "'{0}' cannot be a schema type because nested and generic types are not supported", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateFieldName = new(
        "GGS002", "Duplicate field name", "Members of '{0}' map to the same field name '{1}'", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnsupportedMember = new(
        "GGS003", "Unsupported member", "'{0}' cannot be serialized: {1}", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RegisteredMember = new(
        "GGS004", "Member uses a registered formatter", "'{0}' has type '{1}', which has no [Schema], so it needs a formatter marked with [Formatter]", Category, DiagnosticSeverity.Info, true);

    public static readonly DiagnosticDescriptor InvalidMigration = new(
        "GGS005", "Invalid migration", "Migration '{0}' must be a non-abstract, non-generic class with a parameterless constructor", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingConstructor = new(
        "GGS006", "Missing parameterless constructor", "'{0}' needs a parameterless constructor to be deserialized", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidVersion = new(
        "GGS007", "Invalid schema version", "'{0}' has version {1}, but versions start at 1", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidFormatter = new(
        "GGS008", "Invalid formatter", "Formatter '{0}' must be a public or internal, non-abstract, non-generic class or struct that implements IFormatter<T> once and has a parameterless constructor", Category, DiagnosticSeverity.Error, true);

    public static DiagnosticDescriptor Find(string id)
    {
        return id switch
        {
            "GGS001" => UnsupportedType,
            "GGS002" => DuplicateFieldName,
            "GGS003" => UnsupportedMember,
            "GGS004" => RegisteredMember,
            "GGS005" => InvalidMigration,
            "GGS006" => MissingConstructor,
            "GGS007" => InvalidVersion,
            _ => InvalidFormatter
        };
    }
}
