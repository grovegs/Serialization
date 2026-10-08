using GroveGames.Serialization.MessagePack;

namespace GroveGames.Serialization.Rows;

internal struct RowReader : IFormatReader
{
    private readonly FieldTable _layout;
    private MessagePackReader _inner;
    private int _depth;
    private int _column;
    private bool _rootIsObject;

    public RowReader(ReadOnlyMemory<byte> data, FieldTable layout, MessagePackStack stack)
    {
        _inner = new MessagePackReader(data, stack);
        _layout = layout;
        _depth = 0;
        _column = -1;
        _rootIsObject = false;
    }

    public TokenType Peek()
    {
        var token = _inner.Peek();
        return _depth == 0 && token == TokenType.BeginArray ? TokenType.BeginObject : token;
    }

    public void ReadObjectStart()
    {
        if (_depth == 0)
        {
            _inner.ReadArrayStart();
            _rootIsObject = true;
            _column = -1;
        }
        else
        {
            _inner.ReadObjectStart();
        }

        _depth++;
    }

    public void ReadArrayStart()
    {
        _inner.ReadArrayStart();
        _depth++;
    }

    public bool TryReadField(FieldTable fields, out int index)
    {
        if (!IsAtRoot)
        {
            return Track(_inner.TryReadField(fields, out index));
        }

        if (!Track(_inner.TryReadNextElement()))
        {
            index = -1;
            return false;
        }

        _column++;
        index = _column < _layout.Count ? fields.IndexOf(_layout[_column]) : -1;
        return true;
    }

    public bool TryReadFieldName(out string name)
    {
        if (!IsAtRoot)
        {
            return Track(_inner.TryReadFieldName(out name));
        }

        if (!Track(_inner.TryReadNextElement()))
        {
            name = string.Empty;
            return false;
        }

        _column++;

        if (_column >= _layout.Count)
        {
            throw new FormatException($"The row has more values than its {_layout.Count} columns.");
        }

        name = _layout.NameOf(_column);
        return true;
    }

    public bool TryReadNextElement()
    {
        return Track(_inner.TryReadNextElement());
    }

    public int ReadInt32()
    {
        return _inner.ReadInt32();
    }

    public long ReadInt64()
    {
        return _inner.ReadInt64();
    }

    public float ReadSingle()
    {
        return _inner.ReadSingle();
    }

    public double ReadDouble()
    {
        return _inner.ReadDouble();
    }

    public bool ReadBool()
    {
        return _inner.ReadBool();
    }

    public string? ReadString()
    {
        return _inner.ReadString();
    }

    public bool ReadStringUtf8(out ReadOnlySpan<byte> utf8)
    {
        return _inner.ReadStringUtf8(out utf8);
    }

    public void Skip()
    {
        _inner.Skip();
    }

    public void EndDocument()
    {
        _inner.EndDocument();
    }

    private readonly bool IsAtRoot => _depth == 1 && _rootIsObject;

    private bool Track(bool hasNext)
    {
        if (!hasNext)
        {
            _depth--;
        }

        return hasNext;
    }
}
