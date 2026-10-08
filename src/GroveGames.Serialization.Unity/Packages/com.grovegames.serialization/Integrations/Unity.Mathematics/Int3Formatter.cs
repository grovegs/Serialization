using Unity.Mathematics;

namespace GroveGames.Serialization.Unity
{
    public sealed class Int3Formatter : IFormatter<int3>
    {
        private static readonly FieldTable s_fields = new("x", "y", "z");

        public void Write<TWriter>(ref TWriter writer, int3 value) where TWriter : struct, IFormatWriter
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

        public int3 Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
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

            return new int3(x, y, z);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }
}
