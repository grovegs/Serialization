using System.Text;

namespace GroveGames.Serialization.Generator;

internal static class FieldNames
{
    public static string ToCamelCase(string name)
    {
        if (name.Length == 0 || !char.IsUpper(name[0]))
        {
            return name;
        }

        var upper = 0;

        while (upper < name.Length && char.IsUpper(name[upper]))
        {
            upper++;
        }

        var lower = upper == name.Length || upper == 1 ? upper : upper - 1;
        var builder = new StringBuilder(name.Length);

        for (var i = 0; i < name.Length; i++)
        {
            builder.Append(i < lower ? char.ToLowerInvariant(name[i]) : name[i]);
        }

        return builder.ToString();
    }
}
