namespace GroveGames.Serialization;

public static class SerializerExtensions
{
    public static byte[] Serialize<T>(this ISerializer serializer, T? value)
    {
        var output = ScratchBuffers.RentOutput();

        try
        {
            serializer.Serialize(value, output);
            return output.ToArray();
        }
        finally
        {
            ScratchBuffers.ReturnOutput(output);
        }
    }

    public static void Serialize<T>(this ISerializer serializer, T? value, Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var buffer = ScratchBuffers.RentOutput();

        try
        {
            serializer.Serialize(value, buffer);
            buffer.WriteTo(output);
        }
        finally
        {
            ScratchBuffers.ReturnOutput(buffer);
        }
    }

    public static T? Deserialize<T>(this ISerializer serializer, Stream input)
    {
        return serializer.Deserialize<T>(StreamInput.ReadAll(input));
    }
}
