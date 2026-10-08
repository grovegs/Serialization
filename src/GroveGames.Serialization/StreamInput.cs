namespace GroveGames.Serialization;

internal static class StreamInput
{
    public static ReadOnlyMemory<byte> ReadAll(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var buffer = new ByteBuffer(4096);
        buffer.ReadFrom(input);
        return buffer.WrittenMemory;
    }
}
