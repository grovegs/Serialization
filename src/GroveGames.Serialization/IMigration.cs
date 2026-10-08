namespace GroveGames.Serialization;

public interface IMigration<T>
{
    public int FromVersion { get; }
    public void Apply(DataNode root);
}
