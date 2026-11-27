using System.ComponentModel;

namespace GroveGames.Serialization;

// Generated registrations use it so a list root, such as every CSV, moves through the same versions as its items.
[EditorBrowsable(EditorBrowsableState.Never)]
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
        var items = root.AsArray;

        for (var i = 0; i < items.Count; i++)
        {
            _migration.Apply(items[i]);
        }
    }
}
