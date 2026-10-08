namespace GroveGames.Serialization;

public interface IFormatReader
{
    public TokenType Peek();
    public void ReadObjectStart();
    public bool TryReadField(FieldTable fields, out int index);
    public bool TryReadFieldName(out string name);
    public void ReadArrayStart();
    public bool TryReadNextElement();
    public int ReadInt32();
    public long ReadInt64();
    public float ReadSingle();
    public double ReadDouble();
    public bool ReadBool();
    public string? ReadString();
    public bool ReadStringUtf8(out ReadOnlySpan<byte> utf8);
    public void Skip();
}
