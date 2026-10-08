using GroveGames.Serialization;

namespace ConsoleApplication.Models;

public sealed class ItemFormatter : IFormatter<Item>
{
    private static readonly FieldTable s_fields = new("id", "count", "weight");

    public void Write<TWriter>(ref TWriter writer, Item? value, FormatterRegistry registry) where TWriter : struct, IFormatWriter
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.BeginObject(3);
        writer.WriteField(s_fields[0]);
        writer.WriteString(value.Id);
        writer.WriteField(s_fields[1]);
        writer.WriteInt32(value.Count);
        writer.WriteField(s_fields[2]);
        writer.WriteSingle(value.Weight);
        writer.EndObject();
    }

    public Item? Read<TReader>(ref TReader reader, FormatterRegistry registry) where TReader : struct, IFormatReader
    {
        if (reader.Peek() == TokenType.Null)
        {
            reader.Skip();
            return null;
        }

        var value = new Item();
        reader.ReadObjectStart();

        while (reader.TryReadField(s_fields, out var index))
        {
            switch (index)
            {
                case 0:
                    value.Id = reader.ReadString();
                    break;
                case 1:
                    value.Count = reader.ReadInt32();
                    break;
                case 2:
                    value.Weight = reader.ReadSingle();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return value;
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

        reader.ReadObjectStart();
        writer.BeginObject(-1);

        while (reader.TryReadField(s_fields, out var index))
        {
            switch (index)
            {
                case 0:
                    writer.WriteField(s_fields[0]);

                    if (reader.ReadStringUtf8(out var id))
                    {
                        writer.WriteStringUtf8(id);
                    }
                    else
                    {
                        writer.WriteNull();
                    }

                    break;
                case 1:
                    writer.WriteField(s_fields[1]);
                    writer.WriteInt32(reader.ReadInt32());
                    break;
                case 2:
                    writer.WriteField(s_fields[2]);
                    writer.WriteSingle(reader.ReadSingle());
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        writer.EndObject();
    }
}
