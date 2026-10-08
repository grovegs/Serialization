using System.Text;
using ConsoleApplication.Models;
using GroveGames.Serialization;

namespace ConsoleApplication;

public static class Program
{
    public static void Main()
    {
        var json = new JsonSerializer();
        var messagePack = new MessagePackSerializer();
        var csv = new CsvSerializer();

        var save = new PlayerSave
        {
            Name = "Hero",
            Level = 12,
            Gold = 4_500,
            PlayTime = 3_600.5,
            Items = [new Item { Id = "sword", Count = 1, Weight = 3.5f }, new Item { Id = "potion", Count = 5, Weight = 0.2f }]
        };

        var jsonBytes = json.Serialize(save);
        Console.WriteLine($"JSON ({jsonBytes.Length} bytes): {Encoding.UTF8.GetString(jsonBytes)}");

        var messagePackBytes = messagePack.Serialize(save);
        Console.WriteLine($"MessagePack: {messagePackBytes.Length} bytes");

        var converted = new Converter(messagePack, json).Convert<PlayerSave>(messagePackBytes);
        Console.WriteLine($"MessagePack -> JSON matches: {converted.AsSpan().SequenceEqual(jsonBytes)}");

        Console.WriteLine($"CSV rows:\n{Encoding.UTF8.GetString(csv.Serialize(save.Items))}");

        var item = json.Serialize(new Item { Id = "shield", Count = 1 });
        Console.WriteLine($"v1 item has no version: {Encoding.UTF8.GetString(item)}");

        var oldSave = Encoding.UTF8.GetBytes("{\"name\":\"Veteran\",\"xp\":7200,\"coins\":90}");
        var migrated = json.Deserialize<PlayerSave>(oldSave)!;
        Console.WriteLine($"Save without a version is v1, migrated to v3: {migrated.Name}, level {migrated.Level}, gold {migrated.Gold}");
    }
}
