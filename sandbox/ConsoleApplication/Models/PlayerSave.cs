using GroveGames.Serialization;

namespace ConsoleApplication.Models;

[Schema(version: 3)]
public sealed class PlayerSave
{
    public string? Name;
    public int Level;
    public long Gold;
    public double PlayTime;
    public List<Item>? Items;
}
