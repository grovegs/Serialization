namespace ConsoleApplication.Models;

public sealed class PlayerSave
{
    public string? Name;
    public int Level;
    public long Gold;
    public double PlayTime;
    public List<Item>? Items;
}
