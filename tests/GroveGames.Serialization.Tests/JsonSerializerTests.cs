using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class JsonSerializerTests
{
    [Fact]
    public void Serialize_Item_WritesEnvelopeWithCamelCaseFields()
    {
        var serializer = CreateSerializer();

        var json = Encoding.UTF8.GetString(serializer.Serialize(new TestItem { Id = "sword", Count = 2, Weight = 1.5, Score = 0.25f }));

        Assert.Equal("{\"$v\":1,\"data\":{\"id\":\"sword\",\"count\":2,\"weight\":1.5,\"score\":0.25}}", json);
    }

    [Fact]
    public void Deserialize_SerializedItem_RoundTrips()
    {
        var serializer = CreateSerializer();
        var item = new TestItem { Id = "line\n\"quoted\" \\ é 😀", Count = -7, Weight = 0.1 + 0.2, Score = 3.4028235E+38f };

        var result = serializer.Deserialize<TestItem>(serializer.Serialize(item));

        Assert.NotNull(result);
        Assert.Equal(item.Id, result.Id);
        Assert.Equal(item.Count, result.Count);
        Assert.Equal(item.Weight, result.Weight);
        Assert.Equal(item.Score, result.Score);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Serialize_NonFiniteDouble_WritesStringAndRoundTrips(double weight)
    {
        var serializer = CreateSerializer();

        var bytes = serializer.Serialize(new TestItem { Weight = weight });
        var result = serializer.Deserialize<TestItem>(bytes);

        Assert.DoesNotContain("\"weight\":N", Encoding.UTF8.GetString(bytes));
        Assert.Equal(weight, result!.Weight);
    }

    [Fact]
    public void Deserialize_DataBeforeVersion_ThrowsFormatException()
    {
        var serializer = CreateSerializer();
        var json = "{\"data\":{\"id\":\"a\",\"count\":1,\"weight\":1,\"score\":1},\"$v\":1}";

        Assert.Throws<FormatException>(() => serializer.Deserialize<TestItem>(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Deserialize_DataAfterEnvelope_ThrowsFormatException()
    {
        var serializer = CreateSerializer();
        var json = Encoding.UTF8.GetString(serializer.Serialize(new TestItem { Id = "a" })) + "{}";

        Assert.Throws<FormatException>(() => serializer.Deserialize<TestItem>(Encoding.UTF8.GetBytes(json)));
    }

    [Theory]
    [InlineData("{\"$v\":1,\"data\":nope}")]
    [InlineData("{\"$v\":1,\"data\":{\"id\":nul}}")]
    [InlineData("{\"$v\":1,\"data\":{\"id\":\"\\u12\"}}")]
    [InlineData("{\"$v\":1,\"data\":{\"count\":4294967296}}")]
    [InlineData("{\"$v\":0,\"data\":{}}")]
    public void Deserialize_MalformedJson_ThrowsFormatException(string json)
    {
        var serializer = CreateSerializer();

        Assert.Throws<FormatException>(() => serializer.Deserialize<TestItem>(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Deserialize_EveryTruncation_ThrowsFormatException()
    {
        var serializer = CreateSerializer();
        var bytes = serializer.Serialize(new TestItem { Id = "e\\scaped \"text\"", Count = 123456, Weight = -0.5, Score = 2 });

        for (var length = 0; length < bytes.Length; length++)
        {
            var truncated = bytes.AsMemory(0, length);
            Assert.Throws<FormatException>(() => serializer.Deserialize<TestItem>(truncated));
        }
    }

    [Fact]
    public void Deserialize_DeeplyNestedUnknownField_ThrowsFormatException()
    {
        var serializer = CreateSerializer();
        var json = "{\"$v\":1,\"data\":{\"unknown\":" + new string('[', 100_000) + "}}";

        Assert.Throws<FormatException>(() => serializer.Deserialize<TestItem>(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Deserialize_UnknownAndMissingFields_SkipsAndKeepsDefaults()
    {
        var serializer = CreateSerializer();
        var json = "{\"$v\":1,\"data\":{\"extra\":{\"a\":[1,2,{\"b\":null}]},\"count\":4}}";

        var result = serializer.Deserialize<TestItem>(Encoding.UTF8.GetBytes(json));

        Assert.Null(result!.Id);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Deserialize_NewerVersion_ThrowsNotSupportedException()
    {
        var serializer = CreateSerializer();
        var json = "{\"$v\":2,\"data\":{}}";

        Assert.Throws<NotSupportedException>(() => serializer.Deserialize<TestItem>(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Deserialize_OlderVersion_RunsMigrationsInOrder()
    {
        var registry = new SerializerRegistryBuilder()
            .AddFormatter(new TestSaveFormatter(), version: 3)
            .AddMigration(new TestSaveRenameCoins())
            .AddMigration(new TestSaveXpToLevel())
            .Build();
        var serializer = new JsonSerializer(registry);
        var json = "{\"$v\":1,\"data\":{\"name\":\"hero\",\"xp\":4500,\"coins\":30}}";

        var result = serializer.Deserialize<TestSave>(Encoding.UTF8.GetBytes(json));

        Assert.Equal("hero", result!.Name);
        Assert.Equal(5, result.Level);
        Assert.Equal(30, result.Gold);
    }

    [Fact]
    public void Deserialize_NullRoot_ReturnsNull()
    {
        var serializer = CreateSerializer();

        var result = serializer.Deserialize<TestItem>(serializer.Serialize<TestItem>(null));

        Assert.Null(result);
    }

    [Fact]
    public void Serialize_Stream_RoundTrips()
    {
        var serializer = CreateSerializer();
        using var stream = new MemoryStream();

        serializer.Serialize(new TestItem { Id = "stream", Count = 9 }, stream);
        stream.Position = 0;
        var result = serializer.Deserialize<TestItem>(stream);

        Assert.Equal("stream", result!.Id);
        Assert.Equal(9, result.Count);
    }

    [Fact]
    public void Serialize_UnregisteredType_ThrowsInvalidOperationException()
    {
        var serializer = new JsonSerializer(new SerializerRegistryBuilder().Build());

        Assert.Throws<InvalidOperationException>(() => serializer.Serialize(new TestItem()));
    }

    private static JsonSerializer CreateSerializer()
    {
        var registry = new SerializerRegistryBuilder()
            .AddFormatter(new TestItemFormatter())
            .Build();
        return new JsonSerializer(registry);
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

    private sealed class TestSave
    {
        public string? Name;
        public int Level;
        public long Gold;
    }

    private sealed class TestSaveFormatter : IFormatter<TestSave>
    {
        private static readonly FieldTable s_fields = new("name", "level", "gold");

        public void Write<TWriter>(ref TWriter writer, TestSave? value, SerializerRegistry registry) where TWriter : struct, IFormatWriter
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.BeginObject(3);
            writer.WriteField(s_fields[0]);
            writer.WriteString(value.Name);
            writer.WriteField(s_fields[1]);
            writer.WriteInt32(value.Level);
            writer.WriteField(s_fields[2]);
            writer.WriteInt64(value.Gold);
            writer.EndObject();
        }

        public TestSave? Read<TReader>(ref TReader reader, SerializerRegistry registry) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return null;
            }

            var value = new TestSave();
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

    private sealed class TestSaveRenameCoins : IMigration<TestSave>
    {
        public int FromVersion => 1;

        public void Apply(DataNode root)
        {
            root.Rename("coins", "gold");
        }
    }

    private sealed class TestSaveXpToLevel : IMigration<TestSave>
    {
        public int FromVersion => 2;

        public void Apply(DataNode root)
        {
            var xp = root.Has("xp") ? root["xp"]!.AsInt64 : 0;
            root["level"] = DataNode.FromInt((xp / 1000) + 1);
            root.Remove("xp");
        }
    }
}
