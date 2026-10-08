using GroveGames.Serialization;

namespace ConsoleApplication.Models;

public sealed class PlayerSaveXpToLevel : IMigration<PlayerSave>
{
    public int FromVersion => 2;

    public void Apply(DataValue root)
    {
        var save = root.AsObject;
        var xp = save.TryGetValue("xp", out var value) ? value.AsInt64 : 0;
        save["level"] = (xp / 1000) + 1;
        save.Remove("xp");
    }
}
