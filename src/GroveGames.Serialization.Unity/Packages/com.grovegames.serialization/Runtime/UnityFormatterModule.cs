using GroveGames.Serialization.Unity;

[assembly: GroveGames.Serialization.FormatterModule(typeof(UnityFormatterModule))]

namespace GroveGames.Serialization.Unity
{
    [Preserve]
    internal sealed class UnityFormatterModule : IFormatterModule
    {
        [Preserve]
        public UnityFormatterModule()
        {
        }

        public void Register(FormatterRegistryBuilder builder)
        {
            builder.AddUnityFormatters();
        }
    }
}
