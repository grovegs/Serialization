namespace GroveGames.Serialization;

internal interface IFormat
{
    public void Serialize<T>(T? value, ByteBuffer output);

    public T? Deserialize<T>(ReadOnlyMemory<byte> data);

    public void Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output);

    public void ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output)
        where TReader : struct, IDocumentReader;
}
