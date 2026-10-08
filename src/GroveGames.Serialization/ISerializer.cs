using System.Buffers;

namespace GroveGames.Serialization;

public interface ISerializer
{
    public void Serialize<T>(T? value, IBufferWriter<byte> output);
    public T? Deserialize<T>(ReadOnlyMemory<byte> data);
}
