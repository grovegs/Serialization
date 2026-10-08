namespace GroveGames.Serialization;

public sealed class FieldType
{
    private FieldType(FieldTypeKind kind, bool isNullable, FieldType? elementType, Type? objectType)
    {
        Kind = kind;
        IsNullable = isNullable;
        ElementType = elementType;
        ObjectType = objectType;
    }

    public FieldTypeKind Kind { get; }

    public bool IsNullable { get; }

    public FieldType? ElementType { get; }

    public Type? ObjectType { get; }

    public static FieldType Any()
    {
        return new FieldType(FieldTypeKind.Any, true, null, null);
    }

    public static FieldType Primitive(FieldTypeKind kind, bool isNullable)
    {
        if (kind is FieldTypeKind.Any or FieldTypeKind.Array or FieldTypeKind.Map or FieldTypeKind.Object)
        {
            throw new ArgumentException($"{kind} is not a primitive kind.", nameof(kind));
        }

        return new FieldType(kind, isNullable, null, null);
    }

    public static FieldType Array(FieldType elementType, bool isNullable)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        return new FieldType(FieldTypeKind.Array, isNullable, elementType, null);
    }

    public static FieldType Map(FieldType valueType, bool isNullable)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        return new FieldType(FieldTypeKind.Map, isNullable, valueType, null);
    }

    public static FieldType Object(Type objectType, bool isNullable)
    {
        ArgumentNullException.ThrowIfNull(objectType);
        return new FieldType(FieldTypeKind.Object, isNullable, null, objectType);
    }
}
