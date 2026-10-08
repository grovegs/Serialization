using Unity.Mathematics;

namespace GroveGames.Serialization.Unity
{
    public sealed class Float2Formatter : IFormatter<float2>
    {
        private static readonly FieldTable s_fields = new("x", "y");

        public void Write<TWriter>(ref TWriter writer, float2 value) where TWriter : struct, IFormatWriter
        {
            writer.BeginObject(2);
            writer.WriteField(s_fields[0]);
            writer.WriteSingle(value.x);
            writer.WriteField(s_fields[1]);
            writer.WriteSingle(value.y);
            writer.EndObject();
        }

        public float2 Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return default;
            }

            var x = 0f;
            var y = 0f;
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
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new float2(x, y);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }
}
