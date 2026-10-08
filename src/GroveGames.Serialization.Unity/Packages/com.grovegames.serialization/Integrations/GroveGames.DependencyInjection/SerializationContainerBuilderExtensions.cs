using GroveGames.DependencyInjection;

namespace GroveGames.Serialization.Unity
{
    public static class SerializationContainerBuilderExtensions
    {
        public static IContainerBuilder AddSerialization(this IContainerBuilder builder)
        {
            return builder.AddSerialization(FormatterRegistry.Default);
        }

        public static IContainerBuilder AddSerialization(this IContainerBuilder builder, FormatterRegistry registry)
        {
            builder.AddSingleton(registry);
            builder.AddSingleton(_ => new JsonSerializer(registry));
            builder.AddSingleton(_ => new MessagePackSerializer(registry));
            return builder.AddSingleton(_ => new CsvSerializer(registry));
        }
    }
}
