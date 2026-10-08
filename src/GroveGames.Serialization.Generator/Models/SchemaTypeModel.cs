namespace GroveGames.Serialization.Generator.Models;

internal sealed record SchemaTypeModel(
    string Namespace,
    string Name,
    string FullName,
    bool IsValueType,
    int Version,
    EquatableArray<SchemaMemberModel> Members,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
    public string FormatterName => Name + "Formatter";

    public string FormatterFullName => string.IsNullOrEmpty(Namespace) ? "global::" + FormatterName : "global::" + Namespace + "." + FormatterName;

    public bool IsValid
    {
        get
        {
            foreach (var diagnostic in Diagnostics)
            {
                if (SchemaDiagnostics.Find(diagnostic.Id).DefaultSeverity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
