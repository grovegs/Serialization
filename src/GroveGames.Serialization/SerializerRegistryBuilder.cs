namespace GroveGames.Serialization;

public sealed class SerializerRegistryBuilder
{
    private readonly Dictionary<Type, Func<Dictionary<Type, List<object>>, object>> _registrations;
    private readonly Dictionary<Type, List<object>> _migrations;

    public SerializerRegistryBuilder()
    {
        _registrations = [];
        _migrations = [];
    }

    public SerializerRegistryBuilder AddFormatter<T>(IFormatter<T> formatter, int version = 1)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);

        if (_registrations.ContainsKey(typeof(T)))
        {
            throw new InvalidOperationException($"{typeof(T)} already has a formatter.");
        }

        _registrations.Add(typeof(T), migrations => CreateRegistration(formatter, version, migrations));
        return this;
    }

    public SerializerRegistryBuilder AddMigration<T>(IMigration<T> migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        if (!_migrations.TryGetValue(typeof(T), out var migrations))
        {
            migrations = [];
            _migrations.Add(typeof(T), migrations);
        }

        migrations.Add(migration);
        return this;
    }

    public SerializerRegistry Build()
    {
        foreach (var type in _migrations.Keys)
        {
            if (!_registrations.ContainsKey(type))
            {
                throw new InvalidOperationException($"{type} has migrations but no formatter.");
            }
        }

        var registrations = new Dictionary<Type, object>(_registrations.Count);

        foreach (var pair in _registrations)
        {
            registrations.Add(pair.Key, pair.Value(_migrations));
        }

        return new SerializerRegistry(registrations);
    }

    private static TypeRegistration<T> CreateRegistration<T>(IFormatter<T> formatter, int version, Dictionary<Type, List<object>> allMigrations)
    {
        var migrations = new IMigration<T>?[version];

        if (allMigrations.TryGetValue(typeof(T), out var registered))
        {
            for (var i = 0; i < registered.Count; i++)
            {
                var migration = (IMigration<T>)registered[i];
                var from = migration.FromVersion;

                if (from < 1 || from >= version)
                {
                    throw new InvalidOperationException($"{typeof(T)} is v{version}, so a migration from v{from} is out of range.");
                }

                if (migrations[from] != null)
                {
                    throw new InvalidOperationException($"{typeof(T)} has two migrations from v{from}.");
                }

                migrations[from] = migration;
            }
        }

        return new TypeRegistration<T>(formatter, version, migrations);
    }
}
