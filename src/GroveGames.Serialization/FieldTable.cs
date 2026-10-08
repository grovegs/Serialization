using System.Text;

namespace GroveGames.Serialization;

public sealed class FieldTable
{
    private readonly string[] _names;
    private readonly byte[][] _utf8Names;

    public FieldTable(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        _names = names;
        _utf8Names = new byte[names.Length][];

        for (var i = 0; i < names.Length; i++)
        {
            ArgumentException.ThrowIfNullOrEmpty(names[i], nameof(names));

            for (var j = 0; j < i; j++)
            {
                if (string.Equals(names[j], names[i], StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Field name '{names[i]}' is used twice.", nameof(names));
                }
            }

            _utf8Names[i] = Encoding.UTF8.GetBytes(names[i]);
        }
    }

    public static FieldTable Empty { get; } = new();

    public int Count => _names.Length;

    public byte[] this[int index] => _utf8Names[index];

    public string NameOf(int index)
    {
        return _names[index];
    }

    public int IndexOf(ReadOnlySpan<byte> utf8Name)
    {
        if (utf8Name.IsEmpty)
        {
            return -1;
        }

        var length = utf8Name.Length;
        var first = utf8Name[0];

        for (var i = 0; i < _utf8Names.Length; i++)
        {
            var name = _utf8Names[i];

            if (name.Length != length || name[0] != first)
            {
                continue;
            }

            if (utf8Name.SequenceEqual(name))
            {
                return i;
            }
        }

        return -1;
    }

    public int IndexOf(string name)
    {
        for (var i = 0; i < _names.Length; i++)
        {
            if (string.Equals(_names[i], name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
