namespace GroveGames.Serialization;

public interface IFormatWriter
{
    public void BeginEnvelope(int version);
    public void EndEnvelope();
    public void BeginObject(int fieldCount);
    public void WriteField(byte[] utf8Name);
    public void EndObject();
    public void BeginArray(int count);
    public void EndArray();
    public void WriteInt32(int value);
    public void WriteInt64(long value);
    public void WriteSingle(float value);
    public void WriteDouble(double value);
    public void WriteBool(bool value);
    public void WriteString(string? value);
    public void WriteStringUtf8(ReadOnlySpan<byte> utf8);
    public void WriteNull();
}
