using GroveGames.Serialization.Csv;

namespace GroveGames.Serialization;

public sealed class CsvSerializer : ISerializer, IFormat
{
    public CsvSerializer(SerializerRegistry registry)
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
        var writer = new CsvWriter(output);
        Pipeline.Write(ref writer, value, Registry);
    }

    public void Serialize<T>(T? value, Stream output)
    {
        SerializerStreams.Serialize(this, value, output);
    }

    public T? Deserialize<T>(ReadOnlyMemory<byte> data)
    {
        var reader = new CsvReader(data);
        return Pipeline.Read<T, CsvReader>(ref reader, Registry);
    }

    public T? Deserialize<T>(Stream input)
    {
        return SerializerStreams.Deserialize<T>(this, input);
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output, SerializerRegistry registry)
    {
        var reader = new CsvReader(data);
        target.ConvertFrom<T, CsvReader>(ref reader, output, registry);
    }

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output, SerializerRegistry registry)
    {
        var writer = new CsvWriter(output);
        Pipeline.Convert<T, TReader, CsvWriter>(ref reader, ref writer, registry);
    }
}
