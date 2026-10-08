using System.Text;
using GroveGames.Serialization.Generator.Models;

namespace GroveGames.Serialization.Generator;

internal static class RegistrationEmitter
{
    public static string MethodName(string assemblyName)
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

        return builder.ToString();
    }

    public static string Emit(string assemblyName, IReadOnlyList<SchemaTypeModel> schemas, IReadOnlyList<MigrationModel> migrations)
    {
        var name = MethodName(assemblyName);
        var code = new CodeBuilder();
        code.Line("#nullable disable");
        code.Open("namespace GroveGames.Serialization");
        code.Open($"public static class {name}FormatterRegistryBuilderExtensions");
        code.Open($"public static global::GroveGames.Serialization.FormatterRegistryBuilder Add{name}Formatters(this global::GroveGames.Serialization.FormatterRegistryBuilder builder)");

        foreach (var schema in schemas)
        {
            code.Line($"builder.AddFormatter({schema.FormatterFullName}.Instance, {schema.Version});");
            code.Line($"builder.AddFormatter(new global::GroveGames.Serialization.ListFormatter<{schema.FullName}>());");
        }

        foreach (var migration in migrations)
        {
            code.Line($"builder.AddMigration(new {migration.FullName}());");
        }

        code.Line("return builder;");
        code.Close();
        code.Close();
        code.Close();
        return code.ToString();
    }
}
