namespace GroveGames.Serialization;

internal sealed class TypeRegistration<T>
{
    private readonly IMigration<T>?[] _migrations;
    private readonly IFormatter<T> _formatter;
    private readonly int _version;

    public TypeRegistration(IFormatter<T> formatter, int version, IMigration<T>?[] migrations)
    {
        _formatter = formatter;
        _version = version;
        _migrations = migrations;
    }

    public IFormatter<T> Formatter => _formatter;

    public int Version => _version;

    public void Migrate(DataValue root, int fromVersion)
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
