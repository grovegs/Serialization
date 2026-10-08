namespace GroveGames.Serialization;

internal interface IDocumentWriter : IFormatWriter
{
    public void BeginEnvelope(int version);
    public void EndEnvelope();
}
