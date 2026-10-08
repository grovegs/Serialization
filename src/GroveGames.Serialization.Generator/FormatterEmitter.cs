using GroveGames.Serialization.Generator.Models;

namespace GroveGames.Serialization.Generator;

internal static class FormatterEmitter
{
    private const string Core = "global::GroveGames.Serialization.";

    public static string Emit(SchemaTypeModel model)
    {
        var code = new CodeBuilder();
        var members = model.Members;
        var usesDataValue = false;

        foreach (var member in members)
        {
            usesDataValue |= UsesDataValue(member.Shape);
        }

        code.Line("#nullable disable");
        var hasNamespace = !string.IsNullOrEmpty(model.Namespace);

        if (hasNamespace)
        {
            code.Open($"namespace {model.Namespace}");
        }

        code.Open($"internal sealed class {model.FormatterName} : {Core}ISchemaFormatter<{model.FullName}>");
        code.Line($"public static readonly {model.FormatterName} Instance = new {model.FormatterName}();");
        code.Line($"private static readonly {Core}FieldTable s_fields = new {Core}FieldTable({FieldNameList(model)});");
        code.Line($"private static readonly {Core}TypeSchema s_schema = new {Core}TypeSchema(typeof({model.FullName}), {model.Version}{SchemaFieldList(model)});");

        if (usesDataValue)
        {
            code.Line($"private static readonly {Core}DataValueFormatter s_dataValue = new {Core}DataValueFormatter();");
        }

        code.Line();
        code.Line($"public {Core}TypeSchema Schema => s_schema;");
        code.Line();
        EmitWrite(code, model);
        code.Line();
        EmitRead(code, model);
        code.Line();
        EmitTranscode(code, model);
        EmitConversions(code);
        code.Close();

        if (hasNamespace)
        {
            code.Close();
        }

        return code.ToString();
    }

    private static string FieldNameList(SchemaTypeModel model)
    {
        var names = new List<string>();

        foreach (var member in model.Members)
        {
            names.Add("\"" + member.FieldName + "\"");
        }

        return string.Join(", ", names);
    }

    private static string SchemaFieldList(SchemaTypeModel model)
    {
        var fields = new List<string>();

        foreach (var member in model.Members)
        {
            fields.Add($", new {Core}SchemaField(\"{member.FieldName}\", {FieldTypeExpression(member.Shape, false)})");
        }

        return string.Concat(fields);
    }

    private static string FieldTypeExpression(TypeShape shape, bool nullable)
    {
        var isNullable = nullable || !shape.IsValueType;

        return shape.Kind switch
        {
            ShapeKind.Bool => Primitive("Bool", isNullable),
            ShapeKind.Int32 => Primitive("Int32", isNullable),
            ShapeKind.Int64 => Primitive("Int64", isNullable),
            ShapeKind.Single => Primitive("Single", isNullable),
            ShapeKind.Double => Primitive("Double", isNullable),
            ShapeKind.String => Primitive("String", true),
            ShapeKind.Enum => Primitive(shape.NumberType is "uint" or "long" ? "Int64" : "Int32", isNullable),
            ShapeKind.Nullable => FieldTypeExpression(shape.Element!, true),
            ShapeKind.List or ShapeKind.Array => $"{Core}FieldType.Array({FieldTypeExpression(shape.Element!, false)}, true)",
            ShapeKind.Dictionary => $"{Core}FieldType.Map({FieldTypeExpression(shape.Element!, false)}, true)",
            ShapeKind.DataValue => $"{Core}FieldType.Any()",
            _ => $"{Core}FieldType.Object(typeof({shape.TypeName}), {Bool(isNullable)})"
        };
    }

    private static string Primitive(string kind, bool nullable)
    {
        return $"{Core}FieldType.Primitive({Core}FieldTypeKind.{kind}, {Bool(nullable)})";
    }

    private static string Bool(bool value)
    {
        return value ? "true" : "false";
    }

    private static void EmitWrite(CodeBuilder code, SchemaTypeModel model)
    {
        code.Open($"public void Write<TWriter>(ref TWriter writer, {model.FullName} value) where TWriter : struct, {Core}IFormatWriter");

        if (!model.IsValueType)
        {
            code.Open("if (value == null)").Line("writer.WriteNull();").Line("return;").Close().Line();
        }

        code.Line($"writer.BeginObject({model.Members.Count});");

        for (var i = 0; i < model.Members.Count; i++)
        {
            var member = model.Members[i];
            code.Line($"writer.WriteField(s_fields[{i}]);");
            code.Block();
            WriteValue(code, member.Shape, "value." + member.MemberName, 0);
            code.Close();
        }

        code.Line("writer.EndObject();");
        code.Close();
    }

    private static void EmitRead(CodeBuilder code, SchemaTypeModel model)
    {
        code.Open($"public {model.FullName} Read<TReader>(ref TReader reader) where TReader : struct, {Core}IFormatReader");
        code.Open($"if (reader.Peek() == {Core}TokenType.Null)").Line("reader.Skip();").Line("return default;").Close().Line();
        code.Line($"var value = new {model.FullName}();");
        code.Line("reader.ReadObjectStart();");
        code.Line();
        code.Open("while (reader.TryReadField(s_fields, out var index))");
        code.Open("switch (index)");

        for (var i = 0; i < model.Members.Count; i++)
        {
            var member = model.Members[i];
            code.Open($"case {i}:");
            ReadValue(code, member.Shape, "value." + member.MemberName, 0);
            code.Line("break;");
            code.Close();
        }

        code.Line("default:");
        code.Line("    reader.Skip();");
        code.Line("    break;");
        code.Close();
        code.Close();
        code.Line();
        code.Line("return value;");
        code.Close();
    }

    private static void EmitTranscode(CodeBuilder code, SchemaTypeModel model)
    {
        code.Open($"public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer) where TReader : struct, {Core}IFormatReader where TWriter : struct, {Core}IFormatWriter");
        code.Open($"if (reader.Peek() == {Core}TokenType.Null)").Line("reader.Skip();").Line("writer.WriteNull();").Line("return;").Close().Line();
        code.Line("reader.ReadObjectStart();");
        code.Line("writer.BeginObject(-1);");
        code.Line();
        code.Open("while (reader.TryReadField(s_fields, out var index))");
        code.Open("switch (index)");

        for (var i = 0; i < model.Members.Count; i++)
        {
            code.Open($"case {i}:");
            code.Line($"writer.WriteField(s_fields[{i}]);");
            TranscodeValue(code, model.Members[i].Shape, 0);
            code.Line("break;");
            code.Close();
        }

        code.Line("default:");
        code.Line("    reader.Skip();");
        code.Line("    break;");
        code.Close();
        code.Close();
        code.Line();
        code.Line("writer.EndObject();");
        code.Close();
    }

    private static void WriteValue(CodeBuilder code, TypeShape shape, string expression, int depth)
    {
        switch (shape.Kind)
        {
            case ShapeKind.Bool:
                code.Line($"writer.WriteBool({expression});");
                break;
            case ShapeKind.Int32:
                code.Line($"writer.WriteInt32({expression});");
                break;
            case ShapeKind.Int64:
                code.Line($"writer.WriteInt64({expression});");
                break;
            case ShapeKind.Single:
                code.Line($"writer.WriteSingle({expression});");
                break;
            case ShapeKind.Double:
                code.Line($"writer.WriteDouble({expression});");
                break;
            case ShapeKind.String:
                code.Line($"writer.WriteString({expression});");
                break;
            case ShapeKind.Enum:
                code.Line(shape.NumberType is "uint" or "long" ? $"writer.WriteInt64((long){expression});" : $"writer.WriteInt32((int){expression});");
                break;
            case ShapeKind.Nullable:
                code.Open($"if ({expression}.HasValue)");
                WriteValue(code, shape.Element!, expression + ".Value", depth + 1);
                code.Close();
                code.Open("else").Line("writer.WriteNull();").Close();
                break;
            case ShapeKind.Schema:
                code.Line($"{shape.FormatterName}.Instance.Write(ref writer, {expression});");
                break;
            case ShapeKind.DataValue:
                code.Line($"s_dataValue.Write(ref writer, {expression});");
                break;
            case ShapeKind.Registered:
                code.Line($"{Core}Formatters.Get<{shape.TypeName}>().Write(ref writer, {expression});");
                break;
            case ShapeKind.List:
            case ShapeKind.Array:
                var items = "items" + depth;
                var index = "i" + depth;
                var count = shape.Kind == ShapeKind.List ? "Count" : "Length";
                code.Line($"var {items} = {expression};");
                code.Open($"if ({items} == null)").Line("writer.WriteNull();").Close();
                code.Open("else");
                code.Line($"writer.BeginArray({items}.{count});");
                code.Open($"for (var {index} = 0; {index} < {items}.{count}; {index}++)");
                WriteValue(code, shape.Element!, $"{items}[{index}]", depth + 1);
                code.Close();
                code.Line("writer.EndArray();");
                code.Close();
                break;
            case ShapeKind.Dictionary:
                var map = "map" + depth;
                var pair = "pair" + depth;
                code.Line($"var {map} = {expression};");
                code.Open($"if ({map} == null)").Line("writer.WriteNull();").Close();
                code.Open("else");
                code.Line($"writer.BeginObject({map}.Count);");
                code.Open($"foreach (var {pair} in {map})");
                code.Line($"writer.WriteField(global::System.Text.Encoding.UTF8.GetBytes({pair}.Key));");
                WriteValue(code, shape.Element!, pair + ".Value", depth + 1);
                code.Close();
                code.Line("writer.EndObject();");
                code.Close();
                break;
        }
    }

    private static void ReadValue(CodeBuilder code, TypeShape shape, string target, int depth)
    {
        switch (shape.Kind)
        {
            case ShapeKind.Bool:
                code.Line($"{target} = reader.ReadBool();");
                break;
            case ShapeKind.Int32:
                code.Line($"{target} = {Narrow(shape.NumberType, "reader.ReadInt32()")};");
                break;
            case ShapeKind.Int64:
                code.Line($"{target} = {Narrow(shape.NumberType, "reader.ReadInt64()")};");
                break;
            case ShapeKind.Single:
                code.Line($"{target} = reader.ReadSingle();");
                break;
            case ShapeKind.Double:
                code.Line($"{target} = reader.ReadDouble();");
                break;
            case ShapeKind.String:
                code.Line($"{target} = reader.ReadString();");
                break;
            case ShapeKind.Enum:
                var raw = shape.NumberType is "uint" or "long" ? "reader.ReadInt64()" : "reader.ReadInt32()";
                code.Line($"{target} = ({shape.TypeName}){Narrow(shape.NumberType is "int" or "long" ? null : shape.NumberType, raw)};");
                break;
            case ShapeKind.Nullable:
                var inner = "inner" + depth;
                code.Open($"if (reader.Peek() == {Core}TokenType.Null)").Line("reader.Skip();").Line($"{target} = null;").Close();
                code.Open("else");
                code.Line($"{shape.Element!.TypeName} {inner} = default;");
                ReadValue(code, shape.Element!, inner, depth + 1);
                code.Line($"{target} = {inner};");
                code.Close();
                break;
            case ShapeKind.Schema:
                code.Line($"{target} = {shape.FormatterName}.Instance.Read(ref reader);");
                break;
            case ShapeKind.DataValue:
                code.Line($"{target} = s_dataValue.Read(ref reader);");
                break;
            case ShapeKind.Registered:
                code.Line($"{target} = {Core}Formatters.Get<{shape.TypeName}>().Read(ref reader);");
                break;
            case ShapeKind.List:
            case ShapeKind.Array:
                var list = "list" + depth;
                var item = "item" + depth;
                code.Open($"if (reader.Peek() == {Core}TokenType.Null)").Line("reader.Skip();").Line($"{target} = null;").Close();
                code.Open("else");
                code.Line($"var {list} = new global::System.Collections.Generic.List<{shape.Element!.TypeName}>();");
                code.Line("reader.ReadArrayStart();");
                code.Open("while (reader.TryReadNextElement())");
                code.Line($"{shape.Element!.TypeName} {item} = default;");
                ReadValue(code, shape.Element!, item, depth + 1);
                code.Line($"{list}.Add({item});");
                code.Close();
                code.Line(shape.Kind == ShapeKind.List ? $"{target} = {list};" : $"{target} = {list}.ToArray();");
                code.Close();
                break;
            case ShapeKind.Dictionary:
                var map = "map" + depth;
                var key = "key" + depth;
                var entry = "entry" + depth;
                code.Open($"if (reader.Peek() == {Core}TokenType.Null)").Line("reader.Skip();").Line($"{target} = null;").Close();
                code.Open("else");
                code.Line($"var {map} = new global::System.Collections.Generic.Dictionary<string, {shape.Element!.TypeName}>();");
                code.Line("reader.ReadObjectStart();");
                code.Open($"while (reader.TryReadFieldName(out var {key}))");
                code.Line($"{shape.Element!.TypeName} {entry} = default;");
                ReadValue(code, shape.Element!, entry, depth + 1);
                code.Line($"{map}[{key}] = {entry};");
                code.Close();
                code.Line($"{target} = {map};");
                code.Close();
                break;
        }
    }

    private static void TranscodeValue(CodeBuilder code, TypeShape shape, int depth)
    {
        switch (shape.Kind)
        {
            case ShapeKind.Bool:
                code.Line("writer.WriteBool(reader.ReadBool());");
                break;
            case ShapeKind.Int32:
                code.Line("writer.WriteInt32(reader.ReadInt32());");
                break;
            case ShapeKind.Int64:
                code.Line("writer.WriteInt64(reader.ReadInt64());");
                break;
            case ShapeKind.Single:
                code.Line("writer.WriteSingle(reader.ReadSingle());");
                break;
            case ShapeKind.Double:
                code.Line("writer.WriteDouble(reader.ReadDouble());");
                break;
            case ShapeKind.Enum:
                code.Line(shape.NumberType is "uint" or "long" ? "writer.WriteInt64(reader.ReadInt64());" : "writer.WriteInt32(reader.ReadInt32());");
                break;
            case ShapeKind.String:
                var utf8 = "utf8" + depth;
                code.Open($"if (reader.ReadStringUtf8(out var {utf8}))").Line($"writer.WriteStringUtf8({utf8});").Close();
                code.Open("else").Line("writer.WriteNull();").Close();
                break;
            case ShapeKind.Nullable:
                code.Open($"if (reader.Peek() == {Core}TokenType.Null)").Line("reader.Skip();").Line("writer.WriteNull();").Close();
                code.Open("else");
                TranscodeValue(code, shape.Element!, depth + 1);
                code.Close();
                break;
            case ShapeKind.Schema:
                code.Line($"{shape.FormatterName}.Instance.Transcode(ref reader, ref writer);");
                break;
            case ShapeKind.DataValue:
                code.Line("s_dataValue.Transcode(ref reader, ref writer);");
                break;
            case ShapeKind.Registered:
                code.Line($"{Core}Formatters.Get<{shape.TypeName}>().Transcode(ref reader, ref writer);");
                break;
            case ShapeKind.List:
            case ShapeKind.Array:
                code.Open($"if (reader.Peek() == {Core}TokenType.Null)").Line("reader.Skip();").Line("writer.WriteNull();").Close();
                code.Open("else");
                code.Line("reader.ReadArrayStart();");
                code.Line("writer.BeginArray(-1);");
                code.Open("while (reader.TryReadNextElement())");
                TranscodeValue(code, shape.Element!, depth + 1);
                code.Close();
                code.Line("writer.EndArray();");
                code.Close();
                break;
            case ShapeKind.Dictionary:
                var key = "key" + depth;
                code.Open($"if (reader.Peek() == {Core}TokenType.Null)").Line("reader.Skip();").Line("writer.WriteNull();").Close();
                code.Open("else");
                code.Line("reader.ReadObjectStart();");
                code.Line("writer.BeginObject(-1);");
                code.Open($"while (reader.TryReadFieldName(out var {key}))");
                code.Line($"writer.WriteField(global::System.Text.Encoding.UTF8.GetBytes({key}));");
                TranscodeValue(code, shape.Element!, depth + 1);
                code.Close();
                code.Line("writer.EndObject();");
                code.Close();
                break;
        }
    }

    private static string Narrow(string? numberType, string expression)
    {
        return numberType switch
        {
            "byte" => $"ToByte({expression})",
            "sbyte" => $"ToSByte({expression})",
            "short" => $"ToInt16({expression})",
            "ushort" => $"ToUInt16({expression})",
            "uint" => $"ToUInt32({expression})",
            _ => expression
        };
    }

    private static void EmitConversions(CodeBuilder code)
    {
        EmitConversion(code, "byte", "ToByte", "int");
        EmitConversion(code, "sbyte", "ToSByte", "int");
        EmitConversion(code, "short", "ToInt16", "int");
        EmitConversion(code, "ushort", "ToUInt16", "int");
        EmitConversion(code, "uint", "ToUInt32", "long");
    }

    private static void EmitConversion(CodeBuilder code, string type, string name, string source)
    {
        code.Line();
        code.Open($"private static {type} {name}({source} value)");
        code.Open($"if (value < {type}.MinValue || value > {type}.MaxValue)");
        code.Line($"throw new global::System.FormatException(value + \" does not fit in {type}.\");");
        code.Close();
        code.Line();
        code.Line($"return ({type})value;");
        code.Close();
    }

    private static bool UsesDataValue(TypeShape shape)
    {
        return shape.Kind == ShapeKind.DataValue || (shape.Element != null && UsesDataValue(shape.Element));
    }
}
