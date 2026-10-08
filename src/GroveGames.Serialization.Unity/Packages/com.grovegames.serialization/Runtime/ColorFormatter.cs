using UnityEngine;

namespace GroveGames.Serialization.Unity
{
    public sealed class ColorFormatter : IFormatter<Color>
    {
        private static readonly FieldTable s_fields = new("r", "g", "b", "a");

        public void Write<TWriter>(ref TWriter writer, Color value) where TWriter : struct, IFormatWriter
        {
            writer.BeginObject(4);
            writer.WriteField(s_fields[0]);
            writer.WriteSingle(value.r);
            writer.WriteField(s_fields[1]);
            writer.WriteSingle(value.g);
            writer.WriteField(s_fields[2]);
            writer.WriteSingle(value.b);
            writer.WriteField(s_fields[3]);
            writer.WriteSingle(value.a);
            writer.EndObject();
        }

        public Color Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return default;
            }

            var r = 0f;
            var g = 0f;
            var b = 0f;
            var a = 0f;
            reader.ReadObjectStart();

            while (reader.TryReadField(s_fields, out var index))
            {
                switch (index)
                {
                    case 0:
                        r = reader.ReadSingle();
                        break;
                    case 1:
                        g = reader.ReadSingle();
                        break;
                    case 2:
                        b = reader.ReadSingle();
                        break;
                    case 3:
                        a = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new Color(r, g, b, a);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }
}
