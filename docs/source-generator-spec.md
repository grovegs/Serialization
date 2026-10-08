# Source generator spec

Goal: a Roslyn incremental generator that emits an `IFormatter<T>` for every `[Schema]` type, so formatters no longer have to be written by hand. The hand-written formatters in `sandbox/ConsoleApplication/Models` are the reference output; match their shape.

## Project

- `src/GroveGames.Serialization.Generator/`, `netstandard2.0`, `IIncrementalGenerator`, `Microsoft.CodeAnalysis.CSharp` 4.3.x (works with Unity 2022.3 and later).
- Packed into the core NuGet package as an analyzer (`analyzers/dotnet/cs`). For Unity, the DLL ships in the UPM package with the `RoslynAnalyzer` asset label.
- Use `ForAttributeWithMetadataName("GroveGames.Serialization.SchemaAttribute")`. Keep the pipeline cacheable: project symbols into equatable records, with no `ISymbol` in the model.

## Input

```csharp
[Schema(version: 3)]
public partial class PlayerSave
{
    public string? Name;
    public int Level;
    public long Gold;
    public double PlayTime;
    public List<Item>? Items;
}
```

- Types: `class` or `struct`, and it must be `partial`. Nested and generic types are reported as unsupported for now.
- Members: public instance fields, and public properties with a public getter and a public setter or `init`, in declaration order. Skip `[Ignore]`, static, const and readonly members, and indexers.
- Field name: the camelCase form of the member name (`Gold` → `gold`, `HP` → `hp`, `URLPath` → `urlPath`).

## Supported member types

| Type                                      | Write                                                  | Read                                                           |
| ----------------------------------------- | ------------------------------------------------------ | -------------------------------------------------------------- |
| `int`, `short`, `byte`, `sbyte`, `ushort` | `WriteInt32`                                           | `ReadInt32` and a checked cast                                 |
| `long`, `uint`                            | `WriteInt64`                                           | `ReadInt64`, with a checked cast for `uint`                    |
| `float`                                   | `WriteSingle`                                          | `ReadSingle`                                                   |
| `double`                                  | `WriteDouble`                                          | `ReadDouble`                                                   |
| `bool`                                    | `WriteBool`                                            | `ReadBool`                                                     |
| `string`                                  | `WriteString`                                          | `ReadString`; transcode via `ReadStringUtf8`/`WriteStringUtf8` |
| enums                                     | the underlying integer                                 | integer and a cast                                             |
| `Nullable<T>` of the above                | `WriteNull` when there is no value                     | `Peek() == TokenType.Null` → `null`                            |
| another `[Schema]` type                   | a private readonly instance of its generated formatter | same                                                           |
| `List<T>`, `T[]` of any supported `T`     | inline loop                                            | inline loop                                                    |
| `Dictionary<string, T>`                   | an object with the keys as names                       | object                                                         |
| anything else, such as Unity's `Vector3`  | `registry.GetFormatter<T>()`                           | same                                                           |

A member type with no known formatter at compile time falls back to `registry.GetFormatter<T>()` and reports a warning suggesting `[Schema]` or a custom formatter.

## Output per type

1. `sealed class {Type}Formatter : IFormatter<{Type}>` with a static `FieldTable` in declaration order and `Write`, `Read` and `Transcode`, all taking the `SerializerRegistry`. `switch` on the field index, with `default: reader.Skip()`.
2. Nested `[Schema]` formatters are private readonly fields created in the constructor, not static instances.
3. Classes handle `null` at the top of each method. Structs do not.
4. One extension per assembly, so registration stays explicit and DI friendly:

   ```csharp
   public static SerializerRegistryBuilder Add{AssemblyName}Formatters(this SerializerRegistryBuilder builder)
   ```

   It registers every generated formatter with its `[Schema]` version, a `ListFormatter<T>` for each schema type, and every `IMigration<T>` class in the assembly (each needs a parameterless constructor). There is no module initializer and no static registry.

## Diagnostics (GGS0xx, errors unless noted)

- GGS001: `[Schema]` type is not `partial`.
- GGS002: nested or generic `[Schema]` type (unsupported).
- GGS003: two members map to the same camelCase name.
- GGS004: unsupported member type (a warning when falling back to `registry.GetFormatter<T>()`).
- GGS005: an `IMigration<T>` class has no parameterless constructor.
- GGS006: an `IMigration<T>` for a type that has no `[Schema]`.
- GGS007: gap in the migration chain (warning), for example version 3 with no migration from 1 or 2.

`FromVersion` is a runtime value, so range and duplicate checks stay in `SerializerRegistryBuilder.Build()`.

## Done when

- The console sample, tests and benchmark use the generator, and the hand-written formatters are deleted.
- The output for `PlayerSave` and `Item` is equivalent to the reference, with benchmark numbers within noise.
- Generator unit tests cover snapshots of emitted code and each diagnostic, using `CSharpGeneratorDriver`.
- Round-trip tests cover every supported member type, including nullable, enum, array, dictionary, nested schema and struct schema.
