namespace GroveGames.Serialization.Generator.Models;

internal sealed record TypeShape(ShapeKind Kind, string TypeName, string? NumberType, TypeShape? Element, bool IsValueType, string? FormatterName);
