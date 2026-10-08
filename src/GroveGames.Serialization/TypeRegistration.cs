namespace GroveGames.Serialization;

internal sealed class TypeRegistration<T>
{
    private readonly IMigration<T>?[] _migrations;

    public TypeRegistration(IFormatter<T> formatter, int version, IMigration<T>?[] migrations)
    {
        Formatter = formatter;
        Version = version;
        _migrations = migrations;
    }

    public IFormatter<T> Formatter { get; }

    public int Version { get; }

    public void Migrate(DataNode root, int fromVersion)
    {
        for (var version = fromVersion; version < Version; version++)
        {
            var migration = version < _migrations.Length ? _migrations[version] : null;

            if (migration == null)
            {
                throw new NotSupportedException($"{typeof(T).Name} data is v{fromVersion}, but there is no migration from v{version} to v{version + 1}.");
            }

            migration.Apply(root);
        }
    }
}
