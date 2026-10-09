using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace GroveGames.Serialization;

internal sealed class TimeSpanFormatter : IFormatter<TimeSpan>
{
    private const int MaxLength = 26;

    public void Write<TWriter>(ref TWriter writer, TimeSpan value)
        where TWriter : struct, IFormatWriter
    {
        Span<byte> buffer = stackalloc byte[MaxLength];
        Utf8Formatter.TryFormat(value, buffer, out var written, 'c');
        writer.WriteStringUtf8(buffer.Slice(0, written));
    }

    public TimeSpan Read<TReader>(ref TReader reader)
        where TReader : struct, IFormatReader
    {
        if (!reader.ReadStringUtf8(out var utf8))
        {
            throw new FormatException("Expected a time span string, but found null.");
        }

        if (Utf8Parser.TryParse(utf8, out TimeSpan value, out var consumed, 'c') && consumed == utf8.Length)
        {
            return value;
        }

        if (TimeSpan.TryParse(Encoding.UTF8.GetString(utf8), CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        throw new FormatException($"'{Encoding.UTF8.GetString(utf8)}' is not a valid time span.");
    }

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter
    {
        Write(ref writer, Read(ref reader));
    }
}
