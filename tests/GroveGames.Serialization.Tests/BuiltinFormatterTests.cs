using System.Text;

namespace GroveGames.Serialization.Tests;

public sealed class BuiltinFormatterTests
{
    private static readonly Guid s_id = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");
    private static readonly DateTime s_created = new(2026, 10, 9, 10, 30, 15, 250, DateTimeKind.Utc);

    [Fact]
    public void Serialize_Json_WritesStandardStrings()
    {
        var json = Encoding.UTF8.GetString(new JsonSerializer().Serialize(CreateSample()));

        Assert.Contains("\"id\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\"", json);
        Assert.Contains("\"created\":\"2026-10-09T10:30:15.2500000Z\"", json);
        Assert.Contains("\"at\":\"2026-10-09T10:30:15.2500000+03:00\"", json);
        Assert.Contains("\"duration\":\"01:30:05\"", json);
    }

    [Fact]
    public void Deserialize_JsonAndMessagePack_RoundTrips()
    {
        foreach (var serializer in new ISerializer[] { new JsonSerializer(), new MessagePackSerializer() })
        {
            var result = serializer.Deserialize<BuiltinSample>(serializer.Serialize(CreateSample()))!;

            Assert.Equal(s_id, result.Id);
            Assert.Equal(s_created, result.Created);
            Assert.Equal(DateTimeKind.Utc, result.Created.Kind);
            Assert.Equal(DateTimeKind.Unspecified, result.Unspecified.Kind);
            Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5), result.Unspecified);
            Assert.Equal(new DateTimeOffset(s_created.Ticks, TimeSpan.FromHours(3)), result.At);
            Assert.Equal(new TimeSpan(1, 30, 5), result.Duration);
            Assert.Equal(TimeSpan.FromDays(-2.5), result.Negative);
            Assert.Null(result.MaybeId);
            Assert.Equal(s_id, result.OtherId);
            Assert.Equal([s_created, DateTime.MinValue.ToUniversalTime()], result.Dates);
        }
    }

    [Fact]
    public void Deserialize_HandWrittenValues_ParsesWithInvariantCulture()
    {
        var json = "{\"id\":\"{6f9619ff-8b86-d011-b42d-00c04fc964ff}\",\"created\":\"2026-10-09\",\"duration\":\"2.01:00:00\"}";

        var result = new JsonSerializer().Deserialize<BuiltinSample>(Encoding.UTF8.GetBytes(json))!;

        Assert.Equal(s_id, result.Id);
        Assert.Equal(new DateTime(2026, 10, 9), result.Created);
        Assert.Equal(new TimeSpan(2, 1, 0, 0), result.Duration);
    }

    [Theory]
    [InlineData("{\"id\":\"not-a-guid\"}")]
    [InlineData("{\"created\":\"yesterday\"}")]
    [InlineData("{\"duration\":\"long\"}")]
    [InlineData("{\"id\":null}")]
    public void Deserialize_InvalidValue_ThrowsFormatException(string json)
    {
        Assert.Throws<FormatException>(() => new JsonSerializer().Deserialize<BuiltinSample>(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void Deserialize_CsvRows_RoundTrips()
    {
        var serializer = new CsvSerializer();
        var rows = new List<BuiltinRow> { new() { Id = s_id, Expires = s_created, Cooldown = TimeSpan.FromSeconds(90) } };

        var csv = serializer.Serialize(rows);
        var result = serializer.Deserialize<List<BuiltinRow>>(csv)!;

        Assert.Equal("id,expires,cooldown\n6f9619ff-8b86-d011-b42d-00c04fc964ff,2026-10-09T10:30:15.2500000Z,00:01:30\n", Encoding.UTF8.GetString(csv));
        Assert.Equal(s_id, result[0].Id);
        Assert.Equal(s_created, result[0].Expires);
        Assert.Equal(TimeSpan.FromSeconds(90), result[0].Cooldown);
    }

    [Fact]
    public void Serialize_Roots_RoundTrip()
    {
        var serializer = new MessagePackSerializer();

        Assert.Equal(s_id, serializer.Deserialize<Guid>(serializer.Serialize(s_id)));
        Assert.Equal(s_created, serializer.Deserialize<DateTime>(serializer.Serialize(s_created)));
        Assert.Equal([TimeSpan.FromSeconds(1)], serializer.Deserialize<List<TimeSpan>>(serializer.Serialize(new List<TimeSpan> { TimeSpan.FromSeconds(1) })));
    }

    [Fact]
    public void Deserialize_OlderVersion_MigratesBuiltinFields()
    {
        var json = "{\"started\":\"2026-10-09T10:30:15.2500000Z\",\"length\":\"00:10:00\"}";

        var result = new JsonSerializer().Deserialize<BuiltinSession>(Encoding.UTF8.GetBytes(json))!;

        Assert.Equal(s_created, result.Start);
        Assert.Equal(TimeSpan.FromMinutes(10), result.Length);
    }

    [Fact]
    public void Schema_BuiltinFields_AreStrings()
    {
        var schema = Formatters.GetSchema<BuiltinSample>();

        Assert.Equal(FieldTypeKind.String, schema.Fields[schema.IndexOf("id")].Type.Kind);
        Assert.Equal(FieldTypeKind.String, schema.Fields[schema.IndexOf("created")].Type.Kind);
        Assert.True(schema.Fields[schema.IndexOf("maybeId")].Type.IsNullable);
        Assert.False(schema.Fields[schema.IndexOf("id")].Type.IsNullable);
    }

    private static BuiltinSample CreateSample()
    {
        return new BuiltinSample
        {
            Id = s_id,
            Created = s_created,
            Unspecified = new DateTime(2026, 1, 2, 3, 4, 5),
            At = new DateTimeOffset(s_created.Ticks, TimeSpan.FromHours(3)),
            Duration = new TimeSpan(1, 30, 5),
            Negative = TimeSpan.FromDays(-2.5),
            OtherId = s_id,
            Dates = [s_created, DateTime.MinValue.ToUniversalTime()]
        };
    }
}

[Schema]
public sealed class BuiltinSample
{
    public Guid Id;
    public DateTime Created;
    public DateTime Unspecified;
    public DateTimeOffset At;
    public TimeSpan Duration;
    public TimeSpan Negative;
    public Guid? MaybeId;
    public Guid? OtherId;
    public List<DateTime>? Dates;
}

[Schema]
public sealed class BuiltinRow
{
    public Guid Id;
    public DateTime Expires;
    public TimeSpan Cooldown;
}

[Schema(version: 2)]
public sealed class BuiltinSession
{
    public DateTime Start;
    public TimeSpan Length;
}

public sealed class BuiltinSessionRenameStarted : IMigration<BuiltinSession>
{
    public int FromVersion => 1;

    public void Apply(DataValue root)
    {
        root.AsObject.Rename("started", "start");
    }
}
