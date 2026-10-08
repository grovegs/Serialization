using System.Buffers.Text;
using System.Text;

namespace GroveGames.Serialization.Json;

internal struct JsonWriter : IDocumentWriter
{
    private const int MaxDepth = 63;

    private static readonly byte[] s_true = Encoding.ASCII.GetBytes("true");
    private static readonly byte[] s_false = Encoding.ASCII.GetBytes("false");
    private static readonly byte[] s_null = Encoding.ASCII.GetBytes("null");
    private static readonly byte[] s_nan = Encoding.ASCII.GetBytes("\"NaN\"");
    private static readonly byte[] s_positiveInfinity = Encoding.ASCII.GetBytes("\"Infinity\"");
    private static readonly byte[] s_negativeInfinity = Encoding.ASCII.GetBytes("\"-Infinity\"");
    private static readonly byte[] s_hex = Encoding.ASCII.GetBytes("0123456789abcdef");

    private readonly ByteBuffer _output;
    private ulong _hasItems;
    private int _depth;
    private bool _afterName;

    public JsonWriter(ByteBuffer output)
    {
        _output = output;
        _hasItems = 0;
        _depth = 0;
        _afterName = false;
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
        Prefix();
        _output.Write((byte)'{');
        Push();
    }

    public void EndObject()
    {
        _depth--;
        _output.Write((byte)'}');
    }

    public void BeginArray(int count)
    {
        Prefix();
        _output.Write((byte)'[');
        Push();
    }

    public void EndArray()
    {
        _depth--;
        _output.Write((byte)']');
    }

    public void WriteField(byte[] utf8Name)
    {
        var bit = 1UL << _depth;

        if ((_hasItems & bit) != 0)
        {
            _output.Write((byte)',');
        }
        else
        {
            _hasItems |= bit;
        }

        _output.Write((byte)'"');
        _output.Write(utf8Name);
        _output.Write((byte)'"');
        _output.Write((byte)':');
        _afterName = true;
    }

    public void WriteInt32(int value)
    {
        WriteInt64(value);
    }

    public void WriteInt64(long value)
    {
        Prefix();
        Utf8Formatter.TryFormat(value, _output.GetSpan(20), out var written);
        _output.Advance(written);
    }

    public void WriteSingle(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            WriteNonFinite(value);
            return;
        }

        Prefix();
        Utf8Formatter.TryFormat(value, _output.GetSpan(32), out var written);
        _output.Advance(written);
    }

    public void WriteDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            WriteNonFinite(value);
            return;
        }

        Prefix();
        Utf8Formatter.TryFormat(value, _output.GetSpan(32), out var written);
        _output.Advance(written);
    }

    public void WriteBool(bool value)
    {
        Prefix();
        _output.Write(value ? s_true : s_false);
    }

    public void WriteNull()
    {
        Prefix();
        _output.Write(s_null);
    }

    public void WriteString(string? value)
    {
        if (value == null)
        {
            WriteNull();
            return;
        }

        Prefix();
        _output.Write((byte)'"');
        var start = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];

            if (character != '"' && character != '\\' && character >= 0x20)
            {
                continue;
            }

            Encode(value, start, i);
            WriteEscape(character);
            start = i + 1;
        }

        Encode(value, start, value.Length);
        _output.Write((byte)'"');
    }

    public void WriteStringUtf8(ReadOnlySpan<byte> utf8)
    {
        Prefix();
        _output.Write((byte)'"');
        var start = 0;

        for (var i = 0; i < utf8.Length; i++)
        {
            var value = utf8[i];

            if (value != '"' && value != '\\' && value >= 0x20)
            {
                continue;
            }

            _output.Write(utf8.Slice(start, i - start));
            WriteEscape((char)value);
            start = i + 1;
        }

        _output.Write(utf8.Slice(start));
        _output.Write((byte)'"');
    }

    private void WriteNonFinite(double value)
    {
        Prefix();
        _output.Write(double.IsNaN(value) ? s_nan : value > 0 ? s_positiveInfinity : s_negativeInfinity);
    }

    private void Prefix()
    {
        if (_afterName)
        {
            _afterName = false;
            return;
        }

        if (_depth == 0)
        {
            return;
        }

        var bit = 1UL << _depth;

        if ((_hasItems & bit) != 0)
        {
            _output.Write((byte)',');
        }
        else
        {
            _hasItems |= bit;
        }
    }

    private void Push()
    {
        if (++_depth > MaxDepth)
        {
            throw new InvalidOperationException($"JSON nesting is deeper than {MaxDepth} levels.");
        }

        _hasItems &= ~(1UL << _depth);
    }

    private void Encode(string value, int start, int end)
    {
        if (end <= start)
        {
            return;
        }

        var characters = value.AsSpan(start, end - start);
        var written = Encoding.UTF8.GetBytes(characters, _output.GetSpan(Encoding.UTF8.GetMaxByteCount(characters.Length)));
        _output.Advance(written);
    }

    private void WriteEscape(char character)
    {
        _output.Write((byte)'\\');

        switch (character)
        {
            case '"':
                _output.Write((byte)'"');
                break;
            case '\\':
                _output.Write((byte)'\\');
                break;
            case '\n':
                _output.Write((byte)'n');
                break;
            case '\r':
                _output.Write((byte)'r');
                break;
            case '\t':
                _output.Write((byte)'t');
                break;
            default:
                _output.Write((byte)'u');
                _output.Write((byte)'0');
                _output.Write((byte)'0');
                _output.Write(s_hex[character >> 4]);
                _output.Write(s_hex[character & 0xF]);
                break;
        }
    }
}
