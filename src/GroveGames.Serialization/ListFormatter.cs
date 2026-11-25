namespace GroveGames.Serialization;

public sealed class ListFormatter<T> : IFormatter<List<T>>
{
    public void Write<TWriter>(ref TWriter writer, List<T>? value)
        where TWriter : struct, IFormatWriter
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var formatter = Formatters.Get<T>();
        writer.BeginArray(value.Count);

        for (var i = 0; i < value.Count; i++)
        {
            formatter.Write(ref writer, value[i]);
        }

        writer.EndArray();
    }

    public List<T>? Read<TReader>(ref TReader reader)
        where TReader : struct, IFormatReader
    {
        if (reader.Peek() == TokenType.Null)
        {
            reader.Skip();
            return null;
        }

        var formatter = Formatters.Get<T>();
        reader.ReadArrayStart(out var count);
        var list = new List<T>(Math.Min(Math.Max(count, 0), 1024));

        while (reader.TryReadNextElement())
        {
            list.Add(formatter.Read(ref reader)!);
        }

        return list;
    }

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter
    {
        if (reader.Peek() == TokenType.Null)
        {
            reader.Skip();
            writer.WriteNull();
            return;
        }

        var formatter = Formatters.Get<T>();
        reader.ReadArrayStart(out var count);
        writer.BeginArray(count);

        while (reader.TryReadNextElement())
        {
            formatter.Transcode(ref reader, ref writer);
        }

        writer.EndArray();
    }
}
