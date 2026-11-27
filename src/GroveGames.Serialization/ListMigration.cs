namespace GroveGames.Serialization;

// Register it with a versioned ListFormatter<T> so a list root, such as a CSV, runs its items' migrations. It is not
// registered automatically, because list roots written before were stored without a version and would migrate twice.
public sealed class ListMigration<T> : IMigration<List<T>>
{
    private readonly IMigration<T> _migration;

    public ListMigration(IMigration<T> migration)
    {
        ArgumentNullException.ThrowIfNull(migration);
        _migration = migration;
    }

    public int FromVersion => _migration.FromVersion;

    public void Apply(DataValue root)
    {
        if (root.IsNull)
        {
            return;
        }

        var items = root.AsArray;

        for (var i = 0; i < items.Count; i++)
        {
            if (!items[i].IsNull)
            {
                _migration.Apply(items[i]);
            }
        }
    }
}
