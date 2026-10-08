namespace GroveGames.Serialization.Tests;

public sealed class SerializerRegistryBuilderTests
{
    [Fact]
    public void AddFormatter_SameTypeTwice_ThrowsInvalidOperationException()
    {
        var builder = new SerializerRegistryBuilder().AddFormatter(new TestSaveFormatter());

        Assert.Throws<InvalidOperationException>(() => builder.AddFormatter(new TestSaveFormatter()));
    }

    [Fact]
    public void AddFormatter_ZeroVersion_ThrowsArgumentOutOfRangeException()
    {
        var builder = new SerializerRegistryBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddFormatter(new TestSaveFormatter(), version: 0));
    }

    [Fact]
    public void Build_MigrationWithoutFormatter_ThrowsInvalidOperationException()
    {
        var builder = new SerializerRegistryBuilder().AddMigration(new TestSaveRenameCoins());

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_MigrationFromCurrentVersion_ThrowsInvalidOperationException()
    {
        var builder = new SerializerRegistryBuilder()
            .AddFormatter(new TestSaveFormatter(), version: 1)
            .AddMigration(new TestSaveRenameCoins());

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_TwoMigrationsFromSameVersion_ThrowsInvalidOperationException()
    {
        var builder = new SerializerRegistryBuilder()
            .AddFormatter(new TestSaveFormatter(), version: 2)
            .AddMigration(new TestSaveRenameCoins())
            .AddMigration(new TestSaveRenameCoins());

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_RegisteredFormatter_IsReturnedWithVersion()
    {
        var formatter = new TestSaveFormatter();

        var registry = new SerializerRegistryBuilder().AddFormatter(formatter, version: 3).Build();

        Assert.Same(formatter, registry.GetFormatter<TestSave>());
        Assert.Equal(3, registry.GetVersion<TestSave>());
        Assert.False(registry.TryGetFormatter<string>(out _));
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
