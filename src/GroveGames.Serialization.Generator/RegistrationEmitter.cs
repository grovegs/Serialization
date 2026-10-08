using System.Text;
using GroveGames.Serialization.Generator.Models;

namespace GroveGames.Serialization.Generator;

internal static class RegistrationEmitter
{
    public static string ClassName(string assemblyName)
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

        return builder.Append("FormatterRegistration").ToString();
    }

    public static string Emit(TargetModel target, IReadOnlyList<SchemaTypeModel> schemas, IReadOnlyList<FormatterModel> formatters, IReadOnlyList<MigrationModel> migrations)
    {
        var name = ClassName(target.AssemblyName);
        var code = new CodeBuilder();
        code.Line("#nullable disable");

        if (!target.HasUnityEngine && !target.HasModuleInitializer)
        {
            code.Open("namespace System.Runtime.CompilerServices");
            code.Line("[global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false)]");
            code.Open("internal sealed class ModuleInitializerAttribute : global::System.Attribute");
            code.Close();
            code.Close();
        }

        code.Open("namespace GroveGames.Serialization");
        code.Open($"internal static class {name}");
        code.Line("private static bool s_registered;");
        code.Line();

        if (target.HasUnityEngine)
        {
            code.Line("[global::UnityEngine.RuntimeInitializeOnLoadMethod(global::UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]");

            if (target.HasUnityEditor)
            {
                code.Line("[global::UnityEditor.InitializeOnLoadMethod]");
            }
        }
        else
        {
            code.Line("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        }

        code.Open("internal static void Register()");
        code.Open("if (s_registered)").Line("return;").Close().Line();
        code.Line("s_registered = true;");
        code.Line("global::GroveGames.Serialization.Formatters.Register(Configure);");
        code.Close();
        code.Line();
        code.Open("private static void Configure(global::GroveGames.Serialization.FormatterRegistrar registrar)");

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
