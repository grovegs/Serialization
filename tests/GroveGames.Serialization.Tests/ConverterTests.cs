using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class ConverterTests
{
    static ConverterTests()
    {
        Formatters.Register(registrar => registrar
            .AddFormatter(new TestItemFormatter<TestItem>())
            .AddFormatter(new ListFormatter<TestItem>())
            .AddFormatter(new TestItemFormatter<TestItemV2>(), version: 2)
            .AddMigration(new TestItemRenameAmount()));
    }

    [Fact]
    public void Convert_JsonToMessagePackAndBack_PreservesData()
    {
        var json = new JsonSerializer();
        var messagePack = new MessagePackSerializer();
        var original = json.Serialize(new TestItem { Id = "e\"scaped", Count = 5, Weight = 0.1, Score = 2 });

        var packed = new Converter(json, messagePack).Convert<TestItem>(original);
        var result = new Converter(messagePack, json).Convert<TestItem>(packed);

        Assert.Equal(Encoding.UTF8.GetString(original), Encoding.UTF8.GetString(result));
    }

    [Fact]
    public void Convert_CsvRowsToJson_PreservesData()
    {
        var csv = new CsvSerializer();
        var json = new JsonSerializer();
        var rows = new List<TestItem> { new() { Id = "a", Count = 1, Weight = 1.5, Score = 0.5f } };

        var converted = new Converter(csv, json).Convert<List<TestItem>>(csv.Serialize(rows));
        var result = json.Deserialize<List<TestItem>>(converted);

        Assert.Equal("a", result![0].Id);
        Assert.Equal(1.5, result[0].Weight);
    }

    [Fact]
    public void Convert_DataWithoutVersion_MigratesWhileConverting()
    {
        var json = new JsonSerializer();
        var messagePack = new MessagePackSerializer();
        var output = new ByteBuffer();

        new Converter(json, messagePack).Convert<TestItemV2>(Encoding.UTF8.GetBytes("{\"id\":\"a\",\"amount\":4}"), output);
        var result = messagePack.Deserialize<TestItemV2>(output.WrittenMemory);

        Assert.Equal(4, result!.Count);
    }

    private sealed class TestItemRenameAmount : IMigration<TestItemV2>
    {
        public int FromVersion => 1;

        public void Apply(DataValue root)
        {
            root.AsObject.Rename("amount", "count");
        }
    }

    private class TestItem
    {
        public string? Id;
        public int Count;
        public double Weight;
        public float Score;
    }

    private sealed class TestItemFormatter<T> : IFormatter<T>
        where T : TestItem, new()
    {
        private static readonly FieldTable s_fields = new("id", "count", "weight", "score");

        public void Write<TWriter>(ref TWriter writer, T? value) where TWriter : struct, IFormatWriter
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.BeginObject(4);
            writer.WriteField(s_fields[0]);
            writer.WriteString(value.Id);
            writer.WriteField(s_fields[1]);
            writer.WriteInt32(value.Count);
            writer.WriteField(s_fields[2]);
            writer.WriteDouble(value.Weight);
            writer.WriteField(s_fields[3]);
            writer.WriteSingle(value.Score);
            writer.EndObject();
        }

        public T? Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return null;
            }

            var value = new T();
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
                        value.Weight = reader.ReadDouble();
                        break;
                    case 3:
                        value.Score = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return value;
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }

    private sealed class TestItemV2 : TestItem
    {
    }
}
