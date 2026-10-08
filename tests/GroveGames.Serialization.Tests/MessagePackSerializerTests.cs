namespace GroveGames.Serialization.Tests;

public sealed class MessagePackSerializerTests
{
    [Fact]
    public void Deserialize_SerializedItem_RoundTrips()
    {
        var serializer = CreateSerializer();
        var item = new TestItem { Id = new string('x', 70_000), Count = int.MinValue, Weight = double.Epsilon, Score = float.NaN };

        var result = serializer.Deserialize<TestItem>(serializer.Serialize(item));

        Assert.Equal(item.Id, result!.Id);
        Assert.Equal(item.Count, result.Count);
        Assert.Equal(item.Weight, result.Weight);
        Assert.True(float.IsNaN(result.Score));
    }

    [Fact]
    public void Deserialize_DeeplyNestedUnknownField_ThrowsFormatException()
    {
        var serializer = CreateSerializer();
        var bytes = new List<byte> { 0x82, 0xa2, (byte)'$', (byte)'v', 0x01, 0xa4, (byte)'d', (byte)'a', (byte)'t', (byte)'a', 0x81, 0xa1, (byte)'z' };
        bytes.AddRange(Enumerable.Repeat((byte)0x91, 1_000_000));
        bytes.Add(0xc0);

        Assert.Throws<FormatException>(() => serializer.Deserialize<TestItem>(bytes.ToArray()));
    }

    [Fact]
    public void Deserialize_EveryTruncation_ThrowsFormatException()
    {
        var serializer = CreateSerializer();
        var bytes = serializer.Serialize(new TestItem { Id = "potion", Count = 70_000, Weight = 2.5, Score = 1 });

        for (var length = 0; length < bytes.Length; length++)
        {
            var truncated = bytes.AsMemory(0, length);
            Assert.Throws<FormatException>(() => serializer.Deserialize<TestItem>(truncated));
        }
    }

    [Fact]
    public void Deserialize_UInt64AboveInt64_ThrowsFormatException()
    {
        var serializer = CreateSerializer();
        byte[] bytes = [0x82, 0xa2, (byte)'$', (byte)'v', 0x01, 0xa4, (byte)'d', (byte)'a', (byte)'t', (byte)'a', 0x81, 0xa5, (byte)'c', (byte)'o', (byte)'u', (byte)'n', (byte)'t', 0xcf, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff];

        Assert.Throws<FormatException>(() => serializer.Deserialize<TestItem>(bytes));
    }

    [Fact]
    public void Deserialize_DataAfterEnvelope_ThrowsFormatException()
    {
        var serializer = CreateSerializer();
        byte[] bytes = [.. serializer.Serialize(new TestItem()), 0xc0];

        Assert.Throws<FormatException>(() => serializer.Deserialize<TestItem>(bytes));
    }

    [Fact]
    public void Serialize_ParallelCalls_RoundTripIndependently()
    {
        var serializer = CreateSerializer();

        Parallel.For(0, 2000, i =>
        {
            var result = serializer.Deserialize<TestItem>(serializer.Serialize(new TestItem { Id = i.ToString(), Count = i }));
            Assert.Equal(i.ToString(), result!.Id);
            Assert.Equal(i, result.Count);
        });
    }

    [Fact]
    public void Deserialize_ListRoot_RoundTrips()
    {
        var registry = new SerializerRegistryBuilder()
            .AddFormatter(new TestItemFormatter())
            .AddFormatter(new ListFormatter<TestItem>())
            .Build();
        var serializer = new MessagePackSerializer(registry);
        var items = Enumerable.Range(0, 20).Select(i => new TestItem { Id = "item" + i, Count = i }).ToList();

        var result = serializer.Deserialize<List<TestItem>>(serializer.Serialize(items));

        Assert.Equal(items.Select(i => i.Id), result!.Select(i => i.Id));
    }

    private static MessagePackSerializer CreateSerializer()
    {
        var registry = new SerializerRegistryBuilder()
            .AddFormatter(new TestItemFormatter())
            .Build();
        return new MessagePackSerializer(registry);
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
