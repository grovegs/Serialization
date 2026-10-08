using System.Buffers;

namespace GroveGames.Serialization;

public interface IVersionedSerializer
{
    public SerializerRegistry Registry { get; }
    public void Serialize<T>(T? value, IBufferWriter<byte> output);
    public T? Deserialize<T>(ReadOnlyMemory<byte> data);
}
