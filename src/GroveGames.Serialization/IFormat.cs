namespace GroveGames.Serialization;

internal interface IFormat
{
    public void Serialize<T>(T? value, ByteBuffer output, SerializerRegistry registry, bool versioned);

    public T? Deserialize<T>(ReadOnlyMemory<byte> data, SerializerRegistry registry, int? version);

    public void Convert<T>(ReadOnlyMemory<byte> data, IFormat target, ByteBuffer output, SerializerRegistry registry, int? version, bool versioned);

    public void ConvertFrom<T, TReader>(ref TReader reader, ByteBuffer output, SerializerRegistry registry, int? version, bool versioned)
        where TReader : struct, IDocumentReader;
}
