using UnityEngine;

namespace GroveGames.Serialization.Unity
{
    public sealed class BoundsFormatter : IFormatter<Bounds>
    {
        private static readonly FieldTable s_fields = new("center", "size");

        private readonly Vector3Formatter _vector3Formatter;

        public BoundsFormatter()
        {
            _vector3Formatter = new Vector3Formatter();
        }

        public void Write<TWriter>(ref TWriter writer, Bounds value) where TWriter : struct, IFormatWriter
        {
            writer.BeginObject(2);
            writer.WriteField(s_fields[0]);
            _vector3Formatter.Write(ref writer, value.center);
            writer.WriteField(s_fields[1]);
            _vector3Formatter.Write(ref writer, value.size);
            writer.EndObject();
        }

        public Bounds Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return default;
            }

            var center = Vector3.zero;
            var size = Vector3.zero;
            reader.ReadObjectStart();

            while (reader.TryReadField(s_fields, out var index))
            {
                switch (index)
                {
                    case 0:
                        center = _vector3Formatter.Read(ref reader);
                        break;
                    case 1:
                        size = _vector3Formatter.Read(ref reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new Bounds(center, size);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }
}
