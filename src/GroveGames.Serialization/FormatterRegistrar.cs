namespace GroveGames.Serialization;

public sealed class FormatterRegistrar
{
    private readonly Dictionary<Type, Func<Dictionary<Type, List<object>>, Action>> _formatters;
    private readonly Dictionary<Type, List<object>> _migrations;

    internal FormatterRegistrar()
    {
        _formatters = [];
        _migrations = [];
    }

    public FormatterRegistrar AddFormatter<T>(IFormatter<T> formatter, int version = 1)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);

        if (_formatters.ContainsKey(typeof(T)))
        {
            throw new InvalidOperationException($"{typeof(T)} already has a formatter.");
        }

        _formatters.Add(typeof(T), migrations => Prepare(formatter, version, migrations));
        return this;
    }

    public FormatterRegistrar AddMigration<T>(IMigration<T> migration)
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

    internal void Commit()
    {
        foreach (var type in _migrations.Keys)
        {
            if (!_formatters.ContainsKey(type))
            {
                throw new InvalidOperationException($"{type} has migrations but no formatter.");
            }
        }

        var publishers = new List<Action>(_formatters.Count);

        foreach (var prepare in _formatters.Values)
        {
            publishers.Add(prepare(_migrations));
        }

        foreach (var publish in publishers)
        {
            publish();
        }
    }

    private static Action Prepare<T>(IFormatter<T> formatter, int version, Dictionary<Type, List<object>> allMigrations)
    {
        if (Volatile.Read(ref FormatterCache<T>.Registration) != null)
        {
            throw new InvalidOperationException($"{typeof(T)} already has a formatter.");
        }

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

        var registration = new TypeRegistration<T>(formatter, version, migrations);
        return () => Volatile.Write(ref FormatterCache<T>.Registration, registration);
    }
}
