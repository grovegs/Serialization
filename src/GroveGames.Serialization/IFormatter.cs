namespace GroveGames.Serialization;

public interface IFormatter<T>
{
    public void Write<TWriter>(ref TWriter writer, T? value, FormatterRegistry registry)
        where TWriter : struct, IFormatWriter;

    public T? Read<TReader>(ref TReader reader, FormatterRegistry registry)
        where TReader : struct, IFormatReader;

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer, FormatterRegistry registry)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter;
}
