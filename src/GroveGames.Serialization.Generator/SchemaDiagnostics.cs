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
        "GGS004", "Member needs a formatter", "'{0}' has type '{1}', which has no [Schema] and no [Formatter] in this assembly, so saving it fails unless another assembly registers a formatter for it", Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor InvalidMigration = new(
        "GGS005", "Invalid migration", "Migration '{0}' must be a non-abstract, non-generic class with a parameterless constructor", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingConstructor = new(
        "GGS006", "Missing parameterless constructor", "'{0}' needs a parameterless constructor to be deserialized", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidVersion = new(
        "GGS007", "Invalid schema version", "'{0}' has version {1}, but versions start at 1", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidFormatter = new(
        "GGS008", "Invalid formatter", "Formatter '{0}' must be a public or internal, non-abstract, non-generic class or struct that implements IFormatter<T> once and has a parameterless constructor", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingMigration = new(
        "GGS009", "Missing migration", "'{0}' is version {1}, but no migration in this assembly upgrades it from version {2}, so saved data at that version would fail to load", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateMigration = new(
        "GGS010", "Duplicate migration", "'{0}' has more than one migration from version {1}, so registration would fail at startup", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MigrationOutOfRange = new(
        "GGS011", "Migration outside the versions", "Migration '{0}' upgrades '{1}' from version {2}, but '{1}' is version {3}, so registration would fail at startup", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateFormatter = new(
        "GGS012", "Duplicate formatter", "'{0}' has more than one formatter in this assembly, so registration would fail at startup", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MemberNotSaved = new(
        "GGS013", "Member is not saved", "'{0}' is not saved: {1}", Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor NestedVersion = new(
        "GGS014", "Version of a nested schema is not used", "'{0}' uses '{1}', which is version {2}, but versions and migrations only apply to the root type, so older '{1}' data inside it is read without migrating", Category, DiagnosticSeverity.Warning, true);

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
            "GGS009" => MissingMigration,
            "GGS010" => DuplicateMigration,
            "GGS011" => MigrationOutOfRange,
            "GGS012" => DuplicateFormatter,
            "GGS013" => MemberNotSaved,
            "GGS014" => NestedVersion,
            _ => InvalidFormatter
        };
    }
}
