using System.Buffers;

namespace GroveGames.Serialization;

internal static class FormatOperations
{
    public static void Serialize<T>(IFormat format, T? value, IBufferWriter<byte> output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var buffer = output as ByteBuffer ?? ScratchBuffers.Output();
        format.Serialize(value, buffer);
        Flush(output, buffer);
    }

    public static void Convert<T>(IFormat from, IFormat to, ReadOnlyMemory<byte> data, IBufferWriter<byte> output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var buffer = output as ByteBuffer ?? ScratchBuffers.Output();
        from.Convert<T>(data, to, buffer);
        Flush(output, buffer);
    }

    public static IFormat GetFormat(ISerializer serializer, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(serializer, parameterName);
        return serializer as IFormat ?? throw new ArgumentException($"{serializer.GetType()} is not a built-in serializer.", parameterName);
    }

    private static void Flush(IBufferWriter<byte> output, ByteBuffer buffer)
    {
        if (!ReferenceEquals(output, buffer))
        {
            output.Write(buffer.WrittenSpan);
        }
    }
}
