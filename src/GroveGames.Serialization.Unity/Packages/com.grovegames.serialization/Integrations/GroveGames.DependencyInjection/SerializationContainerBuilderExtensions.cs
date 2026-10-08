using GroveGames.DependencyInjection;

namespace GroveGames.Serialization.Unity
{
    public static class SerializationContainerBuilderExtensions
    {
        public static IContainerBuilder AddSerialization(this IContainerBuilder builder)
        {
            builder.AddSingleton(_ => new JsonSerializer());
            builder.AddSingleton(_ => new MessagePackSerializer());
            return builder.AddSingleton(_ => new CsvSerializer());
        }
    }
}
