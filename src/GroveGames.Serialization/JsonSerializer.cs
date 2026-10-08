using System.Buffers;
using GroveGames.Serialization.Json;

namespace GroveGames.Serialization;

public sealed class JsonSerializer : ISerializer, IFormat
{
    public void Serialize<T>(T? value, IBufferWriter<byte> output)
    {
        FormatOperations.Serialize(this, value, output);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        return ((IFormat)this).Deserialize<T>(data);
    }

    void IFormat.Serialize<T>(T? value, ByteBuffer output) where T : default
    {
        var writer = new JsonWriter(output);
        Pipeline.Write(ref writer, value);
    }

    T? IFormat.Deserialize<T>(ReadOnlyMemory<byte> data) where T : default
    {
        var reader = new JsonReader(data);
        return Pipeline.Read<T, JsonReader>(ref reader);
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output)
    {
        var reader = new JsonReader(data);
        target.ConvertFrom<T, JsonReader>(ref reader, output);
    }

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output)
    {
        var writer = new JsonWriter(output);
        Pipeline.Convert<T, TReader, JsonWriter>(ref reader, ref writer);
    }
}
