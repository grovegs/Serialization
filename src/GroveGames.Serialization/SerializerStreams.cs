namespace GroveGames.Serialization;

internal static class SerializerStreams
{
    public static byte[] Serialize<T>(ISerializer serializer, T? value)
    {
        var output = ScratchBuffers.Output();
        serializer.Serialize(value, output);
        return output.ToArray();
    }

    public static void Serialize<T>(ISerializer serializer, T? value, Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var buffer = ScratchBuffers.Output();
        serializer.Serialize(value, buffer);
        buffer.WriteTo(output);
    }

    public static T? Deserialize<T>(ISerializer serializer, Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var buffer = new ByteBuffer(4096);
        buffer.ReadFrom(input);
        return serializer.Deserialize<T>(buffer.WrittenMemory);
    }
}
