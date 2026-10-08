using System.Globalization;
using System.Text;

namespace GroveGames.Serialization.Csv;

internal struct CsvReader : IDocumentReader
{
    private readonly string[] _headers;
    private readonly List<string[]> _rows;
    private readonly int _version;
    private readonly bool _hasVersionLine;
    private bool _envelopeRead;
    private int _row;
    private int _column;
    private int _depth;

    public CsvReader(ReadOnlyMemory<byte> data)
    {
        var records = Parse(Encoding.UTF8.GetString(data.Span));
        _version = 1;
        _hasVersionLine = false;
        _envelopeRead = false;

        if (records.Count > 0 && records[0][0].StartsWith("#v=", StringComparison.Ordinal))
        {
            if (!int.TryParse(records[0][0].Substring(3), NumberStyles.None, CultureInfo.InvariantCulture, out _version) || _version < 1)
            {
                throw new FormatException($"CSV version line '{records[0][0]}' is not valid.");
            }

            records.RemoveAt(0);
            _hasVersionLine = true;
        }

        _headers = records.Count > 0 ? records[0] : [];
        _rows = records.Count > 1 ? records.GetRange(1, records.Count - 1) : [];
        _row = -1;
        _column = -1;
        _depth = 0;
    }

    public int ReadEnvelope()
    {
        _envelopeRead = true;
        return _version;
    }

    public readonly void EndEnvelope()
    {
    }

    public readonly void EndDocument()
    {
        if (_hasVersionLine && !_envelopeRead)
        {
            throw new FormatException("CSV starts with a version line, so it must be read with a VersionedSerializer.");
        }
    }

    public readonly TokenType Peek()
    {
        if (_depth == 0)
        {
            return TokenType.BeginArray;
        }

        if (_depth == 1)
        {
            return TokenType.BeginObject;
        }

        return Cell.Length == 0 ? TokenType.Null : TokenType.Text;
    }

    public void ReadArrayStart()
    {
        _depth = 1;
        _row = -1;
    }

    public bool TryReadNextElement()
    {
        if (++_row < _rows.Count)
        {
            return true;
        }

        _depth = 0;
        return false;
    }

    public void ReadObjectStart()
    {
        if (_depth != 1)
        {
            throw new FormatException("CSV only contains a list of flat rows.");
        }

        _depth = 2;
        _column = -1;
    }

    public bool TryReadField(FieldTable fields, out int index)
    {
        if (!NextColumn())
        {
            index = -1;
            return false;
        }

        index = fields.IndexOf(_headers[_column]);
        return true;
    }

    public bool TryReadFieldName(out string name)
    {
        if (!NextColumn())
        {
            name = string.Empty;
            return false;
        }

        name = _headers[_column];
        return true;
    }

    public readonly int ReadInt32()
    {
        if (!int.TryParse(Cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw Error("a 32-bit integer");
        }

        return value;
    }

    public readonly long ReadInt64()
    {
        if (!long.TryParse(Cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw Error("a 64-bit integer");
        }

        return value;
    }

    public readonly float ReadSingle()
    {
        if (!float.TryParse(Cell, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw Error("a number");
        }

        return value;
    }

    public readonly double ReadDouble()
    {
        if (!double.TryParse(Cell, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw Error("a number");
        }

        return value;
    }

    public readonly bool ReadBool()
    {
        return Cell switch
        {
            "true" => true,
            "false" => false,
            _ => throw Error("true or false")
        };
    }

    public readonly string? ReadString()
    {
        return Cell;
    }

    public readonly bool ReadStringUtf8(out ReadOnlySpan<byte> utf8)
    {
        var cell = Cell;
        var buffer = ScratchBuffers.Text();
        var written = Encoding.UTF8.GetBytes(cell.AsSpan(), buffer.GetSpan(Encoding.UTF8.GetMaxByteCount(cell.Length)));
        buffer.Advance(written);
        utf8 = buffer.WrittenSpan;
        return true;
    }

    public readonly void Skip()
    {
    }

    private readonly string Cell
    {
        get
        {
            if (_depth != 2 || _row < 0 || _row >= _rows.Count || _column < 0 || _column >= _rows[_row].Length)
            {
                throw new FormatException("CSV has no cell at this position.");
            }

            return _rows[_row][_column];
        }
    }

    private readonly FormatException Error(string expected)
    {
        return new FormatException($"CSV row {_row + 1}, column '{_headers[_column]}': '{Cell}' is not {expected}.");
    }

    private bool NextColumn()
    {
        if (++_column < _headers.Length && _column < _rows[_row].Length)
        {
            return true;
        }

        _depth = 1;
        return false;
    }

    private static List<string[]> Parse(string text)
    {
        var records = new List<string[]>();
        var fields = new List<string>();
        var builder = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];

            if (quoted)
            {
                if (character != '"')
                {
                    builder.Append(character);
                }
                else if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                }
                else
                {
                    quoted = false;
                }

                continue;
            }

            switch (character)
            {
                case '"':
                    quoted = true;
                    break;
                case ',':
                    fields.Add(builder.ToString());
                    builder.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    fields.Add(builder.ToString());
                    builder.Clear();
                    records.Add([.. fields]);
                    fields.Clear();
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        if (quoted)
        {
            throw new FormatException("CSV ends inside a quoted cell.");
        }

        if (builder.Length > 0 || fields.Count > 0)
        {
            fields.Add(builder.ToString());
            records.Add([.. fields]);
        }

        return records;
    }
}
