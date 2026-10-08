namespace GroveGames.Serialization.Unity
{
    public static class UnityFormatterRegistryBuilderExtensions
    {
        public static FormatterRegistryBuilder AddUnityFormatters(this FormatterRegistryBuilder builder)
        {
            return builder
                .AddFormatter(new Vector2Formatter())
                .AddFormatter(new Vector3Formatter())
                .AddFormatter(new Vector4Formatter())
                .AddFormatter(new Vector2IntFormatter())
                .AddFormatter(new Vector3IntFormatter())
                .AddFormatter(new QuaternionFormatter())
                .AddFormatter(new ColorFormatter())
                .AddFormatter(new Color32Formatter())
                .AddFormatter(new RectFormatter())
                .AddFormatter(new BoundsFormatter());
        }
    }
}
