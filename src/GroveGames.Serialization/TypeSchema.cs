namespace GroveGames.Serialization;

public sealed class TypeSchema
{
    private const ulong FnvOffset = 14695981039346656037;
    private const ulong FnvPrime = 1099511628211;

    private readonly SchemaField[] _fields;
    private readonly Type _type;
    private readonly int _version;
    private readonly ulong _fingerprint;

    public TypeSchema(Type type, int version, params SchemaField[] fields)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        ArgumentNullException.ThrowIfNull(fields);

        for (var i = 0; i < fields.Length; i++)
        {
            for (var j = 0; j < i; j++)
            {
                if (string.Equals(fields[i].Name, fields[j].Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Field name '{fields[i].Name}' is used twice.", nameof(fields));
                }
            }
        }

        _type = type;
        _version = version;
        _fields = fields;
        _fingerprint = ComputeFingerprint(fields);
    }

    public Type Type => _type;

    public int Version => _version;

    public IReadOnlyList<SchemaField> Fields => _fields;

    public ulong Fingerprint => _fingerprint;

    public int IndexOf(string name)
    {
        for (var i = 0; i < _fields.Length; i++)
        {
            if (string.Equals(_fields[i].Name, name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static ulong ComputeFingerprint(SchemaField[] fields)
    {
        var hash = FnvOffset;

        for (var i = 0; i < fields.Length; i++)
        {
            hash = Mix(hash, fields[i].Name);
            hash = Mix(hash, fields[i].Type);
        }

        return hash;
    }

    private static ulong Mix(ulong hash, FieldType type)
    {
        hash = Mix(hash, (byte)type.Kind);
        hash = Mix(hash, type.IsNullable ? (byte)1 : (byte)0);

        if (type.ElementType != null)
        {
            hash = Mix(hash, type.ElementType);
        }

        if (type.ObjectType != null)
        {
            hash = Mix(hash, type.ObjectType.FullName ?? type.ObjectType.Name);
        }

        return hash;
    }

    private static ulong Mix(ulong hash, string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            hash = Mix(hash, (byte)text[i]);
            hash = Mix(hash, (byte)(text[i] >> 8));
        }

        return Mix(hash, 0);
    }

    private static ulong Mix(ulong hash, byte value)
    {
        return (hash ^ value) * FnvPrime;
    }
}
