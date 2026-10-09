using System.Buffers.Text;
using System.Text;

namespace GroveGames.Serialization;

internal sealed class GuidFormatter : IFormatter<Guid>
{
    private const int MaxLength = 36;

    public void Write<TWriter>(ref TWriter writer, Guid value)
        where TWriter : struct, IFormatWriter
    {
        Span<byte> buffer = stackalloc byte[MaxLength];
        Utf8Formatter.TryFormat(value, buffer, out var written, 'D');
        writer.WriteStringUtf8(buffer.Slice(0, written));
    }

    public Guid Read<TReader>(ref TReader reader)
        where TReader : struct, IFormatReader
    {
        if (!reader.ReadStringUtf8(out var utf8))
        {
            throw new FormatException("Expected a GUID string, but found null.");
        }

        if (Utf8Parser.TryParse(utf8, out Guid value, out var consumed, 'D') && consumed == utf8.Length)
        {
            return value;
        }

        if (Guid.TryParse(Encoding.UTF8.GetString(utf8), out value))
        {
            return value;
        }

        throw new FormatException($"'{Encoding.UTF8.GetString(utf8)}' is not a valid GUID.");
    }

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter
    {
        Write(ref writer, Read(ref reader));
    }
}
