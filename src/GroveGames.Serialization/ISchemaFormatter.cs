namespace GroveGames.Serialization;

public interface ISchemaFormatter<T> : IFormatter<T>
{
    public TypeSchema Schema { get; }
}
