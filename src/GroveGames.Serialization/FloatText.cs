using System.Buffers.Text;
#if NETSTANDARD2_1
using System.Globalization;
#endif

namespace GroveGames.Serialization;

internal static class FloatText
{
    public const int MaxLength = 32;

    // Mono's Utf8Formatter formats like ToString("G"), which keeps 15 digits of a double and 7 of a float and does not
    // round-trip, and it rejects 'R' and 'G' with a precision. Formatting "R" into a char span gives the shortest text
    // that reads back as the same value there too, without allocating. Modern .NET's Utf8Formatter already does that.
    public static int Write(double value, Span<byte> output)
    {
#if NETSTANDARD2_1
        Span<char> text = stackalloc char[MaxLength];
        value.TryFormat(text, out var written, "R", CultureInfo.InvariantCulture);
        return Copy(text.Slice(0, written), output);
#else
        Utf8Formatter.TryFormat(value, output, out var written);
        return written;
#endif
    }

    public static int Write(float value, Span<byte> output)
    {
#if NETSTANDARD2_1
        Span<char> text = stackalloc char[MaxLength];
        value.TryFormat(text, out var written, "R", CultureInfo.InvariantCulture);
        return Copy(text.Slice(0, written), output);
#else
        Utf8Formatter.TryFormat(value, output, out var written);
        return written;
#endif
    }

#if NETSTANDARD2_1
    private static int Copy(ReadOnlySpan<char> text, Span<byte> output)
    {
        for (var i = 0; i < text.Length; i++)
        {
            output[i] = (byte)text[i];
        }

        return text.Length;
    }
#endif
}
