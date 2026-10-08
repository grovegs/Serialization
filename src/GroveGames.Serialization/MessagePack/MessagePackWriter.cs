using System.Buffers.Binary;
using System.Text;

namespace GroveGames.Serialization.MessagePack;

internal struct MessagePackWriter : IFormatWriter
{
    private readonly ByteBuffer _output;
    private readonly MessagePackStack _stack;
    private int _depth;

    public MessagePackWriter(ByteBuffer output, MessagePackStack stack)
    {
        _output = output;
        _stack = stack;
        _depth = 0;
    }

    public void BeginEnvelope(int version)
    {
        BeginObject(2);
        WriteField(Envelope.Fields[Envelope.VersionField]);
        WriteInt32(version);
        WriteField(Envelope.Fields[Envelope.DataField]);
    }

    public void EndEnvelope()
    {
        EndObject();
    }

    public void BeginObject(int fieldCount)
    {
        Open(fieldCount, false, 0x80, 0xde, 0xdf);
    }

    public void BeginArray(int count)
    {
        Open(count, true, 0x90, 0xdc, 0xdd);
    }

    public void EndObject()
    {
        Close();
    }

    public void EndArray()
    {
        Close();
    }

    public void WriteField(byte[] utf8Name)
    {
        _stack.Counts[_depth]++;
        WriteStringHeader(utf8Name.Length);
        _output.Write(utf8Name);
    }

    public void WriteInt32(int value)
    {
        OnValue();
        WriteInteger(value);
    }

    public void WriteInt64(long value)
    {
        OnValue();
        WriteInteger(value);
    }

    public void WriteSingle(float value)
    {
        OnValue();
        var span = _output.Take(5);
        span[0] = 0xca;
        BinaryPrimitives.WriteInt32BigEndian(span.Slice(1), BitConverter.SingleToInt32Bits(value));
    }

    public void WriteDouble(double value)
    {
        OnValue();
        var span = _output.Take(9);
        span[0] = 0xcb;
        BinaryPrimitives.WriteInt64BigEndian(span.Slice(1), BitConverter.DoubleToInt64Bits(value));
    }

    public void WriteBool(bool value)
    {
        OnValue();
        _output.Write(value ? (byte)0xc3 : (byte)0xc2);
    }

    public void WriteNull()
    {
        OnValue();
        _output.Write(0xc0);
    }

    public void WriteString(string? value)
    {
        if (value == null)
        {
            WriteNull();
            return;
        }

        OnValue();
        var length = Encoding.UTF8.GetByteCount(value);
        WriteStringHeader(length);
        Encoding.UTF8.GetBytes(value.AsSpan(), _output.Take(length));
    }

    public void WriteStringUtf8(ReadOnlySpan<byte> utf8)
    {
        OnValue();
        WriteStringHeader(utf8.Length);
        _output.Write(utf8);
    }

    private void OnValue()
    {
        if (_depth > 0 && _stack.IsArray[_depth])
        {
            _stack.Counts[_depth]++;
        }
    }

    private void Open(int count, bool isArray, byte fixHeader, byte header16, byte header32)
    {
        OnValue();

        if (++_depth > MessagePackStack.MaxDepth)
        {
            throw new InvalidOperationException($"MessagePack nesting is deeper than {MessagePackStack.MaxDepth} levels.");
        }

        _stack.IsArray[_depth] = isArray;
        _stack.Counts[_depth] = 0;

        if (count < 0)
        {
            _stack.Positions[_depth] = _output.Length;
            _output.Take(5)[0] = header32;
            return;
        }

        _stack.Positions[_depth] = -1;

        if (count < 16)
        {
            _output.Write((byte)(fixHeader | count));
        }
        else if (count <= ushort.MaxValue)
        {
            var span = _output.Take(3);
            span[0] = header16;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1), (ushort)count);
        }
        else
        {
            var span = _output.Take(5);
            span[0] = header32;
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1), (uint)count);
        }
    }

    private void Close()
    {
        var position = _stack.Positions[_depth];

        if (position >= 0)
        {
            BinaryPrimitives.WriteUInt32BigEndian(_output.Slice(position + 1, 4), (uint)_stack.Counts[_depth]);
        }

        _depth--;
    }

    private void WriteInteger(long value)
    {
        if (value >= 0)
        {
            if (value <= 0x7f)
            {
                _output.Write((byte)value);
            }
            else if (value <= byte.MaxValue)
            {
                var span = _output.Take(2);
                span[0] = 0xcc;
                span[1] = (byte)value;
            }
            else if (value <= ushort.MaxValue)
            {
                var span = _output.Take(3);
                span[0] = 0xcd;
                BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1), (ushort)value);
            }
            else if (value <= uint.MaxValue)
            {
                var span = _output.Take(5);
                span[0] = 0xce;
                BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1), (uint)value);
            }
            else
            {
                var span = _output.Take(9);
                span[0] = 0xcf;
                BinaryPrimitives.WriteUInt64BigEndian(span.Slice(1), (ulong)value);
            }
        }
        else if (value >= -32)
        {
            _output.Write((byte)(sbyte)value);
        }
        else if (value >= sbyte.MinValue)
        {
            var span = _output.Take(2);
            span[0] = 0xd0;
            span[1] = (byte)(sbyte)value;
        }
        else if (value >= short.MinValue)
        {
            var span = _output.Take(3);
            span[0] = 0xd1;
            BinaryPrimitives.WriteInt16BigEndian(span.Slice(1), (short)value);
        }
        else if (value >= int.MinValue)
        {
            var span = _output.Take(5);
            span[0] = 0xd2;
            BinaryPrimitives.WriteInt32BigEndian(span.Slice(1), (int)value);
        }
        else
        {
            var span = _output.Take(9);
            span[0] = 0xd3;
            BinaryPrimitives.WriteInt64BigEndian(span.Slice(1), value);
        }
    }

    private void WriteStringHeader(int length)
    {
        if (length < 32)
        {
            _output.Write((byte)(0xa0 | length));
        }
        else if (length <= byte.MaxValue)
        {
            var span = _output.Take(2);
            span[0] = 0xd9;
            span[1] = (byte)length;
        }
        else if (length <= ushort.MaxValue)
        {
            var span = _output.Take(3);
            span[0] = 0xda;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1), (ushort)length);
        }
        else
        {
            var span = _output.Take(5);
            span[0] = 0xdb;
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1), (uint)length);
        }
    }
}
