using UnityEngine;

namespace GroveGames.Serialization.Unity
{
    public sealed class Color32Formatter : IFormatter<Color32>
    {
        private static readonly FieldTable s_fields = new("r", "g", "b", "a");

        public void Write<TWriter>(ref TWriter writer, Color32 value, SerializerRegistry registry) where TWriter : struct, IFormatWriter
        {
            writer.BeginObject(4);
            writer.WriteField(s_fields[0]);
            writer.WriteInt32(value.r);
            writer.WriteField(s_fields[1]);
            writer.WriteInt32(value.g);
            writer.WriteField(s_fields[2]);
            writer.WriteInt32(value.b);
            writer.WriteField(s_fields[3]);
            writer.WriteInt32(value.a);
            writer.EndObject();
        }

        public Color32 Read<TReader>(ref TReader reader, SerializerRegistry registry) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return default;
            }

            var r = 0;
            var g = 0;
            var b = 0;
            var a = 0;
            reader.ReadObjectStart();

            while (reader.TryReadField(s_fields, out var index))
            {
                switch (index)
                {
                    case 0:
                        r = reader.ReadInt32();
                        break;
                    case 1:
                        g = reader.ReadInt32();
                        break;
                    case 2:
                        b = reader.ReadInt32();
                        break;
                    case 3:
                        a = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer, SerializerRegistry registry)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader, registry), registry);
        }
    }
}
