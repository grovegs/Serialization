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
- Use `[Formatter]` on an `IFormatter<T>` only for a type you do not own.
- Create each serializer once and reuse it, and write into a `ByteBuffer` you keep. Do not call `Serialize(value)` on a hot path; it returns a new array.
- Treat `FormatException` from `Deserialize` as corrupt data and recover; never let it crash the game.

## Unity

- The Unity package adds formatters for Unity and Unity.Mathematics types. Use them instead of converting vectors and colors by hand.
- Registration runs by itself at startup; do not call registration methods.
- An asmdef that overrides references lists `GroveGames.Serialization.dll` in its precompiled references.
