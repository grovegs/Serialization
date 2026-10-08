using System.Text;
using ConsoleApplication.Models;
using GroveGames.Serialization;

namespace ConsoleApplication;

public static class Program
{
    public static void Main()
    {
        var registry = new SerializerRegistryBuilder()
            .AddFormatter(new ItemFormatter())
            .AddFormatter(new ListFormatter<Item>())
            .AddFormatter(new PlayerSaveFormatter(), version: 3)
            .AddMigration(new PlayerSaveRenameCoins())
            .AddMigration(new PlayerSaveXpToLevel())
            .Build();

        var json = new JsonSerializer(registry);
        var messagePack = new MessagePackSerializer(registry);
        var csv = new CsvSerializer(registry);

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

        var old = "{\"$v\":1,\"data\":{\"name\":\"Veteran\",\"xp\":7200,\"coins\":90}}";
        var migrated = json.Deserialize<PlayerSave>(Encoding.UTF8.GetBytes(old))!;
        Console.WriteLine($"Migrated v1 -> v3: {migrated.Name}, level {migrated.Level}, gold {migrated.Gold}");
    }
}
