using GroveGames.Serialization;

namespace ConsoleApplication.Models;

public sealed class PlayerSaveXpToLevel : IMigration<PlayerSave>
{
    public int FromVersion => 2;

    public void Apply(DataNode root)
    {
        var xp = root.Has("xp") ? root["xp"]!.AsInt64 : 0;
        root["level"] = DataNode.FromInt((xp / 1000) + 1);
        root.Remove("xp");
    }
}
