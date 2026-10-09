using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace GroveGames.Serialization;

internal sealed class DateTimeOffsetFormatter : IFormatter<DateTimeOffset>
{
    private const int MaxLength = 33;

    public void Write<TWriter>(ref TWriter writer, DateTimeOffset value)
        where TWriter : struct, IFormatWriter
    {
        Span<byte> buffer = stackalloc byte[MaxLength];
        Utf8Formatter.TryFormat(value, buffer, out var written, 'O');
        writer.WriteStringUtf8(buffer.Slice(0, written));
    }

    public DateTimeOffset Read<TReader>(ref TReader reader)
        where TReader : struct, IFormatReader
    {
        if (!reader.ReadStringUtf8(out var utf8))
        {
            throw new FormatException("Expected a date and time with offset string, but found null.");
        }

        if (Utf8Parser.TryParse(utf8, out DateTimeOffset value, out var consumed, 'O') && consumed == utf8.Length)
        {
            return value;
        }

        if (DateTimeOffset.TryParse(Encoding.UTF8.GetString(utf8), CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            return value;
        }

        throw new FormatException($"'{Encoding.UTF8.GetString(utf8)}' is not a valid date and time with offset.");
    }

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter
    {
        Write(ref writer, Read(ref reader));
    }
}
