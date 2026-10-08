namespace GroveGames.Serialization;

internal interface IFormat
{
    public void Serialize<T>(T? value, ByteBuffer output, SerializerRegistry registry);

    public T? Deserialize<T>(ReadOnlyMemory<byte> data, SerializerRegistry registry);

    public void Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output, SerializerRegistry registry);

    public void ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output, SerializerRegistry registry)
        where TReader : struct, IDocumentReader;
}
