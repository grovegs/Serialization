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
- **Versioned migrations**: Older data is upgraded step by step on load and on conversion. Data without a version counts as v1.
- **Safe on corrupt data**: Malformed or truncated input throws `FormatException` and never reads out of range or overflows the stack.
- **No configuration**: Formatters, versions and migrations register themselves the first time a type is used. Serializers need no setup, and finding a formatter is a static field read.
- **Unity integration**: Formatters for Unity and Unity.Mathematics types, plus a GroveGames.DependencyInjection integration.

## .NET

Install via NuGet:

```bash
dotnet add package GroveGames.Serialization
```

### Schema Types

Mark a type with `[Schema]` and the bundled source generator writes its formatter at compile time, with no reflection at runtime:

```csharp
[Schema(version: 3)]
public sealed class PlayerSave
{
    public string? Name;
    public int Level;
    public long Gold;
    public List<Item>? Items;

    [Ignore]
    public int Cached;
}
```

Every public field and every public property with a public getter and setter is serialized under its camelCase name (`Gold` → `gold`, `URLPath` → `urlPath`), in declaration order. `[Ignore]` excludes a member.

Supported member types: `bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `float`, `double`, `string`, enums, `Nullable<T>`, other `[Schema]` types (classes or structs), `List<T>`, `T[]`, `Dictionary<string, T>` and `DataValue`. Any other type, such as Unity's `Vector3`, uses the formatter registered for it.

The generator also registers every schema type with its version, a `ListFormatter<T>` for each, and every `IMigration<T>` for those types. Nothing needs to be called: the registration runs when the assembly starts.

| Diagnostic | Severity | Meaning                                                                 |
| ---------- | -------- | ----------------------------------------------------------------------- |
| GGS001     | Error    | Nested and generic types cannot be schema types                         |
| GGS002     | Error    | Two members map to the same field name                                  |
| GGS003     | Error    | A member type is not supported, including `ulong` and init-only setters |
| GGS004     | Info     | A member uses a `[Formatter]` because its type has no `[Schema]`        |
| GGS005     | Error    | A migration for a schema type has no parameterless constructor          |
| GGS006     | Error    | A schema class has no parameterless constructor                         |
| GGS007     | Error    | A schema or formatter version is below 1                                |
| GGS008     | Error    | A `[Formatter]` class is not a valid formatter                          |

### Type Schemas

Generated formatters describe their type at runtime:

```csharp
TypeSchema schema = Formatters.GetSchema<PlayerSave>();
FieldType level = schema.Fields[schema.IndexOf("level")].Type;
ulong fingerprint = schema.Fingerprint;
```

`FieldType` gives the declared kind (`Bool`, `Int32`, `Int64`, `Single`, `Double`, `String`, `Array`, `Map`, `Object` or `Any`), whether it is nullable, the element type of arrays and maps, and the CLR type of objects. The fingerprint changes whenever a field name or type changes, so storage can detect a schema change that was not given a new version.

### Custom Formatters

A type can also have a hand-written `IFormatter<T>`. Mark it with `[Formatter]`, or `[Formatter(version: 2)]` for a versioned type, and the generator registers it with its list formatter and its migrations:

```csharp
[Formatter]
internal sealed class PointFormatter : IFormatter<Point>
{
}
```

A formatter needs a parameterless constructor and must be public or internal. See the Unity package's formatters for complete examples.

### Registration

The generator writes the registration of each assembly's `[Schema]` types and `[Formatter]` classes at compile time, and runs it at startup: with `RuntimeInitializeOnLoadMethod` (and `InitializeOnLoadMethod` in the editor) when the assembly is compiled by Unity, and with a module initializer everywhere else. There is no reflection or assembly scanning. In Unity, a DLL that was compiled outside Unity is registered the first time one of its types is used, because Unity does not run module initializers on its own.

`Formatters.Get<T>()`, `GetVersion<T>()` and `GetSchema<T>()` return what is registered for a type. Each type's registration is cached in a static field, so a lookup is a field read.

Each type has exactly one formatter. Registering a type twice throws, naming the type.

### Serializing

```csharp
var json = new JsonSerializer();
var messagePack = new MessagePackSerializer();
var csv = new CsvSerializer();

byte[] bytes = messagePack.Serialize(save);
PlayerSave? loaded = messagePack.Deserialize<PlayerSave>(bytes);

var buffer = new ByteBuffer();
json.Serialize(save, buffer);
```

`Serialize(value, IBufferWriter<byte>)` writes into any buffer writer; a `ByteBuffer` you own and reuse is written with no allocation, and any other buffer writer receives the bytes in one copy. `Serialize(value)` returns a new `byte[]`, and there are `Stream` overloads.

### Converting

```csharp
var toJson = new Converter(messagePack, json);
byte[] jsonBytes = toJson.Convert<PlayerSave>(bytes);
```

Older data is migrated while converting.

### Versions and Migrations

Each root type has a version, and the version is written with the value only when it is above 1:

| Format      | Version 1 | Version 3                          |
| ----------- | --------- | ---------------------------------- |
| JSON        | the value | `{"$v":3,"data":{...}}`            |
| MessagePack | the value | a 3-byte extension, then the value |
| CSV         | the rows  | a `#v=3` first line, then the rows |

Data without a version counts as v1, so data written before a type was versioned keeps loading. Older data is loaded into a `DataValue` tree, each migration from its version runs in order, and the result is read normally. Data from a newer version throws `NotSupportedException`.

```csharp
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

```

Migrations for `[Schema]` types are registered by the generated method. Each needs a parameterless constructor.

Versions belong to the root type. A change inside a nested type is migrated by the root type that contains it.

### Formats

| Format      | Shape                            | Notes                                                                                    |
| ----------- | -------------------------------- | ---------------------------------------------------------------------------------------- |
| JSON        | Objects keyed by camelCase names | `NaN` and infinities are written as the strings `"NaN"`, `"Infinity"` and `"-Infinity"`. |
| MessagePack | Maps keyed by name strings       | Floats are 32-bit (`0xca`), doubles 64-bit (`0xcb`).                                     |
| CSV         | The root is a list of flat rows  | Nested objects and lists throw `NotSupportedException`.                                  |

Values: `int`, `long`, `float`, `double`, `bool`, `string`, nested objects, lists and `null`.

### Core Components

- **`ISerializer`**: `JsonSerializer`, `MessagePackSerializer` and `CsvSerializer`
- **`IConverter`** / **`Converter`**: Converts data between two serializers without creating objects
- **`Formatters`**: The formatter, version and schema registered for each type
- **`[Formatter]`**: Registers a hand-written formatter
- **`[Schema]`**: Generates a formatter, a `TypeSchema` and a registration method at compile time
- **`IFormatter<T>`** / **`ISchemaFormatter<T>`**: Writes, reads and transcodes one type over any format; schema formatters also describe the type
- **`TypeSchema`** / **`SchemaField`** / **`FieldType`**: Runtime description of a type's fields
- **`IMigration<T>`**: Upgrades older data by field name
- **`DataValue`** / **`DataObject`** / **`DataArray`**: A tagged value type and its containers, used by migrations and for schema-less data
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

The package registers `Vector2`, `Vector3`, `Vector4`, `Vector2Int`, `Vector3Int`, `Quaternion`, `Color`, `Color32`, `Rect` and `Bounds` at startup, and `float2`, `float3`, `float4`, `int2`, `int3` and `quaternion` when `com.unity.mathematics` is installed, each with its list formatter. Each value is an object of named components, such as `{"x":1,"y":2,"z":3}`, so it is not supported in CSV rows.

### Dependency Injection

With [GroveGames.DependencyInjection](https://github.com/grovegs/DependencyInjection) installed, register the three serializers in an installer:

```csharp
builder.AddSerialization();
```

Inject `JsonSerializer`, `MessagePackSerializer` or `CsvSerializer` where you need them.

## Godot

Download the Godot addon from the [latest release](https://github.com/grovegs/Serialization/releases/latest) and extract it to your project's `addons` folder. The core API is the same as in .NET.

## Architecture

- **Struct readers and writers**: Formatters are generic over `TWriter : struct, IFormatWriter` and `TReader : struct, IFormatReader`, so every per-value call is direct, including under IL2CPP.
- **Pre-encoded names**: `FieldTable` stores field names as UTF-8, so writers never encode them and readers match them without allocating.
- **Fast path**: Data at the current version is read straight into the object. Only older data builds a `DataValue` tree, whose scalars are stored inline without allocating.
- **Bounded nesting**: Readers stop at 63 levels of nesting and throw `FormatException`.

## Testing

```bash
dotnet test
```

This runs the core tests and the source generator tests, which CI runs as two jobs.

The Unity package tests run from `sandbox/UnityApplication` with the Unity Test Runner. Before opening the sandbox the first time, build the core library and the generator into it, because the package cannot compile until they exist:

```bash
cd sandbox/UnityApplication
dotnet build ../../src/GroveGames.Serialization -c Release -f netstandard2.1 -o Assets/Plugins
dotnet build ../../src/GroveGames.Serialization.Generator -c Release -o Assets/Plugins/Analyzers
```

After that, `Grove Games > Plugin Builder > Build` rebuilds both and labels the generator as a Roslyn analyzer.

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
