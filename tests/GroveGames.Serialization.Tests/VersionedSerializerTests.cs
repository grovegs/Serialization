using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class VersionedSerializerTests
{
    [Fact]
    public void Serialize_Json_WritesVersionEnvelope()
    {
        var serializer = new VersionedSerializer(new JsonSerializer(CreateRegistry()));

        var json = Encoding.UTF8.GetString(serializer.Serialize(new TestSave { Name = "hero", Level = 2, Gold = 3 }));

        Assert.Equal("{\"$v\":3,\"data\":{\"name\":\"hero\",\"level\":2,\"gold\":3}}", json);
    }

    [Fact]
    public void Deserialize_OlderEnvelope_RunsMigrationsInOrder()
    {
        var serializer = new VersionedSerializer(new JsonSerializer(CreateRegistry()));
        var json = "{\"$v\":1,\"data\":{\"name\":\"hero\",\"xp\":4500,\"coins\":30}}";

        var result = serializer.Deserialize<TestSave>(Encoding.UTF8.GetBytes(json));

        Assert.Equal(5, result!.Level);
        Assert.Equal(30, result.Gold);
    }

    [Fact]
    public void Deserialize_MessagePackRoundTrip_KeepsValues()
    {
        var serializer = new VersionedSerializer(new MessagePackSerializer(CreateRegistry()));

        var result = serializer.Deserialize<TestSave>(serializer.Serialize(new TestSave { Name = "hero", Level = 9, Gold = long.MaxValue }));

        Assert.Equal(9, result!.Level);
        Assert.Equal(long.MaxValue, result.Gold);
    }

    [Theory]
    [InlineData("{\"data\":{\"name\":\"a\"},\"$v\":3}")]
    [InlineData("{\"$v\":0,\"data\":{}}")]
    [InlineData("{\"$v\":3,\"data\":{}}{}")]
    [InlineData("{\"name\":\"a\"}")]
    public void Deserialize_MalformedEnvelope_ThrowsFormatException(string json)
    {
        var serializer = new VersionedSerializer(new JsonSerializer(CreateRegistry()));

        Assert.Throws<FormatException>(() => serializer.Deserialize<TestSave>(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Deserialize_NewerEnvelope_ThrowsNotSupportedException()
    {
        var serializer = new VersionedSerializer(new JsonSerializer(CreateRegistry()));

        Assert.Throws<NotSupportedException>(() => serializer.Deserialize<TestSave>(Encoding.UTF8.GetBytes("{\"$v\":4,\"data\":{}}")));
    }

    [Fact]
    public void Deserialize_EveryTruncation_ThrowsFormatException()
    {
        var serializer = new VersionedSerializer(new MessagePackSerializer(CreateRegistry()));
        var bytes = serializer.Serialize(new TestSave { Name = "hero", Level = 300, Gold = 70_000 });

        for (var length = 0; length < bytes.Length; length++)
        {
            var truncated = bytes.AsMemory(0, length);
            Assert.Throws<FormatException>(() => serializer.Deserialize<TestSave>(truncated));
        }
    }

    [Fact]
    public void Constructor_CustomSerializer_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new VersionedSerializer(new TestSerializer()));
    }

    private static SerializerRegistry CreateRegistry()
    {
        return new SerializerRegistryBuilder()
            .AddFormatter(new TestSaveFormatter(), version: 3)
            .AddMigration(new TestSaveRenameCoins())
            .AddMigration(new TestSaveXpToLevel())
            .Build();
    }

    private sealed class TestSerializer : ISerializer
    {
        public SerializerRegistry Registry => new SerializerRegistryBuilder().Build();

        public void Serialize<T>(T? value, System.Buffers.IBufferWriter<byte> output)
        {
        }

        public T? Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            return default;
        }

        public T? Deserialize<T>(ReadOnlyMemory<byte> data, int version)
        {
            return default;
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
