namespace GroveGames.Serialization;

public interface IConverter
{
    public byte[] Convert<T>(ReadOnlyMemory<byte> data);
    public void Convert<T>(ReadOnlyMemory<byte> data, ByteBuffer output);
}
