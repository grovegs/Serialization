using System.Buffers;

namespace GroveGames.Serialization;

public interface IConverter
{
    public void Convert<T>(ReadOnlyMemory<byte> data, IBufferWriter<byte> output);
}
