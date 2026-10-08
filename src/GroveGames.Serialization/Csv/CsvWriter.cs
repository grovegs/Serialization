using System.Buffers.Text;
using System.Text;

namespace GroveGames.Serialization.Csv;

internal struct CsvWriter : IDocumentWriter
{
    private static readonly byte[] s_versionPrefix = Encoding.ASCII.GetBytes("#v=");
    private static readonly byte[] s_true = Encoding.ASCII.GetBytes("true");
    private static readonly byte[] s_false = Encoding.ASCII.GetBytes("false");

    private readonly ByteBuffer _output;
    private readonly ByteBuffer _row;
    private readonly List<byte[]> _header;
    private bool _headerWritten;
    private int _depth;
    private int _column;

    public CsvWriter(ByteBuffer output)
    {
        _output = output;
        _row = new ByteBuffer(256);
        _header = [];
        _headerWritten = false;
        _depth = 0;
        _column = 0;
    }

    public void BeginEnvelope(int version)
    {
        _output.Write(s_versionPrefix);
        Utf8Formatter.TryFormat(version, _output.GetSpan(11), out var written);
        _output.Advance(written);
        _output.Write((byte)'\n');
    }

    public void EndEnvelope()
    {
    }

    public void BeginArray(int count)
    {
        if (_depth != 0)
        {
            throw NotFlat("a nested list");
        }

        _depth = 1;
    }

    public void EndArray()
    {
        _depth = 0;
    }

    public void BeginObject(int fieldCount)
    {
        if (_depth == 0)
        {
            throw NotFlat("a single object at the root");
        }

        if (_depth != 1)
        {
            throw NotFlat("a nested object");
        }

        _depth = 2;
        _column = 0;
        _row.Reset();
    }

    public void WriteField(byte[] utf8Name)
    {
        if (!_headerWritten)
        {
            _header.Add(utf8Name);
        }

        if (_column++ > 0)
        {
            _row.Write((byte)',');
        }
    }

    public void EndObject()
    {
        _depth = 1;

        if (!_headerWritten)
        {
            for (var i = 0; i < _header.Count; i++)
            {
                if (i > 0)
                {
                    _output.Write((byte)',');
                }

                _output.Write(_header[i]);
            }

            _output.Write((byte)'\n');
            _headerWritten = true;
        }

        _output.Write(_row.WrittenSpan);
        _output.Write((byte)'\n');
    }

    public void WriteInt32(int value)
    {
        WriteInt64(value);
    }

    public void WriteInt64(long value)
    {
        EnsureCell();
        Utf8Formatter.TryFormat(value, _row.GetSpan(20), out var written);
        _row.Advance(written);
    }

    public void WriteSingle(float value)
    {
        EnsureCell();
        Utf8Formatter.TryFormat(value, _row.GetSpan(32), out var written);
        _row.Advance(written);
    }

    public void WriteDouble(double value)
    {
        EnsureCell();
        Utf8Formatter.TryFormat(value, _row.GetSpan(32), out var written);
        _row.Advance(written);
    }

    public void WriteBool(bool value)
    {
        EnsureCell();
        _row.Write(value ? s_true : s_false);
    }

    public void WriteNull()
    {
        EnsureCell();
    }

    public void WriteString(string? value)
    {
        EnsureCell();

        if (value == null)
        {
            return;
        }

        var buffer = ScratchBuffers.Text();
        var written = Encoding.UTF8.GetBytes(value.AsSpan(), buffer.GetSpan(Encoding.UTF8.GetMaxByteCount(value.Length)));
        buffer.Advance(written);
        WriteCell(buffer.WrittenSpan);
    }

    public void WriteStringUtf8(ReadOnlySpan<byte> utf8)
    {
        EnsureCell();
        WriteCell(utf8);
    }

    private static NotSupportedException NotFlat(string what)
    {
        return new NotSupportedException($"CSV only supports a list of flat rows, but this type has {what}. Use JSON or MessagePack for it.");
    }

    private readonly void EnsureCell()
    {
        if (_depth != 2)
        {
            throw NotFlat("values outside a row");
        }
    }

    private readonly void WriteCell(ReadOnlySpan<byte> utf8)
    {
        var quote = false;

        for (var i = 0; i < utf8.Length; i++)
        {
            var value = utf8[i];

            if (value == ',' || value == '"' || value == '\n' || value == '\r')
            {
                quote = true;
                break;
            }
        }

        if (!quote)
        {
            _row.Write(utf8);
            return;
        }

        _row.Write((byte)'"');

        for (var i = 0; i < utf8.Length; i++)
        {
            if (utf8[i] == '"')
            {
                _row.Write((byte)'"');
            }

            _row.Write(utf8[i]);
        }

        _row.Write((byte)'"');
    }
}
