namespace GroveGames.Serialization.Unity
{
    public static class MathematicsSerializerRegistryBuilderExtensions
    {
        public static SerializerRegistryBuilder AddMathematicsFormatters(this SerializerRegistryBuilder builder)
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
