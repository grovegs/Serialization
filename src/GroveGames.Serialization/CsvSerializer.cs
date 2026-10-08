using System.Buffers;
using GroveGames.Serialization.Csv;

namespace GroveGames.Serialization;

public sealed class CsvSerializer : ISerializer, IFormat
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
        var writer = new CsvWriter(output);
        Pipeline.Write(ref writer, value);
    }

    T? IFormat.Deserialize<T>(ReadOnlyMemory<byte> data) where T : default
    {
        var reader = new CsvReader(data);
        return Pipeline.Read<T, CsvReader>(ref reader);
    }

    void IFormat.Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output)
    {
        var reader = new CsvReader(data);
        target.ConvertFrom<T, CsvReader>(ref reader, output);
    }

    void IFormat.ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output)
    {
        var writer = new CsvWriter(output);
        Pipeline.Convert<T, TReader, CsvWriter>(ref reader, ref writer);
    }
}
