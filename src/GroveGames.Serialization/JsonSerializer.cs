using System.Buffers;
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

    public void Serialize<T>(T? value, IBufferWriter<byte> output)
    {
        FormatOperations.Serialize(this, value, output, Registry);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        return ((IFormat)this).Deserialize<T>(data, Registry);
    }

    void IFormat.Serialize<T>(T? value, ByteBuffer output, SerializerRegistry registry) where T : default
    {
        var writer = new JsonWriter(output);
        Pipeline.Write(ref writer, value, registry);
    }

    T? IFormat.Deserialize<T>(ReadOnlyMemory<byte> data, SerializerRegistry registry) where T : default
    {
        var reader = new JsonReader(data);
        return Pipeline.Read<T, JsonReader>(ref reader, registry);
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
