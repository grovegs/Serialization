using UnityEngine;

namespace GroveGames.Serialization.Unity
{
    public sealed class Vector3IntFormatter : IFormatter<Vector3Int>
    {
        private static readonly FieldTable s_fields = new("x", "y", "z");

        public void Write<TWriter>(ref TWriter writer, Vector3Int value, FormatterRegistry registry) where TWriter : struct, IFormatWriter
        {
            writer.BeginObject(3);
            writer.WriteField(s_fields[0]);
            writer.WriteInt32(value.x);
            writer.WriteField(s_fields[1]);
            writer.WriteInt32(value.y);
            writer.WriteField(s_fields[2]);
            writer.WriteInt32(value.z);
            writer.EndObject();
        }

        public Vector3Int Read<TReader>(ref TReader reader, FormatterRegistry registry) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return default;
            }

            var x = 0;
            var y = 0;
            var z = 0;
            reader.ReadObjectStart();

            while (reader.TryReadField(s_fields, out var index))
            {
                switch (index)
                {
                    case 0:
                        x = reader.ReadInt32();
                        break;
                    case 1:
                        y = reader.ReadInt32();
                        break;
                    case 2:
                        z = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new Vector3Int(x, y, z);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer, FormatterRegistry registry)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader, registry), registry);
        }
    }
}
