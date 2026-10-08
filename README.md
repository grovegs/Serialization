# GroveGames.Serialization

High-performance JSON, MessagePack and CSV serialization for .NET, Unity and Godot, with conversion between formats and versioned migrations.

[![Build Status](https://github.com/grovegs/Serialization/actions/workflows/release.yml/badge.svg)](https://github.com/grovegs/Serialization/actions/workflows/release.yml)
[![Tests](https://github.com/grovegs/Serialization/actions/workflows/tests.yml/badge.svg)](https://github.com/grovegs/Serialization/actions/workflows/tests.yml)
[![Latest Release](https://img.shields.io/github/v/release/grovegs/Serialization)](https://github.com/grovegs/Serialization/releases/latest)
[![NuGet](https://img.shields.io/nuget/v/GroveGames.Serialization)](https://www.nuget.org/packages/GroveGames.Serialization)

---

## Features

- **Three formats, one model**: JSON, MessagePack and CSV read and write the same types through the same formatter.
- **Zero-allocation serialization**: Writers are structs over a reusable `ByteBuffer`. Deserializing allocates only the resulting objects.
- **Format conversion**: Convert JSON, MessagePack and CSV into each other without creating the objects.
- **Versioned migrations**: Older data is upgraded step by step on load and on conversion, from a version stored in the payload or anywhere else.
- **Safe on corrupt data**: Malformed or truncated input throws `FormatException` and never reads out of range or overflows the stack.
- **DI friendly**: No global state. Formatters and migrations live in an immutable `SerializerRegistry` that each serializer receives in its constructor.
- **Unity integration**: Formatters for Unity and Unity.Mathematics types, plus a GroveGames.DependencyInjection integration.

## .NET

Install via NuGet:

```bash
dotnet add package GroveGames.Serialization
```

### Formatters

Each type has an `IFormatter<T>` that writes its fields by name. Every public member is written under its camelCase name in every format. Until the source generator is available, formatters are written by hand; see `sandbox/ConsoleApplication/Models` for complete examples.

```csharp
public sealed class ItemFormatter : IFormatter<Item>
{
    private static readonly FieldTable s_fields = new("id", "count");

    public void Write<TWriter>(ref TWriter writer, Item? value, SerializerRegistry registry) where TWriter : struct, IFormatWriter
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.BeginObject(2);
        writer.WriteField(s_fields[0]);
        writer.WriteString(value.Id);
        writer.WriteField(s_fields[1]);
        writer.WriteInt32(value.Count);
        writer.EndObject();
    }

    public Item? Read<TReader>(ref TReader reader, SerializerRegistry registry) where TReader : struct, IFormatReader
    {
        if (reader.Peek() == TokenType.Null)
        {
            reader.Skip();
            return null;
        }

        var value = new Item();
        reader.ReadObjectStart();

        while (reader.TryReadField(s_fields, out var index))
        {
            switch (index)
            {
                case 0:
                    value.Id = reader.ReadString();
                    break;
                case 1:
                    value.Count = reader.ReadInt32();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return value;
    }

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer, SerializerRegistry registry)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter
    {
        Write(ref writer, Read(ref reader, registry), registry);
    }
}
```

Unknown fields are skipped and missing fields keep their defaults, so adding or removing a field needs no migration.

### Serializing

```csharp
var registry = new SerializerRegistryBuilder()
    .AddFormatter(new ItemFormatter())
    .AddFormatter(new ListFormatter<Item>())
    .AddFormatter(new PlayerSaveFormatter(), version: 3)
    .Build();

var json = new JsonSerializer(registry);
var messagePack = new MessagePackSerializer(registry);
var csv = new CsvSerializer(registry);

byte[] bytes = messagePack.Serialize(save);
PlayerSave? loaded = messagePack.Deserialize<PlayerSave>(bytes);

var buffer = new ByteBuffer();
json.Serialize(save, buffer);
```

Serializers write the bare value. `Serialize(value, IBufferWriter<byte>)` writes into any buffer writer; a `ByteBuffer` you own and reuse is written with no allocation, and any other buffer writer receives the bytes in one copy. `Serialize(value)` returns a new `byte[]`, and there are `Stream` overloads.

### Converting

```csharp
var toJson = new Converter(messagePack, json);
byte[] jsonBytes = toJson.Convert<PlayerSave>(bytes);
```

Both serializers must share the same registry. `Convert<T>(data, version, output)` migrates data stored at an older version while converting.

### Versions and Migrations

Each root type has a version in the registry. There are two ways to keep track of the version data was written with:

- **Stored elsewhere**, such as in a database record or collection: pass it to `Deserialize<T>(data, version)`.
- **Inside the payload**, for files and messages: wrap the serializer in a `VersionedSerializer`, which writes `{"$v":3,"data":{...}}` in JSON and MessagePack and a `#v=3` first line in CSV. Use `VersionedConverter` to convert these payloads.

```csharp
PlayerSave? record = json.Deserialize<PlayerSave>(storedBytes, version: 1);

var file = new VersionedSerializer(json);
byte[] fileBytes = file.Serialize(save);
PlayerSave? loaded = file.Deserialize<PlayerSave>(fileBytes);
```

Data from an older version is loaded into a `DataNode` tree, each migration from its version runs in order, and the result is read normally. Data from a newer version throws `NotSupportedException`.

```csharp
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

var registry = new SerializerRegistryBuilder()
    .AddFormatter(new PlayerSaveFormatter(), version: 3)
    .AddMigration(new PlayerSaveRenameCoins())
    .AddMigration(new PlayerSaveXpToLevel())
    .Build();
```

Versions belong to the root type. A change inside a nested type is migrated by the root type that contains it.

### Formats

| Format      | Shape                            | Notes                                                                                    |
| ----------- | -------------------------------- | ---------------------------------------------------------------------------------------- |
| JSON        | Objects keyed by camelCase names | `NaN` and infinities are written as the strings `"NaN"`, `"Infinity"` and `"-Infinity"`. |
| MessagePack | Maps keyed by name strings       | Floats are 32-bit (`0xca`), doubles 64-bit (`0xcb`).                                     |
| CSV         | The root is a list of flat rows  | Nested objects and lists throw `NotSupportedException`.                                  |

Values: `int`, `long`, `float`, `double`, `bool`, `string`, nested objects, lists and `null`.

### Core Components

- **`ISerializer`**: `JsonSerializer`, `MessagePackSerializer` and `CsvSerializer`, each built with a `SerializerRegistry`
- **`IVersionedSerializer`** / **`VersionedSerializer`**: Adds the version to the payload for files and messages
- **`IConverter`**: `Converter` for bare data and `VersionedConverter` for versioned payloads, without creating objects
- **`SerializerRegistryBuilder`** / **`SerializerRegistry`**: Registers formatters, versions and migrations, then freezes them
- **`IFormatter<T>`**: Writes, reads and transcodes one type over any format
- **`IMigration<T>`** / **`DataNode`**: Upgrades older data by field name
- **`ListFormatter<T>`**: Formatter for a `List<T>` root
- **`ByteBuffer`**: Growable, reusable `IBufferWriter<byte>`

## Unity

Install the core through [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) (`GroveGames.Serialization`), then add the package to `Packages/manifest.json`:

```json
{
    "dependencies": {
        "com.grovegames.serialization": "https://github.com/grovegs/Serialization.git?path=src/GroveGames.Serialization.Unity/Packages/com.grovegames.serialization"
    }
}
```

### Unity Formatters

```csharp
var registry = new SerializerRegistryBuilder()
    .AddUnityFormatters()
    .AddMathematicsFormatters()
    .AddFormatter(new PlayerSaveFormatter(), version: 3)
    .Build();
```

`AddUnityFormatters` registers `Vector2`, `Vector3`, `Vector4`, `Vector2Int`, `Vector3Int`, `Quaternion`, `Color`, `Color32`, `Rect` and `Bounds`. `AddMathematicsFormatters` registers `float2`, `float3`, `float4`, `int2`, `int3` and `quaternion`, and compiles only when `com.unity.mathematics` is installed. Each value is an object of named components, such as `{"x":1,"y":2,"z":3}`, so it is not supported in CSV rows.

### Dependency Injection

With [GroveGames.DependencyInjection](https://github.com/grovegs/DependencyInjection) installed, register the registry and the three serializers in an installer:

```csharp
builder.AddSerialization(registry);
```

Inject `JsonSerializer`, `MessagePackSerializer` or `CsvSerializer` where you need them.

## Godot

Download the Godot addon from the [latest release](https://github.com/grovegs/Serialization/releases/latest) and extract it to your project's `addons` folder. The core API is the same as in .NET.

## Architecture

- **Struct readers and writers**: Formatters are generic over `TWriter : struct, IFormatWriter` and `TReader : struct, IFormatReader`, so every per-value call is direct, including under IL2CPP.
- **Pre-encoded names**: `FieldTable` stores field names as UTF-8, so writers never encode them and readers match them without allocating.
- **Fast path**: Data at the current version is read straight into the object. Only older data builds a `DataNode` tree.
- **Bounded nesting**: Readers stop at 63 levels of nesting and throw `FormatException`.

## Testing

```bash
dotnet test
```

The Unity package tests run from `sandbox/UnityApplication` with the Unity Test Runner.

---

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Submit a pull request

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
