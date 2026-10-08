using Unity.Mathematics;

namespace GroveGames.Serialization.Unity
{
    public sealed class Int2Formatter : IFormatter<int2>
    {
        private static readonly FieldTable s_fields = new("x", "y");

        public void Write<TWriter>(ref TWriter writer, int2 value) where TWriter : struct, IFormatWriter
        {
            writer.BeginObject(2);
            writer.WriteField(s_fields[0]);
            writer.WriteInt32(value.x);
            writer.WriteField(s_fields[1]);
            writer.WriteInt32(value.y);
            writer.EndObject();
        }

        public int2 Read<TReader>(ref TReader reader) where TReader : struct, IFormatReader
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

            return new int2(x, y);
        }

        public void Transcode<TReader, TWriter>(ref TReader reader, ref TWriter writer)
            where TReader : struct, IFormatReader
            where TWriter : struct, IFormatWriter
        {
            Write(ref writer, Read(ref reader));
        }
    }
}
