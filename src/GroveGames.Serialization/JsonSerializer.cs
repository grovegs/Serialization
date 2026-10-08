using GroveGames.Serialization.Json;

namespace GroveGames.Serialization;

public sealed class JsonSerializer : ISerializer, IFormat
{
    public JsonSerializer(SerializerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        Registry = registry;
    }

    public SerializerRegistry Registry { get; }

    public byte[] Serialize<T>(T? value)
    {
        return SerializerStreams.Serialize(this, value);
    }

    public void Serialize<T>(T? value, ByteBuffer output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var writer = new JsonWriter(output);
        Pipeline.Write(ref writer, value, Registry);
    }

    public void Serialize<T>(T? value, Stream output)
    {
        SerializerStreams.Serialize(this, value, output);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        var reader = new JsonReader(data);
        return Pipeline.Read<T, JsonReader>(ref reader, Registry);
    }

    public T? Deserialize<T>(Stream input)
    {
        return SerializerStreams.Deserialize<T>(this, input);
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output, SerializerRegistry registry)
    {
        var reader = new JsonReader(data);
        target.ConvertFrom<T, JsonReader>(ref reader, output, registry);
    }

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output, SerializerRegistry registry)
    {
        var writer = new JsonWriter(output);
        Pipeline.Convert<T, TReader, JsonWriter>(ref reader, ref writer, registry);
    }
}
