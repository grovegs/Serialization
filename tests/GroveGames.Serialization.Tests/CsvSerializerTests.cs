using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class CsvSerializerTests
{
    [Fact]
    public void Serialize_Rows_WritesHeaderAndRows()
    {
        var serializer = CreateSerializer(version: 1);
        var rows = new List<TestItem> { new() { Id = "a,b", Count = 1, Weight = 0.5, Score = 2 }, new() { Id = "say \"hi\"", Count = 2 } };

        var csv = Encoding.UTF8.GetString(serializer.Serialize(rows));

        Assert.Equal("id,count,weight,score\n\"a,b\",1,0.5,2\n\"say \"\"hi\"\"\",2,0,0\n", csv);
    }

    [Fact]
    public void Deserialize_SerializedRows_RoundTrips()
    {
        var serializer = CreateSerializer(version: 1);
        var rows = new List<TestItem> { new() { Id = "line\nbreak", Count = -3, Weight = 1e-7, Score = 4.5f }, new() { Id = "", Count = 0 } };

        var result = serializer.Deserialize<List<TestItem>>(serializer.Serialize(rows));

        Assert.Equal(2, result!.Count);
        Assert.Equal("line\nbreak", result[0].Id);
        Assert.Equal(-3, result[0].Count);
        Assert.Equal(1e-7, result[0].Weight);
        Assert.Equal(4.5f, result[0].Score);
        Assert.Equal("", result[1].Id);
    }

    [Fact]
    public void Deserialize_OlderVersionWithNumberLikeText_KeepsText()
    {
        var registry = new SerializerRegistryBuilder()
            .AddFormatter(new TestItemFormatter())
            .AddFormatter(new ListFormatter<TestItem>(), version: 2)
            .AddMigration(new TestRowsNoChange())
            .Build();
        var serializer = new CsvSerializer(registry);
        var csv = "id,count,weight,score\n007,1,2.5,0.5\n";

        var result = serializer.Deserialize<List<TestItem>>(Encoding.UTF8.GetBytes(csv));

        Assert.Equal("007", result![0].Id);
        Assert.Equal(1, result[0].Count);
        Assert.Equal(2.5, result[0].Weight);
    }

    [Theory]
    [InlineData("id,count\na,notanumber\n")]
    [InlineData("id,count\na,99999999999\n")]
    [InlineData("#v=x\nid,count\na,1\n")]
    [InlineData("id,count\n\"open,1\n")]
    public void Deserialize_MalformedCsv_ThrowsFormatException(string csv)
    {
        var serializer = CreateSerializer(version: 1);

        Assert.Throws<FormatException>(() => serializer.Deserialize<List<TestItem>>(Encoding.UTF8.GetBytes(csv)));
    }

    [Fact]
    public void Serialize_VersionedRows_WritesVersionLine()
    {
        var serializer = CreateSerializer(version: 2);

        var csv = Encoding.UTF8.GetString(serializer.Serialize(new List<TestItem> { new() { Id = "a", Count = 1 } }));

        Assert.StartsWith("#v=2\nid,count,weight,score\n", csv);
    }

    [Fact]
    public void Serialize_SingleObject_ThrowsNotSupportedException()
    {
        var serializer = CreateSerializer(version: 1);

        Assert.Throws<NotSupportedException>(() => serializer.Serialize(new TestItem()));
    }

    private static CsvSerializer CreateSerializer(int version)
    {
        var registry = new SerializerRegistryBuilder()
            .AddFormatter(new TestItemFormatter())
            .AddFormatter(new ListFormatter<TestItem>(), version)
            .Build();
        return new CsvSerializer(registry);
    }

    private sealed class TestRowsNoChange : IMigration<List<TestItem>>
    {
        public int FromVersion => 1;

        public void Apply(DataNode root)
        {
        }
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
