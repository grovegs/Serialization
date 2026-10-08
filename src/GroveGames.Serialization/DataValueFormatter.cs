namespace GroveGames.Serialization;

public sealed class DataValueFormatter : IFormatter<DataValue>
{
    public void Write<TWriter>(ref TWriter writer, DataValue value, FormatterRegistry registry)
        where TWriter : struct, IFormatWriter
    {
        switch (value.Kind)
        {
            case DataKind.Bool:
                writer.WriteBool(value.AsBool);
                break;
            case DataKind.Integer:
                writer.WriteInt64(value.AsInt64);
                break;
            case DataKind.Float:
                writer.WriteDouble(value.AsDouble);
                break;
            case DataKind.String:
            case DataKind.Text:
                writer.WriteString(value.AsString);
                break;
            case DataKind.Array:
                var array = value.AsArray;
                writer.BeginArray(array.Count);

                for (var i = 0; i < array.Count; i++)
                {
                    Write(ref writer, array[i], registry);
                }

                writer.EndArray();
                break;
            case DataKind.Object:
                var obj = value.AsObject;
                writer.BeginObject(obj.Count);

                for (var i = 0; i < obj.Count; i++)
                {
                    var field = obj[i];
                    writer.WriteField(System.Text.Encoding.UTF8.GetBytes(field.Name));
                    Write(ref writer, field.Value, registry);
                }

                writer.EndObject();
                break;
            default:
                writer.WriteNull();
                break;
        }
    }

    public DataValue Read<TReader>(ref TReader reader, FormatterRegistry registry)
        where TReader : struct, IFormatReader
    {
        return DataValue.Read(ref reader);
    }

    public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer, FormatterRegistry registry)
        where TReader : struct, IFormatReader
        where TWriter : struct, IFormatWriter
    {
        Write(ref writer, DataValue.Read(ref reader), registry);
    }
}
