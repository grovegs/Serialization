namespace GroveGames.Serialization;

internal static class Envelope
{
    public const int VersionField = 0;
    public const int DataField = 1;

    public static FieldTable Fields { get; } = new("$v", "data");
}
