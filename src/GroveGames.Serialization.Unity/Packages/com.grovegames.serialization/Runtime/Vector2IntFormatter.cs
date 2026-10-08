using UnityEngine;

namespace GroveGames.Serialization.Unity
{
    public sealed class Vector2IntFormatter : IFormatter<Vector2Int>
    {
        private static readonly FieldTable s_fields = new("x", "y");

        public void Write<TWriter>(ref TWriter writer, Vector2Int value) where TWriter : struct, IFormatWriter
        {
            writer.BeginObject(2);
            writer.WriteField(s_fields[0]);
            writer.WriteInt32(value.x);
            writer.WriteField(s_fields[1]);
            writer.WriteInt32(value.y);
            writer.EndObject();
        }

        public Vector2Int Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return default;
            }

            var x = 0;
            var y = 0;
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
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new Vector2Int(x, y);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }
}
