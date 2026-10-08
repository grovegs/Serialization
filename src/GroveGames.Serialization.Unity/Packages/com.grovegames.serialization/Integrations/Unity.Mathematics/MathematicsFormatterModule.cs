using GroveGames.Serialization.Unity;

[assembly: GroveGames.Serialization.FormatterModule(typeof(MathematicsFormatterModule))]

namespace GroveGames.Serialization.Unity
{
    [Preserve]
    internal sealed class MathematicsFormatterModule : IFormatterModule
    {
        [Preserve]
        public MathematicsFormatterModule()
        {
        }

        public void Register(FormatterRegistryBuilder builder)
        {
            builder.AddFormatter(new Float2Formatter());
            builder.AddFormatter(new Float3Formatter());
            builder.AddFormatter(new Float4Formatter());
            builder.AddFormatter(new Int2Formatter());
            builder.AddFormatter(new Int3Formatter());
            builder.AddFormatter(new MathematicsQuaternionFormatter());
        }
    }
}
