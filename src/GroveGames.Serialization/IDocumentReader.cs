namespace GroveGames.Serialization;

internal interface IDocumentReader : IFormatReader
{
    public int ReadEnvelope();
    public void EndEnvelope();
    public void EndDocument();
}
