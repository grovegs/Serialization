namespace GroveGames.Serialization.Unity
{
    public static class MathematicsFormatterRegistryBuilderExtensions
    {
        public static FormatterRegistryBuilder AddMathematicsFormatters(this FormatterRegistryBuilder builder)
        {
            return builder
                .AddFormatter(new Float2Formatter())
                .AddFormatter(new Float3Formatter())
                .AddFormatter(new Float4Formatter())
                .AddFormatter(new Int2Formatter())
                .AddFormatter(new Int3Formatter())
                .AddFormatter(new MathematicsQuaternionFormatter());
        }
    }
}
