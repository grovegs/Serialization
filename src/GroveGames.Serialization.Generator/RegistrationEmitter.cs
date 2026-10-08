using System.Text;
using GroveGames.Serialization.Generator.Models;

namespace GroveGames.Serialization.Generator;

internal static class RegistrationEmitter
{
    public static string ModuleName(string assemblyName)
    {
        var builder = new StringBuilder();
        var upper = true;

        foreach (var character in assemblyName)
        {
            if (!char.IsLetterOrDigit(character))
            {
                upper = true;
                continue;
            }

            builder.Append(upper ? char.ToUpperInvariant(character) : character);
            upper = false;
        }

        if (builder.Length == 0 || char.IsDigit(builder[0]))
        {
            builder.Insert(0, "Assembly");
        }

        return builder.Append("FormatterModule").ToString();
    }

    public static string Emit(string assemblyName, IReadOnlyList<SchemaTypeModel> schemas, IReadOnlyList<FormatterModel> formatters, IReadOnlyList<MigrationModel> migrations)
    {
        var name = ModuleName(assemblyName);
        var code = new CodeBuilder();
        code.Line("#nullable disable");
        code.Line($"[assembly: global::GroveGames.Serialization.FormatterModule(typeof(global::GroveGames.Serialization.{name}))]");
        code.Open("namespace GroveGames.Serialization");
        code.Line("[global::GroveGames.Serialization.Preserve]");
        code.Open($"internal sealed class {name} : global::GroveGames.Serialization.IFormatterModule");
        code.Line("[global::GroveGames.Serialization.Preserve]");
        code.Open($"public {name}()");
        code.Close();
        code.Open("public void Register(global::GroveGames.Serialization.FormatterRegistrar registrar)");

        foreach (var schema in schemas)
        {
            code.Line($"registrar.AddFormatter({schema.FormatterFullName}.Instance, {schema.Version});");
            code.Line($"registrar.AddFormatter(new global::GroveGames.Serialization.ListFormatter<{schema.FullName}>());");
        }

        foreach (var formatter in formatters)
        {
            code.Line($"registrar.AddFormatter(new {formatter.FullName}(), {formatter.Version});");
            code.Line($"registrar.AddFormatter(new global::GroveGames.Serialization.ListFormatter<{formatter.TargetFullName}>());");
        }

        foreach (var migration in migrations)
        {
            code.Line($"registrar.AddMigration(new {migration.FullName}());");
        }

        code.Close();
        code.Close();
        code.Close();
        return code.ToString();
    }
}
