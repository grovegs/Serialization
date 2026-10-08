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
        FormatOperations.Serialize(this, value, output, Registry, versioned: false);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        return ((IFormat)this).Deserialize<T>(data, Registry, Registry.GetVersion<T>());
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data, int version)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        return ((IFormat)this).Deserialize<T>(data, Registry, version);
    }

    void IFormat.Serialize<T>(T? value, ByteBuffer output, SerializerRegistry registry, bool versioned) where T : default
    {
        var writer = new JsonWriter(output);
        Pipeline.Write(ref writer, value, registry, versioned);
    }

    T? IFormat.Deserialize<T>(ReadOnlyMemory<byte> data, SerializerRegistry registry, int? version) where T : default
    {
        var reader = new JsonReader(data);
        return Pipeline.Read<T, JsonReader>(ref reader, registry, version);
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output, SerializerRegistry registry, int? version, bool versioned)
    {
        var reader = new JsonReader(data);
        target.ConvertFrom<T, JsonReader>(ref reader, output, registry, version, versioned);
    }

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output, SerializerRegistry registry, int? version, bool versioned)
    {
        var writer = new JsonWriter(output);
        Pipeline.Convert<T, TReader, JsonWriter>(ref reader, ref writer, registry, version, versioned);
    }
}
