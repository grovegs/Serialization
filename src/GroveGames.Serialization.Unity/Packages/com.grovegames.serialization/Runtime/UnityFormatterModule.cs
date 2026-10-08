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
            builder.AddFormatter(new Vector2Formatter());
            builder.AddFormatter(new Vector3Formatter());
            builder.AddFormatter(new Vector4Formatter());
            builder.AddFormatter(new Vector2IntFormatter());
            builder.AddFormatter(new Vector3IntFormatter());
            builder.AddFormatter(new QuaternionFormatter());
            builder.AddFormatter(new ColorFormatter());
            builder.AddFormatter(new Color32Formatter());
            builder.AddFormatter(new RectFormatter());
            builder.AddFormatter(new BoundsFormatter());
        }
    }
}
