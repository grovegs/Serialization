namespace GroveGames.Serialization;

internal interface IFormat
{
    public void Serialize<T>(T? value, ByteBuffer output, FormatterRegistry registry);

    public T? Deserialize<T>(ReadOnlyMemory<byte> data, FormatterRegistry registry);

    public void Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output, FormatterRegistry registry);

    public void ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output, FormatterRegistry registry)
        where TReader : struct, IDocumentReader;
}
