namespace GroveGames.Serialization;

public static class SerializerExtensions
{
    public static byte[] Serialize<T>(this ISerializer serializer, T? value)
    {
        var output = ScratchBuffers.Output();
        serializer.Serialize(value, output);
        return output.ToArray();
    }

    public static void Serialize<T>(this ISerializer serializer, T? value, Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var buffer = ScratchBuffers.Output();
        serializer.Serialize(value, buffer);
        buffer.WriteTo(output);
    }

    public static T? Deserialize<T>(this ISerializer serializer, Stream input)
    {
        return serializer.Deserialize<T>(StreamInput.ReadAll(input));
    }
}
