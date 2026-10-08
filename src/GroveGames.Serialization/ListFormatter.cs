namespace GroveGames.Serialization;

public sealed class ListFormatter<T> : IFormatter<List<T>>
{
    public void Write<TWriter>(ref TWriter writer, List<T>? value, FormatterRegistry registry)
        where TWriter : struct, IFormatWriter
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var formatter = registry.GetFormatter<T>();
        writer.BeginArray(value.Count);

        for (var i = 0; i < value.Count; i++)
        {
            formatter.Write(ref writer, value[i], registry);
        }

        writer.EndArray();
    }

    public List<T>? Read<TReader>(ref TReader reader, FormatterRegistry registry)
        where TReader : struct, IFormatReader
    {
        if (reader.Peek() == TokenType.Null)
        {
            reader.Skip();
            return null;
        }

        var formatter = registry.GetFormatter<T>();
        var list = new List<T>();
        reader.ReadArrayStart();

        while (reader.TryReadNextElement())
        {
            list.Add(formatter.Read(ref reader, registry)!);
        }

        return list;
    }

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer, FormatterRegistry registry)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter
    {
        if (reader.Peek() == TokenType.Null)
        {
            reader.Skip();
            writer.WriteNull();
            return;
        }

        var formatter = registry.GetFormatter<T>();
        reader.ReadArrayStart();
        writer.BeginArray(-1);

        while (reader.TryReadNextElement())
        {
            formatter.Transcode(ref reader, ref writer, registry);
        }

        writer.EndArray();
    }
}
