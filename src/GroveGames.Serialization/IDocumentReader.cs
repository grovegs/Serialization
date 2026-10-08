namespace GroveGames.Serialization;

internal interface IDocumentReader : IFormatReader
{
    public bool TryReadEnvelope(out int version);
    public void EndEnvelope();
    public void EndDocument();
}
