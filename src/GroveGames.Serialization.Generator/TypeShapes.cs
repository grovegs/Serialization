using GroveGames.Serialization.Generator.Models;
using Microsoft.CodeAnalysis;

namespace GroveGames.Serialization.Generator;

internal static class TypeShapes
{
    private const string SchemaAttribute = "GroveGames.Serialization.SchemaAttribute";
    private const string DataValue = "GroveGames.Serialization.DataValue";

    public static TypeShape? Create(ITypeSymbol type, Compilation compilation, out string? error)
    {
        error = null;
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
                return new TypeShape(ShapeKind.Bool, name, null, null, true, null);
            case SpecialType.System_Int32:
                return new TypeShape(ShapeKind.Int32, name, null, null, true, null);
            case SpecialType.System_Byte:
                return new TypeShape(ShapeKind.Int32, name, "byte", null, true, null);
            case SpecialType.System_SByte:
                return new TypeShape(ShapeKind.Int32, name, "sbyte", null, true, null);
            case SpecialType.System_Int16:
                return new TypeShape(ShapeKind.Int32, name, "short", null, true, null);
            case SpecialType.System_UInt16:
                return new TypeShape(ShapeKind.Int32, name, "ushort", null, true, null);
            case SpecialType.System_Int64:
                return new TypeShape(ShapeKind.Int64, name, null, null, true, null);
            case SpecialType.System_UInt32:
                return new TypeShape(ShapeKind.Int64, name, "uint", null, true, null);
            case SpecialType.System_Single:
                return new TypeShape(ShapeKind.Single, name, null, null, true, null);
            case SpecialType.System_Double:
                return new TypeShape(ShapeKind.Double, name, null, null, true, null);
            case SpecialType.System_String:
                return new TypeShape(ShapeKind.String, name, null, null, false, null);
            case SpecialType.System_UInt64:
            case SpecialType.System_Char:
            case SpecialType.System_Decimal:
            case SpecialType.System_Object:
                error = $"{type.ToDisplayString()} is not a supported type";
                return null;
        }

        if (type is IArrayTypeSymbol array)
        {
            if (array.Rank != 1)
            {
                error = "only one-dimensional arrays are supported";
                return null;
            }

            var element = Create(array.ElementType, compilation, out error);
            return element == null ? null : new TypeShape(ShapeKind.Array, name, null, element, false, null);
        }

        if (type is not INamedTypeSymbol named)
        {
            error = $"{type.ToDisplayString()} is not a supported type";
            return null;
        }

        if (named.TypeKind == TypeKind.Enum)
        {
            var number = named.EnumUnderlyingType?.SpecialType switch
            {
                SpecialType.System_Int32 => "int",
                SpecialType.System_Byte => "byte",
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_Int16 => "short",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_Int64 => "long",
                _ => null
            };

            if (number == null)
            {
                error = "enums backed by ulong are not supported";
                return null;
            }

            return new TypeShape(ShapeKind.Enum, name, number, null, true, null);
        }

        if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var element = Create(named.TypeArguments[0], compilation, out error);
            return element == null ? null : new TypeShape(ShapeKind.Nullable, name, null, element, true, null);
        }

        var definition = named.OriginalDefinition.ToDisplayString();

        if (definition == "System.Collections.Generic.List<T>")
        {
            var element = Create(named.TypeArguments[0], compilation, out error);
            return element == null ? null : new TypeShape(ShapeKind.List, name, null, element, false, null);
        }

        if (definition == "System.Collections.Generic.Dictionary<TKey, TValue>")
        {
            if (named.TypeArguments[0].SpecialType != SpecialType.System_String)
            {
                error = "only dictionaries with string keys are supported";
                return null;
            }

            var element = Create(named.TypeArguments[1], compilation, out error);
            return element == null ? null : new TypeShape(ShapeKind.Dictionary, name, null, element, false, null);
        }

        if (named.ToDisplayString() == DataValue)
        {
            return new TypeShape(ShapeKind.DataValue, name, null, null, true, null);
        }

        if (named.IsGenericType || named.TypeKind is TypeKind.Interface or TypeKind.Delegate or TypeKind.TypeParameter)
        {
            error = $"{type.ToDisplayString()} is not a supported type";
            return null;
        }

        if (HasSchema(named) && SymbolEqualityComparer.Default.Equals(named.ContainingAssembly, compilation.Assembly) && named.ContainingType == null)
        {
            var formatter = named.ContainingNamespace.IsGlobalNamespace
                ? "global::" + named.Name + "Formatter"
                : "global::" + named.ContainingNamespace.ToDisplayString() + "." + named.Name + "Formatter";
            return new TypeShape(ShapeKind.Schema, name, null, null, named.IsValueType, formatter);
        }

        return new TypeShape(ShapeKind.Registered, name, null, null, named.IsValueType, null);
    }

    public static bool HasSchema(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == SchemaAttribute)
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasRegisteredMember(TypeShape shape)
    {
        return shape.Kind == ShapeKind.Registered || (shape.Element != null && HasRegisteredMember(shape.Element));
    }
}
