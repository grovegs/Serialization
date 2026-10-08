namespace GroveGames.Serialization;

public interface IFormatter<T>
{
    public void Write<TWriter>(ref TWriter writer, T? value)
        where TWriter : struct, IFormatWriter;

    public T? Read<TReader>(ref TReader reader)
        where TReader : struct, IFormatReader;

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter;
}
