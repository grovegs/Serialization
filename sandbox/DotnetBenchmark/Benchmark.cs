using BenchmarkDotNet.Attributes;
using ConsoleApplication.Models;
using GroveGames.Serialization;
using SystemTextJson = System.Text.Json;

namespace DotnetBenchmark;

[MemoryDiagnoser]
[ShortRunJob]
public class Benchmark
{
    private static readonly SystemTextJson.JsonSerializerOptions s_systemTextJsonOptions = new() { IncludeFields = true, PropertyNamingPolicy = SystemTextJson.JsonNamingPolicy.CamelCase };

    private PlayerSave _save = null!;
    private JsonSerializer _json = null!;
    private MessagePackSerializer _messagePack = null!;
    private RowSerializer _rows = null!;
    private FieldTable _layout = null!;
    private byte[] _rowBytes = null!;
    private ByteBuffer _output = null!;
    private byte[] _jsonBytes = null!;
    private byte[] _messagePackBytes = null!;
    private byte[] _systemTextJsonBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        var registry = new FormatterRegistryBuilder()
            .AddDotnetBenchmarkFormatters()
            .Build();
        _json = new JsonSerializer(registry);
        _messagePack = new MessagePackSerializer(registry);
        _rows = new RowSerializer(registry);
        _layout = _rows.GetLayout<PlayerSave>();
        _output = new ByteBuffer(64 * 1024);
        _save = new PlayerSave { Name = "Hero", Level = 42, Gold = 1_000_000, PlayTime = 12_345.678, Items = [] };

        for (var i = 0; i < 100; i++)
        {
            _save.Items.Add(new Item { Id = "item" + i, Count = i, Weight = i * 0.25f });
        }

        _jsonBytes = _json.Serialize(_save);
        _messagePackBytes = _messagePack.Serialize(_save);
        _output.Reset();
        _rows.Serialize(_save, _output);
        _rowBytes = _output.ToArray();
        Console.WriteLine($"Sizes: JSON {_jsonBytes.Length}, MessagePack {_messagePackBytes.Length}, row {_rowBytes.Length}");
        _systemTextJsonBytes = SystemTextJson.JsonSerializer.SerializeToUtf8Bytes(_save, s_systemTextJsonOptions);
    }

    [Benchmark]
    public int JsonSerialize()
    {
        _output.Reset();
        _json.Serialize(_save, _output);
        return _output.Length;
    }

    [Benchmark]
    public int MessagePackSerialize()
    {
        _output.Reset();
        _messagePack.Serialize(_save, _output);
        return _output.Length;
    }

    [Benchmark]
    public int RowSerialize()
    {
        _output.Reset();
        _rows.Serialize(_save, _output);
        return _output.Length;
    }

    [Benchmark(Baseline = true)]
    public int SystemTextJsonSerialize()
    {
        return SystemTextJson.JsonSerializer.SerializeToUtf8Bytes(_save, s_systemTextJsonOptions).Length;
    }

    [Benchmark]
    public PlayerSave? JsonDeserialize()
    {
        return _json.Deserialize<PlayerSave>(_jsonBytes);
    }

    [Benchmark]
    public PlayerSave? MessagePackDeserialize()
    {
        return _messagePack.Deserialize<PlayerSave>(_messagePackBytes);
    }

    [Benchmark]
    public PlayerSave? RowDeserialize()
    {
        return _rows.Deserialize<PlayerSave>(_rowBytes, _layout);
    }

    [Benchmark]
    public PlayerSave? SystemTextJsonDeserialize()
    {
        return SystemTextJson.JsonSerializer.Deserialize<PlayerSave>(_systemTextJsonBytes, s_systemTextJsonOptions);
    }
}
