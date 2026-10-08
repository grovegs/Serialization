namespace GroveGames.Serialization;

public interface ISerializer
{
    public SerializerRegistry Registry { get; }
    public byte[] Serialize<T>(T? value);
    public void Serialize<T>(T? value, ByteBuffer output);
    public void Serialize<T>(T? value, Stream output);
    public T? Deserialize<T>(ReadOnlyMemory<byte> data);
    public T? Deserialize<T>(Stream input);
}
