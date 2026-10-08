using System.Buffers.Binary;
using System.Text;

namespace GroveGames.Serialization.MessagePack;

internal struct MessagePackReader : IDocumentReader
{
    private const byte VersionExtension = 0x56;

    private readonly byte[] _buffer;
    private readonly int _end;
    private readonly MessagePackStack _stack;
    private int _position;
    private int _depth;

    public MessagePackReader(ReadOnlyMemory<byte> data, MessagePackStack stack)
    {
        var input = new InputSegment(data);
        _buffer = input.Buffer;
        _position = input.Start;
        _end = input.End;
        _stack = stack;
        _depth = 0;
    }

    public bool TryReadEnvelope(out int version)
    {
        version = 1;

        if (_position >= _end)
        {
            return false;
        }

        var header = _buffer[_position];

        if (header == 0xd4)
        {
            Need(3);
            ExpectVersionExtension(_buffer[_position + 1]);
            version = _buffer[_position + 2];
            _position += 3;
        }
        else if (header == 0xd6)
        {
            Need(6);
            ExpectVersionExtension(_buffer[_position + 1]);
            var stored = BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(_buffer, _position + 2, 4));

            if (stored > int.MaxValue)
            {
                throw Error($"version {stored} is not valid");
            }

            version = (int)stored;
            _position += 6;
        }
        else
        {
            return false;
        }

        if (version < 1)
        {
            throw Error($"version {version} is not valid");
        }

        return true;
    }

    public readonly void EndEnvelope()
    {
    }

    public void EndDocument()
    {
        if (_position != _end)
        {
            throw Error("unexpected data after the value");
        }
    }

    public TokenType Peek()
    {
        Need(1);
        var value = _buffer[_position];

        if (value <= 0x7f || value >= 0xe0)
        {
            return TokenType.Integer;
        }

        if (value <= 0x8f)
        {
            return TokenType.BeginObject;
        }

        if (value <= 0x9f)
        {
            return TokenType.BeginArray;
        }

        if (value <= 0xbf)
        {
            return TokenType.String;
        }

        switch (value)
        {
            case 0xc0:
                return TokenType.Null;
            case 0xc2:
            case 0xc3:
                return TokenType.Bool;
            case 0xca:
            case 0xcb:
                return TokenType.Float;
            case 0xd9:
            case 0xda:
            case 0xdb:
                return TokenType.String;
            case 0xdc:
            case 0xdd:
                return TokenType.BeginArray;
            case 0xde:
            case 0xdf:
                return TokenType.BeginObject;
        }

        if (value >= 0xcc && value <= 0xd3)
        {
            return TokenType.Integer;
        }

        throw Error($"unsupported type byte 0x{value:x2}");
    }

    public void ReadObjectStart()
    {
        Push(ReadMapHeader());
    }

    public void ReadArrayStart()
    {
        Push(ReadArrayHeader());
    }

    public bool TryReadField(FieldTable fields, out int index)
    {
        if (!NextItem())
        {
            index = -1;
            return false;
        }

        if (!ReadStringUtf8(out var name))
        {
            throw Error("a field name is null");
        }

        index = fields.IndexOf(name);
        return true;
    }

    public bool TryReadFieldName(out string name)
    {
        if (!NextItem())
        {
            name = string.Empty;
            return false;
        }

        name = ReadString() ?? throw Error("a field name is null");
        return true;
    }

    public bool TryReadNextElement()
    {
        return NextItem();
    }

    public int ReadInt32()
    {
        var value = ReadInt64();

        if (value < int.MinValue || value > int.MaxValue)
        {
            throw Error($"{value} does not fit in a 32-bit integer");
        }

        return (int)value;
    }

    public long ReadInt64()
    {
        var value = ReadByte();

        if (value <= 0x7f)
        {
            return value;
        }

        if (value >= 0xe0)
        {
            return (sbyte)value;
        }

        switch (value)
        {
            case 0xcc:
                return ReadByte();
            case 0xcd:
                return ReadUInt16();
            case 0xce:
                return ReadUInt32();
            case 0xcf:
                var unsigned = ReadUInt64();

                if (unsigned > long.MaxValue)
                {
                    throw Error($"{unsigned} does not fit in a 64-bit integer");
                }

                return (long)unsigned;
            case 0xd0:
                return (sbyte)ReadByte();
            case 0xd1:
                return (short)ReadUInt16();
            case 0xd2:
                return (int)ReadUInt32();
            case 0xd3:
                return (long)ReadUInt64();
        }

        _position--;
        throw Error("expected an integer");
    }

    public float ReadSingle()
    {
        Need(1);

        if (_buffer[_position] == 0xca)
        {
            _position++;
            return BitConverter.Int32BitsToSingle((int)ReadUInt32());
        }

        return (float)ReadDouble();
    }

    public double ReadDouble()
    {
        Need(1);
        var value = _buffer[_position];

        if (value == 0xcb)
        {
            _position++;
            return BitConverter.Int64BitsToDouble((long)ReadUInt64());
        }

        if (value == 0xca)
        {
            _position++;
            return BitConverter.Int32BitsToSingle((int)ReadUInt32());
        }

        return ReadInt64();
    }

    public bool ReadBool()
    {
        var value = ReadByte();

        if (value == 0xc3)
        {
            return true;
        }

        if (value == 0xc2)
        {
            return false;
        }

        _position--;
        throw Error("expected a bool");
    }

    public string? ReadString()
    {
        if (!ReadStringUtf8(out var utf8))
        {
            return null;
        }

        return utf8.Length == 0 ? string.Empty : Encoding.UTF8.GetString(utf8);
    }

    public bool ReadStringUtf8(out ReadOnlySpan<byte> utf8)
    {
        var length = ReadStringHeader();

        if (length < 0)
        {
            utf8 = default;
            return false;
        }

        Need(length);
        utf8 = new ReadOnlySpan<byte>(_buffer, _position, length);
        _position += length;
        return true;
    }

    public void Skip()
    {
        SkipValue(_depth);
    }

    private void ExpectVersionExtension(byte type)
    {
        if (type != VersionExtension)
        {
            throw Error($"unsupported extension type {type}");
        }
    }

    private FormatException Error(string message)
    {
        return new FormatException($"MessagePack at byte {_position}: {message}.");
    }

    private void Need(long count)
    {
        if (_position + count > _end)
        {
            throw Error("unexpected end of data");
        }
    }

    private byte ReadByte()
    {
        Need(1);
        return _buffer[_position++];
    }

    private ushort ReadUInt16()
    {
        Need(2);
        var value = BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(_buffer, _position, 2));
        _position += 2;
        return value;
    }

    private uint ReadUInt32()
    {
        Need(4);
        var value = BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(_buffer, _position, 4));
        _position += 4;
        return value;
    }

    private ulong ReadUInt64()
    {
        Need(8);
        var value = BinaryPrimitives.ReadUInt64BigEndian(new ReadOnlySpan<byte>(_buffer, _position, 8));
        _position += 8;
        return value;
    }

    private int ReadCount(uint count)
    {
        if (count > int.MaxValue)
        {
            throw Error($"count {count} is too large");
        }

        return (int)count;
    }

    private int ReadMapHeader()
    {
        var value = ReadByte();

        if (value >= 0x80 && value <= 0x8f)
        {
            return value & 0x0f;
        }

        if (value == 0xde)
        {
            return ReadUInt16();
        }

        if (value == 0xdf)
        {
            return ReadCount(ReadUInt32());
        }

        _position--;
        throw Error("expected a map");
    }

    private int ReadArrayHeader()
    {
        var value = ReadByte();

        if (value >= 0x90 && value <= 0x9f)
        {
            return value & 0x0f;
        }

        if (value == 0xdc)
        {
            return ReadUInt16();
        }

        if (value == 0xdd)
        {
            return ReadCount(ReadUInt32());
        }

        _position--;
        throw Error("expected an array");
    }

    private int ReadStringHeader()
    {
        var value = ReadByte();

        if (value >= 0xa0 && value <= 0xbf)
        {
            return value & 0x1f;
        }

        switch (value)
        {
            case 0xc0:
                return -1;
            case 0xd9:
                return ReadByte();
            case 0xda:
                return ReadUInt16();
            case 0xdb:
                return ReadCount(ReadUInt32());
        }

        _position--;
        throw Error("expected a string");
    }

    private void Push(int count)
    {
        if (++_depth > MessagePackStack.MaxDepth)
        {
            throw Error($"nesting is deeper than {MessagePackStack.MaxDepth} levels");
        }

        _stack.Counts[_depth] = count;
    }

    private bool NextItem()
    {
        if (_stack.Counts[_depth] == 0)
        {
            _depth--;
            return false;
        }

        _stack.Counts[_depth]--;
        return true;
    }

    private void SkipValue(int depth)
    {
        if (depth >= MessagePackStack.MaxDepth)
        {
            throw Error($"nesting is deeper than {MessagePackStack.MaxDepth} levels");
        }

        switch (Peek())
        {
            case TokenType.BeginObject:
                var fields = (long)ReadMapHeader() * 2;

                for (long i = 0; i < fields; i++)
                {
                    SkipValue(depth + 1);
                }

                break;
            case TokenType.BeginArray:
                var items = ReadArrayHeader();

                for (var i = 0; i < items; i++)
                {
                    SkipValue(depth + 1);
                }

                break;
            case TokenType.String:
                var length = ReadStringHeader();

                if (length > 0)
                {
                    Need(length);
                    _position += length;
                }

                break;
            case TokenType.Integer:
                ReadInt64();
                break;
            case TokenType.Float:
                ReadDouble();
                break;
            default:
                _position++;
                break;
        }
    }
}
