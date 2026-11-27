---
name: grovegames-serialization
description: Rules for serializing data in game code with GroveGames.Serialization. Use when writing or reviewing a type that is saved, loaded, sent or converted between JSON, MessagePack and CSV, or when changing the shape of such a type.
---

# Using GroveGames.Serialization

Rules for game code that uses this package. They hold in every game. The API is in the package's `README.md`.

- Mark a saved type with `[Schema]` and let the generator write its formatter. Do not write a formatter by hand for a type you own, and never serialize with reflection or an engine serializer.
- A schema type is top-level and non-generic, with public fields or public get-set properties and a parameterless constructor. Fix the generator's `GGS` errors instead of working around them.
- Field names are part of the saved format. Never rename, retype or reorder a member of a shipped type without raising the `[Schema(version: n)]` and adding an `IMigration<T>` for the step.
- Exclude runtime-only members with `[Ignore]`.
- Treat `GGS013` (a public member that is not saved) and `GGS014` (a versioned schema used as a member) as errors: mark the member `[Ignore]` or reshape the type, and keep versions on root types.
- Declare saved members on the schema type itself; inherited members are not saved.
- Use `[Formatter]` on an `IFormatter<T>` only for a type you do not own.
- Create each serializer once and reuse it, and write into a `ByteBuffer` you keep. Do not call `Serialize(value)` on a hot path; it returns a new array.
- Treat `FormatException` from `Deserialize` as unreadable data and recover; never let it crash the game. It covers corrupt bytes, data from a newer build, and a migration that is missing or throws.
- Give every `IMigration<T>` a constant `FromVersion` (`public int FromVersion => 1;`) so the generator can report a missing step (`GGS009`) at compile time.
