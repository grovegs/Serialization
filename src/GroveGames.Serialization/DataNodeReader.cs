using System.Text;

namespace GroveGames.Serialization;

internal struct DataNodeReader : IFormatReader
{
    private const int MaxDepth = 64;

    private readonly DataNode[] _containers;
    private readonly int[] _indices;
    private int _depth;
    private DataNode _current;

    public DataNodeReader(DataNode root)
    {
        _containers = new DataNode[MaxDepth];
        _indices = new int[MaxDepth];
        _depth = 0;
        _current = root;
    }

    public TokenType Peek()
    {
        return _current.Kind switch
        {
            NodeKind.Object => TokenType.BeginObject,
            NodeKind.Array => TokenType.BeginArray,
            NodeKind.Int => TokenType.Integer,
            NodeKind.Float => TokenType.Float,
            NodeKind.String => TokenType.String,
            NodeKind.Text => TokenType.Text,
            NodeKind.Bool => TokenType.Bool,
            _ => TokenType.Null
        };
    }

    public void ReadObjectStart()
    {
        Push(NodeKind.Object);
    }

    public void ReadArrayStart()
    {
        Push(NodeKind.Array);
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
        var container = _containers[_depth];

        if (++_indices[_depth] >= container.Fields.Count)
        {
            _depth--;
            name = string.Empty;
            return false;
        }

        var field = container.Fields[_indices[_depth]];
        _current = field.Value;
        name = field.Name;
        return true;
    }

    public bool TryReadNextElement()
    {
        var container = _containers[_depth];

        if (++_indices[_depth] >= container.Items.Count)
        {
            _depth--;
            return false;
        }

        _current = container.Items[_indices[_depth]];
        return true;
    }

    public int ReadInt32()
    {
        var value = _current.AsInt64;

        if (value < int.MinValue || value > int.MaxValue)
        {
            throw new FormatException($"{value} does not fit in a 32-bit integer.");
        }

        return (int)value;
    }

    public long ReadInt64()
    {
        return _current.AsInt64;
    }

    public float ReadSingle()
    {
        return (float)_current.AsDouble;
    }

    public double ReadDouble()
    {
        return _current.AsDouble;
    }

    public bool ReadBool()
    {
        return _current.AsBool;
    }

    public string? ReadString()
    {
        return _current.AsString;
    }

    public bool ReadStringUtf8(out ReadOnlySpan<byte> utf8)
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

    public void Skip()
    {
    }

    private void Push(NodeKind kind)
    {
        if (_current.Kind != kind)
        {
            throw new FormatException($"Node is {_current.Kind}, not {kind}.");
        }

        if (++_depth >= MaxDepth)
        {
            throw new FormatException($"Nesting is deeper than {MaxDepth - 1} levels.");
        }

        _containers[_depth] = _current;
        _indices[_depth] = -1;
    }
}
