using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class VersionedConverterTests
{
    [Fact]
    public void Convert_OlderJsonEnvelope_WritesCurrentMessagePackEnvelope()
    {
        var registry = new SerializerRegistryBuilder()
            .AddFormatter(new TestRecordFormatter(), version: 2)
            .AddMigration(new TestRecordRenameAmount())
            .Build();
        var json = new VersionedSerializer(new JsonSerializer(registry));
        var messagePack = new VersionedSerializer(new MessagePackSerializer(registry));

        var converted = new VersionedConverter(json, messagePack).Convert<TestRecord>(Encoding.UTF8.GetBytes("{\"$v\":1,\"data\":{\"amount\":7}}"));
        var result = messagePack.Deserialize<TestRecord>(converted);

        Assert.Equal(7, result!.Count);
    }

    private sealed class TestRecord
    {
        public int Count;
    }

    private sealed class TestRecordFormatter : IFormatter<TestRecord>
    {
        private static readonly FieldTable s_fields = new("count");

        public void Write<TWriter>(ref TWriter writer, TestRecord? value, SerializerRegistry registry) where TWriter : struct, IFormatWriter
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.BeginObject(1);
            writer.WriteField(s_fields[0]);
            writer.WriteInt32(value.Count);
            writer.EndObject();
        }

        public TestRecord? Read<TReader>(ref TReader reader, SerializerRegistry registry) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return null;
            }

            var value = new TestRecord();
            reader.ReadObjectStart();

            while (reader.TryReadField(s_fields, out var index))
            {
                if (index == 0)
                {
                    value.Count = reader.ReadInt32();
                }
                else
                {
                    reader.Skip();
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

    private sealed class TestRecordRenameAmount : IMigration<TestRecord>
    {
        public int FromVersion => 1;

        public void Apply(DataNode root)
        {
            root.Rename("amount", "count");
        }
    }
}
