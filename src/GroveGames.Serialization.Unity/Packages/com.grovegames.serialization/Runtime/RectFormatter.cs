using UnityEngine;

namespace GroveGames.Serialization.Unity
{
    public sealed class RectFormatter : IFormatter<Rect>
    {
        private static readonly FieldTable s_fields = new("x", "y", "width", "height");

        public void Write<TWriter>(ref TWriter writer, Rect value, SerializerRegistry registry) where TWriter : struct, IFormatWriter
        {
            writer.BeginObject(4);
            writer.WriteField(s_fields[0]);
            writer.WriteSingle(value.x);
            writer.WriteField(s_fields[1]);
            writer.WriteSingle(value.y);
            writer.WriteField(s_fields[2]);
            writer.WriteSingle(value.width);
            writer.WriteField(s_fields[3]);
            writer.WriteSingle(value.height);
            writer.EndObject();
        }

        public Rect Read<TReader>(ref TReader reader, SerializerRegistry registry) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return default;
            }

            var x = 0f;
            var y = 0f;
            var width = 0f;
            var height = 0f;
            reader.ReadObjectStart();

            while (reader.TryReadField(s_fields, out var index))
            {
                switch (index)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        width = reader.ReadSingle();
                        break;
                    case 3:
                        height = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new Rect(x, y, width, height);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer, SerializerRegistry registry)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader, registry), registry);
        }
    }
}
