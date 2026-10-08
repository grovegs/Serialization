using GroveGames.Serialization;

namespace ConsoleApplication.Models;

public sealed class PlayerSaveRenameCoins : IMigration<PlayerSave>
{
    public int FromVersion => 1;

    public void Apply(DataValue root)
    {
        root.AsObject.Rename("coins", "gold");
    }
}
