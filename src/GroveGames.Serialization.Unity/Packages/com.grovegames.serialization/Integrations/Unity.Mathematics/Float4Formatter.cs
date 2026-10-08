using Unity.Mathematics;

namespace GroveGames.Serialization.Unity
{
    public sealed class Float4Formatter : IFormatter<float4>
    {
        private static readonly FieldTable s_fields = new("x", "y", "z", "w");

        public void Write<TWriter>(ref TWriter writer, float4 value) where TWriter : struct, IFormatWriter
        {
            writer.BeginObject(4);
            writer.WriteField(s_fields[0]);
            writer.WriteSingle(value.x);
            writer.WriteField(s_fields[1]);
            writer.WriteSingle(value.y);
            writer.WriteField(s_fields[2]);
            writer.WriteSingle(value.z);
            writer.WriteField(s_fields[3]);
            writer.WriteSingle(value.w);
            writer.EndObject();
        }

        public float4 Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
        {
            if (reader.Peek() == TokenType.Null)
            {
                reader.Skip();
                return default;
            }

            var x = 0f;
            var y = 0f;
            var z = 0f;
            var w = 0f;
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
                        z = reader.ReadSingle();
                        break;
                    case 3:
                        w = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new float4(x, y, z, w);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }
}
