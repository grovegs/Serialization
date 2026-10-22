namespace GroveGames.Serialization.Tests;

public sealed class FormattersTests
{
    [Fact]
    public void AddFormatter_SameTypeTwice_ThrowsInvalidOperationException()
    {
        var registrar = new FormatterRegistrar().AddFormatter(new TestSaveFormatter<TestSave>());

        Assert.Throws<InvalidOperationException>(() => registrar.AddFormatter(new TestSaveFormatter<TestSave>()));
    }

    [Fact]
    public void AddFormatter_ZeroVersion_ThrowsArgumentOutOfRangeException()
    {
        var registrar = new FormatterRegistrar();

        Assert.Throws<ArgumentOutOfRangeException>(() => registrar.AddFormatter(new TestSaveFormatter<TestSave>(), version: 0));
    }

    [Fact]
    public void Register_MigrationWithoutFormatter_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Formatters.Register(registrar => registrar.AddMigration(new TestSaveRenameCoins<MigrationOnlySave>())));
    }

    [Fact]
    public void Register_MigrationFromCurrentVersion_ThrowsAndRegistersNothing()
    {
        Assert.Throws<InvalidOperationException>(() => Formatters.Register(registrar => registrar
            .AddFormatter(new TestSaveFormatter<CurrentVersionSave>(), version: 1)
            .AddMigration(new TestSaveRenameCoins<CurrentVersionSave>())));

        Assert.False(Formatters.TryGet<CurrentVersionSave>(out _));
    }

    [Fact]
    public void Register_TwoMigrationsFromSameVersion_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Formatters.Register(registrar => registrar
            .AddFormatter(new TestSaveFormatter<TwoMigrationsSave>(), version: 2)
            .AddMigration(new TestSaveRenameCoins<TwoMigrationsSave>())
            .AddMigration(new TestSaveRenameCoins<TwoMigrationsSave>())));
    }

    [Fact]
    public void Register_Formatter_IsReturnedWithVersion()
    {
        var formatter = new TestSaveFormatter<VersionedSave>();

        Formatters.Register(registrar => registrar.AddFormatter(formatter, version: 3));

        Assert.Same(formatter, Formatters.Get<VersionedSave>());
        Assert.Equal(3, Formatters.GetVersion<VersionedSave>());
    }

    [Fact]
    public void Register_AlreadyRegisteredType_ThrowsAndRegistersNothing()
    {
        Formatters.Register(registrar => registrar.AddFormatter(new TestSaveFormatter<FirstSave>()));

        Assert.Throws<InvalidOperationException>(() => Formatters.Register(registrar => registrar
            .AddFormatter(new TestSaveFormatter<SecondSave>())
            .AddFormatter(new TestOtherSaveFormatter<FirstSave>())));

        Assert.False(Formatters.TryGet<SecondSave>(out _));
    }

    [Fact]
    public void Register_SameFormatterAndVersionTwice_KeepsFirstRegistration()
    {
        var first = new TestSaveFormatter<RepeatedSave>();
        Formatters.Register(registrar => registrar.AddFormatter(first, version: 2));

        Formatters.Register(registrar => registrar.AddFormatter(new TestSaveFormatter<RepeatedSave>(), version: 2));

        Assert.Same(first, Formatters.Get<RepeatedSave>());
        Assert.Equal(2, Formatters.GetVersion<RepeatedSave>());
    }

    [Fact]
    public void Register_SameFormatterWithOtherVersion_ThrowsInvalidOperationException()
    {
        Formatters.Register(registrar => registrar.AddFormatter(new TestSaveFormatter<ReversionedSave>()));

        Assert.Throws<InvalidOperationException>(() => Formatters.Register(registrar => registrar.AddFormatter(new TestSaveFormatter<ReversionedSave>(), version: 2)));
    }

    [Fact]
    public void Register_FailedConfiguration_CanBeRetried()
    {
        var attempts = 0;
        Action<FormatterRegistrar> configure = registrar =>
        {
            attempts++;
            registrar.AddFormatter(new TestSaveFormatter<RetriedSave>(), version: attempts);

            if (attempts == 1)
            {
                registrar.AddMigration(new TestSaveRenameCoins<RetriedSave>());
            }
        };
        Assert.Throws<InvalidOperationException>(() => Formatters.Register(configure));

        Formatters.Register(configure);

        Assert.True(Formatters.TryGet<RetriedSave>(out _));
    }

    [Fact]
    public void Get_UnregisteredType_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => Formatters.Get<UnregisteredSave>());
        Assert.False(Formatters.TryGet<UnregisteredSave>(out _));
    }

    [Fact]
    public void Get_SchemaType_IsRegisteredWhenAssemblyLoads()
    {
        Assert.True(Formatters.TryGet<GeneratedSample>(out _));
        Assert.True(Formatters.TryGet<List<GeneratedSample>>(out _));
        Assert.Equal(2, Formatters.GetVersion<GeneratedChild>());
        Assert.NotNull(Formatters.GetSchema<GeneratedSample>());
    }

    [Fact]
    public void Serialize_DataValueRoot_UsesCoreFormatter()
    {
        var serializer = new JsonSerializer();
        var value = new DataObject { ["name"] = "hero", ["level"] = 3 };

        var result = serializer.Deserialize<DataValue>(serializer.Serialize<DataValue>(value));

        Assert.Equal("hero", result.AsObject["name"].AsString);
        Assert.Equal(3, result.AsObject["level"].AsInt64);
    }

    private class TestSave
    {
        public string? Name;
        public int Level;
        public long Gold;
    }

    private sealed class MigrationOnlySave : TestSave
    {
    }

    private sealed class CurrentVersionSave : TestSave
    {
    }

    private sealed class TwoMigrationsSave : TestSave
    {
    }

    private sealed class VersionedSave : TestSave
    {
    }

    private sealed class FirstSave : TestSave
    {
    }

    private sealed class SecondSave : TestSave
    {
    }

    private sealed class UnregisteredSave : TestSave
    {
    }

    private sealed class RepeatedSave : TestSave
    {
    }

    private sealed class RetriedSave : TestSave
    {
    }

    private sealed class ReversionedSave : TestSave
    {
    }

    private sealed class TestSaveFormatter<T> : IFormatter<T>
        where T : TestSave, new()
    {
        public void Write<TWriter>(ref TWriter writer, T? value) where TWriter : struct, IFormatWriter
        {
            writer.WriteNull();
        }

        public T? Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
        {
            reader.Skip();
            return new T();
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }

    private sealed class TestOtherSaveFormatter<T> : IFormatter<T>
        where T : TestSave, new()
    {
        public void Write<TWriter>(ref TWriter writer, T? value) where TWriter : struct, IFormatWriter
        {
            writer.WriteNull();
        }

        public T? Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
        {
            reader.Skip();
            return new T();
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }

    private sealed class TestSaveRenameCoins<T> : IMigration<T>
        where T : TestSave
    {
        public int FromVersion => 1;

        public void Apply(DataValue root)
        {
            root.AsObject.Rename("coins", "gold");
        }
    }
}
