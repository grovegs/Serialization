using GroveGames.Serialization;

namespace ConsoleApplication.Models;

[Schema]
public sealed class Item
{
    public string? Id;
    public int Count;
    public float Weight;
}
