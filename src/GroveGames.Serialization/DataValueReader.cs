using System.Text;

namespace GroveGames.Serialization;

internal struct DataValueReader : IFormatReader
{
    private const int MaxDepth = 64;

    private readonly DataValue[] _containers;
    private readonly int[] _indices;
    private int _depth;
    private DataValue _current;

    public DataValueReader(DataValue root)
    {
        _containers = new DataValue[MaxDepth];
        _indices = new int[MaxDepth];
        _depth = 0;
        _current = root;
    }

    public readonly TokenType Peek()
    {
        return _current.Kind switch
        {
            DataKind.Object => TokenType.BeginObject,
            DataKind.Array => TokenType.BeginArray,
            DataKind.Integer => TokenType.Integer,
            DataKind.Float => TokenType.Float,
            DataKind.String => TokenType.String,
            DataKind.Text => TokenType.Text,
            DataKind.Bool => TokenType.Bool,
            _ => TokenType.Null
        };
    }

    public void ReadObjectStart()
    {
        Push(DataKind.Object);
    }

    public void ReadArrayStart()
    {
        Push(DataKind.Array);
    }

    public bool TryReadField(FieldTable fields, out int index)
    {
        if (!TryReadFieldName(out var name))
        {
            index = -1;
            return false;
        }

        index = fields.IndexOf(name);
        return true;
    }

    public bool TryReadFieldName(out string name)
    {
        var container = _containers[_depth].AsObject;

        if (++_indices[_depth] >= container.Count)
        {
            _depth--;
            name = string.Empty;
            return false;
        }

        var field = container[_indices[_depth]];
        _current = field.Value;
        name = field.Name;
        return true;
    }

    public bool TryReadNextElement()
    {
        var container = _containers[_depth].AsArray;

        if (++_indices[_depth] >= container.Count)
        {
            _depth--;
            return false;
        }

        _current = container[_indices[_depth]];
        return true;
    }

    public readonly int ReadInt32()
    {
        var value = _current.AsInt64;

        if (value < int.MinValue || value > int.MaxValue)
        {
            throw new FormatException($"{value} does not fit in a 32-bit integer.");
        }

        return (int)value;
    }

    public readonly long ReadInt64()
    {
        return _current.AsInt64;
    }

    public readonly float ReadSingle()
    {
        return (float)_current.AsDouble;
    }

    public readonly double ReadDouble()
    {
        return _current.AsDouble;
    }

    public readonly bool ReadBool()
    {
        return _current.AsBool;
    }

    public readonly string? ReadString()
    {
        return _current.AsString;
    }

    public readonly bool ReadStringUtf8(out ReadOnlySpan<byte> utf8)
    {
        var text = _current.AsString;

        if (text == null)
        {
            utf8 = default;
            return false;
        }

        var buffer = ScratchBuffers.Text();
        var written = Encoding.UTF8.GetBytes(text.AsSpan(), buffer.GetSpan(Encoding.UTF8.GetMaxByteCount(text.Length)));
        buffer.Advance(written);
        utf8 = buffer.WrittenSpan;
        return true;
    }

    public readonly void Skip()
    {
    }

    private void Push(DataKind kind)
    {
        if (_current.Kind != kind)
        {
            throw new FormatException($"Value is {_current.Kind}, not {kind}.");
        }

        if (++_depth >= MaxDepth)
        {
            throw new FormatException($"Nesting is deeper than {MaxDepth - 1} levels.");
        }

        _containers[_depth] = _current;
        _indices[_depth] = -1;
    }
}
