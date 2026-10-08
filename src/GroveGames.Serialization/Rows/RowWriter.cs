using GroveGames.Serialization.MessagePack;

namespace GroveGames.Serialization.Rows;

internal struct RowWriter : IFormatWriter
{
    private MessagePackWriter _inner;
    private int _depth;
    private bool _rootIsObject;

    public RowWriter(ByteBuffer output, MessagePackStack stack)
    {
        _inner = new MessagePackWriter(output, stack);
        _depth = 0;
        _rootIsObject = false;
    }

    public void BeginObject(int fieldCount)
    {
        if (_depth == 0)
        {
            _rootIsObject = true;
            _inner.BeginArray(fieldCount);
        }
        else
        {
            _inner.BeginObject(fieldCount);
        }

        _depth++;
    }

    public void EndObject()
    {
        _depth--;

        if (_depth == 0 && _rootIsObject)
        {
            _inner.EndArray();
        }
        else
        {
            _inner.EndObject();
        }
    }

    public void BeginArray(int count)
    {
        _inner.BeginArray(count);
        _depth++;
    }

    public void EndArray()
    {
        _depth--;
        _inner.EndArray();
    }

    public void WriteField(byte[] utf8Name)
    {
        if (_depth == 1 && _rootIsObject)
        {
            return;
        }

        _inner.WriteField(utf8Name);
    }

    public void WriteInt32(int value)
    {
        _inner.WriteInt32(value);
    }

    public void WriteInt64(long value)
    {
        _inner.WriteInt64(value);
    }

    public void WriteSingle(float value)
    {
        _inner.WriteSingle(value);
    }

    public void WriteDouble(double value)
    {
        _inner.WriteDouble(value);
    }

    public void WriteBool(bool value)
    {
        _inner.WriteBool(value);
    }

    public void WriteString(string? value)
    {
        _inner.WriteString(value);
    }

    public void WriteStringUtf8(ReadOnlySpan<byte> utf8)
    {
        _inner.WriteStringUtf8(utf8);
    }

    public void WriteNull()
    {
        _inner.WriteNull();
    }
}
