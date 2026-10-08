using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class ConverterTests
{
    [Fact]
    public void Convert_JsonToMessagePackAndBack_PreservesData()
    {
        var registry = CreateRegistry();
        var json = new JsonSerializer(registry);
        var messagePack = new MessagePackSerializer(registry);
        var original = json.Serialize(new TestItem { Id = "e\"scaped", Count = 5, Weight = 0.1, Score = 2 });

        var packed = new Converter(json, messagePack).Convert<TestItem>(original);
        var result = new Converter(messagePack, json).Convert<TestItem>(packed);

        Assert.Equal(Encoding.UTF8.GetString(original), Encoding.UTF8.GetString(result));
    }

    [Fact]
    public void Convert_CsvRowsToJson_PreservesData()
    {
        var registry = CreateRegistry();
        var csv = new CsvSerializer(registry);
        var json = new JsonSerializer(registry);
        var rows = new List<TestItem> { new() { Id = "a", Count = 1, Weight = 1.5, Score = 0.5f } };

        var converted = new Converter(csv, json).Convert<List<TestItem>>(csv.Serialize(rows));
        var result = json.Deserialize<List<TestItem>>(converted);

        Assert.Equal("a", result![0].Id);
        Assert.Equal(1.5, result[0].Weight);
    }

    [Fact]
    public void Convert_DataWithoutVersion_MigratesWhileConverting()
    {
        var registry = new SerializerRegistryBuilder()
            .AddFormatter(new TestItemFormatter(), version: 2)
            .AddMigration(new TestItemRenameAmount())
            .Build();
        var json = new JsonSerializer(registry);
        var messagePack = new MessagePackSerializer(registry);
        var output = new ByteBuffer();

        new Converter(json, messagePack).Convert<TestItem>(Encoding.UTF8.GetBytes("{\"id\":\"a\",\"amount\":4}"), output);
        var result = messagePack.Deserialize<TestItem>(output.WrittenMemory);

        Assert.Equal(4, result!.Count);
    }

    [Fact]
    public void Constructor_DifferentRegistries_ThrowsArgumentException()
    {
        var json = new JsonSerializer(CreateRegistry());
        var messagePack = new MessagePackSerializer(CreateRegistry());

        Assert.Throws<ArgumentException>(() => new Converter(json, messagePack));
    }

    private sealed class TestItemRenameAmount : IMigration<TestItem>
    {
        public int FromVersion => 1;

        public void Apply(DataNode root)
        {
            root.Rename("amount", "count");
        }
    }

    private static SerializerRegistry CreateRegistry()
    {
        return new SerializerRegistryBuilder()
            .AddFormatter(new TestItemFormatter())
            .AddFormatter(new ListFormatter<TestItem>())
            .Build();
    }

    private sealed class TestItem
    {
        public string? Id;
        public int Count;
        public double Weight;
        public float Score;
    }

    private sealed class TestItemFormatter : IFormatter<TestItem>
    {
        private static readonly FieldTable s_fields = new("id", "count", "weight", "score");

        public void Write<TWriter>(ref TWriter writer, TestItem? value, SerializerRegistry registry) where TWriter : struct, IFormatWriter
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

        public TestItem? Read<TReader>(ref TReader reader, SerializerRegistry registry) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return null;
            }

            var value = new TestItem();
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

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer, SerializerRegistry registry)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader, registry), registry);
        }
    }
}
