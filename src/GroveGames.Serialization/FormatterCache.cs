namespace GroveGames.Serialization;

internal static class FormatterCache<T>
{
    public static TypeRegistration<T>? Registration;
    public static bool AssembliesInitialized;
}
