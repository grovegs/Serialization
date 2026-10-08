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
            builder.AddMathematicsFormatters();
        }
    }
}
