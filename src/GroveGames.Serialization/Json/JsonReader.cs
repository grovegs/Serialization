using System.Buffers.Text;
using System.Text;

namespace GroveGames.Serialization.Json;

internal struct JsonReader : IDocumentReader
{
    private const int MaxDepth = 63;

    private static readonly byte[] s_true = Encoding.ASCII.GetBytes("true");
    private static readonly byte[] s_false = Encoding.ASCII.GetBytes("false");
    private static readonly byte[] s_null = Encoding.ASCII.GetBytes("null");

    private readonly byte[] _buffer;
    private readonly int _end;
    private int _position;
    private int _depth;
    private ulong _first;

    public JsonReader(ReadOnlyMemory<byte> data)
    {
        var input = new InputSegment(data);
        _buffer = input.Buffer;
        _position = input.Start;
        _end = input.End;
        _depth = 0;
        _first = 0;
    }

    public bool TryReadEnvelope(out int version)
    {
        version = 1;
        SkipWhitespace();

        if (_position >= _end || _buffer[_position] != '{')
        {
            return false;
        }

        var position = _position;
        var depth = _depth;
        var first = _first;
        ReadObjectStart();

        if (!TryReadField(Envelope.Fields, out var field) || field != Envelope.VersionField)
        {
            _position = position;
            _depth = depth;
            _first = first;
            return false;
        }

        version = ReadInt32();

        if (version < 1)
        {
            throw Error($"version {version} is not valid");
        }

        if (!TryReadField(Envelope.Fields, out field) || field != Envelope.DataField)
        {
            throw Error("the envelope has no \"data\" after \"$v\"");
        }

        return true;
    }

    public void EndEnvelope()
    {
        while (TryReadField(Envelope.Fields, out _))
        {
            Skip();
        }
    }

    public void EndDocument()
    {
        SkipWhitespace();

        if (_position != _end)
        {
            throw Error("unexpected data after the value");
        }
    }

    public TokenType Peek()
    {
        SkipWhitespace();

        switch (Current())
        {
            case (byte)'{':
                return TokenType.BeginObject;
            case (byte)'[':
                return TokenType.BeginArray;
            case (byte)'"':
                return TokenType.String;
            case (byte)'t':
            case (byte)'f':
                return TokenType.Bool;
            case (byte)'n':
                return TokenType.Null;
        }

        for (var i = _position; i < _end; i++)
        {
            var value = _buffer[i];

            if (value == '.' || value == 'e' || value == 'E')
            {
                return TokenType.Float;
            }

            if (!IsNumberCharacter(value))
            {
                break;
            }
        }

        return TokenType.Integer;
    }

    public void ReadObjectStart()
    {
        Expect((byte)'{');
        Push();
    }

    public void ReadArrayStart()
    {
        Expect((byte)'[');
        Push();
    }

    public bool TryReadField(FieldTable fields, out int index)
    {
        if (!Next((byte)'}'))
        {
            index = -1;
            return false;
        }

        Expect((byte)'"');
        var start = _position;
        var escaped = false;

        while (Current() != '"')
        {
            if (_buffer[_position] == '\\')
            {
                escaped = true;
                break;
            }

            _position++;
        }

        if (escaped)
        {
            _position = start - 1;
            ReadStringUtf8(out var name);
            index = fields.IndexOf(name);
        }
        else
        {
            index = fields.IndexOf(new ReadOnlySpan<byte>(_buffer, start, _position - start));
            _position++;
        }

        Expect((byte)':');
        return true;
    }

    public bool TryReadFieldName(out string name)
    {
        if (!Next((byte)'}'))
        {
            name = string.Empty;
            return false;
        }

        name = ReadString() ?? throw Error("a field name is null");
        Expect((byte)':');
        return true;
    }

    public bool TryReadNextElement()
    {
        return Next((byte)']');
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
        SkipWhitespace();

        if (!Utf8Parser.TryParse(new ReadOnlySpan<byte>(_buffer, _position, _end - _position), out long value, out var consumed))
        {
            throw Error("expected an integer");
        }

        _position += consumed;
        return value;
    }

    public float ReadSingle()
    {
        return (float)ReadDouble();
    }

    public double ReadDouble()
    {
        SkipWhitespace();

        if (Current() == '"')
        {
            return ReadNonFinite();
        }

        if (!Utf8Parser.TryParse(new ReadOnlySpan<byte>(_buffer, _position, _end - _position), out double value, out var consumed))
        {
            throw Error("expected a number");
        }

        _position += consumed;
        return value;
    }

    public bool ReadBool()
    {
        SkipWhitespace();

        if (Current() == 't')
        {
            ExpectLiteral(s_true);
            return true;
        }

        if (Current() == 'f')
        {
            ExpectLiteral(s_false);
            return false;
        }

        throw Error("expected true or false");
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
        SkipWhitespace();

        if (Current() == 'n')
        {
            ExpectLiteral(s_null);
            utf8 = default;
            return false;
        }

        Expect((byte)'"');
        var start = _position;
        var escaped = false;

        while (Current() != '"')
        {
            if (_buffer[_position] == '\\')
            {
                escaped = true;
                _position++;
            }

            _position++;
        }

        var end = _position++;
        utf8 = escaped ? Unescape(start, end) : new ReadOnlySpan<byte>(_buffer, start, end - start);
        return true;
    }

    public void Skip()
    {
        switch (Peek())
        {
            case TokenType.BeginObject:
                ReadObjectStart();

                while (TryReadField(FieldTable.Empty, out _))
                {
                    Skip();
                }

                break;
            case TokenType.BeginArray:
                ReadArrayStart();

                while (TryReadNextElement())
                {
                    Skip();
                }

                break;
            case TokenType.String:
                ReadStringUtf8(out _);
                break;
            case TokenType.Bool:
                ReadBool();
                break;
            case TokenType.Null:
                ExpectLiteral(s_null);
                break;
            default:
                var start = _position;

                while (_position < _end && IsNumberCharacter(_buffer[_position]))
                {
                    _position++;
                }

                if (_position == start)
                {
                    throw Error("expected a value");
                }

                break;
        }
    }

    private static bool IsNumberCharacter(byte value)
    {
        return (value >= '0' && value <= '9') || value == '-' || value == '+' || value == '.' || value == 'e' || value == 'E';
    }

    private FormatException Error(string message)
    {
        return new FormatException($"JSON at byte {_position}: {message}.");
    }

    private void SkipWhitespace()
    {
        while (_position < _end)
        {
            var value = _buffer[_position];

            if (value == ' ' || value == '\n' || value == '\r' || value == '\t')
            {
                _position++;
            }
            else
            {
                break;
            }
        }
    }

    private byte Current()
    {
        if (_position >= _end)
        {
            throw Error("unexpected end of data");
        }

        return _buffer[_position];
    }

    private void Expect(byte value)
    {
        SkipWhitespace();

        if (Current() != value)
        {
            throw Error($"expected '{(char)value}'");
        }

        _position++;
    }

    private void ExpectLiteral(byte[] literal)
    {
        if (_end - _position < literal.Length || !new ReadOnlySpan<byte>(_buffer, _position, literal.Length).SequenceEqual(literal))
        {
            throw Error($"expected {Encoding.ASCII.GetString(literal)}");
        }

        _position += literal.Length;
    }

    private void Push()
    {
        if (++_depth > MaxDepth)
        {
            throw Error($"nesting is deeper than {MaxDepth} levels");
        }

        _first |= 1UL << _depth;
    }

    private bool Next(byte close)
    {
        SkipWhitespace();

        if (Current() == close)
        {
            _position++;
            _depth--;
            return false;
        }

        var bit = 1UL << _depth;

        if ((_first & bit) == 0)
        {
            Expect((byte)',');
        }
        else
        {
            _first &= ~bit;
        }

        return true;
    }

    private double ReadNonFinite()
    {
        var start = _position;
        ReadStringUtf8(out var text);

        if (text.SequenceEqual("NaN"u8))
        {
            return double.NaN;
        }

        if (text.SequenceEqual("Infinity"u8))
        {
            return double.PositiveInfinity;
        }

        if (text.SequenceEqual("-Infinity"u8))
        {
            return double.NegativeInfinity;
        }

        _position = start;
        throw Error("expected a number");
    }

    private ReadOnlySpan<byte> Unescape(int start, int end)
    {
        var output = ScratchBuffers.Text();

        for (var i = start; i < end; i++)
        {
            var value = _buffer[i];

            if (value != '\\')
            {
                output.Write(value);
                continue;
            }

            value = _buffer[++i];

            switch (value)
            {
                case (byte)'n':
                    output.Write((byte)'\n');
                    break;
                case (byte)'r':
                    output.Write((byte)'\r');
                    break;
                case (byte)'t':
                    output.Write((byte)'\t');
                    break;
                case (byte)'b':
                    output.Write((byte)'\b');
                    break;
                case (byte)'f':
                    output.Write((byte)'\f');
                    break;
                case (byte)'u':
                    var codePoint = Hex4(i + 1, end);
                    i += 4;

                    if (codePoint >= 0xD800 && codePoint <= 0xDBFF && i + 6 < end && _buffer[i + 1] == '\\' && _buffer[i + 2] == 'u')
                    {
                        var low = Hex4(i + 3, end);

                        if (low >= 0xDC00 && low <= 0xDFFF)
                        {
                            codePoint = 0x10000 + ((codePoint - 0xD800) << 10) + (low - 0xDC00);
                            i += 6;
                        }
                    }

                    WriteCodePoint(output, codePoint);
                    break;
                default:
                    output.Write(value);
                    break;
            }
        }

        return output.WrittenSpan;
    }

    private int Hex4(int at, int end)
    {
        if (at + 4 > end)
        {
            throw Error("incomplete \\u escape");
        }

        var value = 0;

        for (var i = 0; i < 4; i++)
        {
            var hex = _buffer[at + i];
            var digit = hex >= '0' && hex <= '9' ? hex - '0' : hex >= 'a' && hex <= 'f' ? hex - 'a' + 10 : hex >= 'A' && hex <= 'F' ? hex - 'A' + 10 : -1;

            if (digit < 0)
            {
                throw Error("invalid \\u escape");
            }

            value = (value << 4) | digit;
        }

        return value;
    }

    private static void WriteCodePoint(ByteBuffer output, int codePoint)
    {
        if (codePoint < 0x80)
        {
            output.Write((byte)codePoint);
        }
        else if (codePoint < 0x800)
        {
            output.Write((byte)(0xC0 | (codePoint >> 6)));
            output.Write((byte)(0x80 | (codePoint & 0x3F)));
        }
        else if (codePoint < 0x10000)
        {
            output.Write((byte)(0xE0 | (codePoint >> 12)));
            output.Write((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
            output.Write((byte)(0x80 | (codePoint & 0x3F)));
        }
        else
        {
            output.Write((byte)(0xF0 | (codePoint >> 18)));
            output.Write((byte)(0x80 | ((codePoint >> 12) & 0x3F)));
            output.Write((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
            output.Write((byte)(0x80 | (codePoint & 0x3F)));
        }
    }
}
