using GroveGames.Serialization;

namespace ConsoleApplication.Models;

public sealed class PlayerSaveFormatter : IFormatter<PlayerSave>
{
    private static readonly FieldTable s_fields = new("name", "level", "gold", "playTime", "items");

    private readonly ItemFormatter _itemFormatter;

    public PlayerSaveFormatter()
    {
        _itemFormatter = new ItemFormatter();
    }

    public void Write<TWriter>(ref TWriter writer, PlayerSave? value, FormatterRegistry registry) where TWriter : struct, IFormatWriter
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.BeginObject(5);
        writer.WriteField(s_fields[0]);
        writer.WriteString(value.Name);
        writer.WriteField(s_fields[1]);
        writer.WriteInt32(value.Level);
        writer.WriteField(s_fields[2]);
        writer.WriteInt64(value.Gold);
        writer.WriteField(s_fields[3]);
        writer.WriteDouble(value.PlayTime);
        writer.WriteField(s_fields[4]);

        if (value.Items == null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.BeginArray(value.Items.Count);

            for (var i = 0; i < value.Items.Count; i++)
            {
                _itemFormatter.Write(ref writer, value.Items[i], registry);
            }

            writer.EndArray();
        }

        writer.EndObject();
    }

    public PlayerSave? Read<TReader>(ref TReader reader, FormatterRegistry registry) where TReader : struct, IFormatReader
    {
        if (reader.Peek() == TokenType.Null)
        {
            reader.Skip();
            return null;
        }

        var value = new PlayerSave();
        reader.ReadObjectStart();

        while (reader.TryReadField(s_fields, out var index))
        {
            switch (index)
            {
                case 0:
                    value.Name = reader.ReadString();
                    break;
                case 1:
                    value.Level = reader.ReadInt32();
                    break;
                case 2:
                    value.Gold = reader.ReadInt64();
                    break;
                case 3:
                    value.PlayTime = reader.ReadDouble();
                    break;
                case 4:
                    value.Items = ReadItems(ref reader, registry);
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

                    if (reader.ReadStringUtf8(out var name))
                    {
                        writer.WriteStringUtf8(name);
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
                    writer.WriteInt64(reader.ReadInt64());
                    break;
                case 3:
                    writer.WriteField(s_fields[3]);
                    writer.WriteDouble(reader.ReadDouble());
                    break;
                case 4:
                    writer.WriteField(s_fields[4]);

                    if (reader.Peek() == TokenType.Null)
                    {
                        reader.Skip();
                        writer.WriteNull();
                        break;
                    }

                    reader.ReadArrayStart();
                    writer.BeginArray(-1);

                    while (reader.TryReadNextElement())
                    {
                        _itemFormatter.Transcode(ref reader, ref writer, registry);
                    }

                    writer.EndArray();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        writer.EndObject();
    }

    private List<Item>? ReadItems<TReader>(ref TReader reader, FormatterRegistry registry) where TReader : struct, IFormatReader
    {
        if (reader.Peek() == TokenType.Null)
        {
            reader.Skip();
            return null;
        }

        var items = new List<Item>();
        reader.ReadArrayStart();

        while (reader.TryReadNextElement())
        {
            items.Add(_itemFormatter.Read(ref reader, registry)!);
        }

        return items;
    }
}
