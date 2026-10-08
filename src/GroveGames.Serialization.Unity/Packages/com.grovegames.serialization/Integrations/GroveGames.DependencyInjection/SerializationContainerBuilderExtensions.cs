using GroveGames.DependencyInjection;

namespace GroveGames.Serialization.Unity
{
    public static class SerializationContainerBuilderExtensions
    {
        public static IContainerBuilder AddSerialization(this IContainerBuilder builder, FormatterRegistry registry)
        {
            builder.AddSingleton(registry);
            builder.AddSingleton<JsonSerializer>();
            builder.AddSingleton<MessagePackSerializer>();
            return builder.AddSingleton<CsvSerializer>();
        }
    }
}
