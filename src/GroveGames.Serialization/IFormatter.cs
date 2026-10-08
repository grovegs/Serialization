namespace GroveGames.Serialization;

public interface IFormatter<T>
{
    public void Write<TWriter>(ref TWriter writer, T? value, SerializerRegistry registry)
        where TWriter : struct, IFormatWriter;

    public T? Read<TReader>(ref TReader reader, SerializerRegistry registry)
        where TReader : struct, IFormatReader;

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer, SerializerRegistry registry)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter;
}
