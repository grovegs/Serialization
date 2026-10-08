using UnityEngine;

namespace GroveGames.Serialization.Unity
{
    internal static class UnityFormatterRegistration
    {
        private static bool s_registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        internal static void Register()
        {
            if (s_registered)
            {
                return;
            }

            s_registered = true;
            Formatters.Register(Configure);
        }

        private static void Configure(FormatterRegistrar registrar)
        {
            registrar.AddFormatter(new Vector2Formatter());
            registrar.AddFormatter(new ListFormatter<Vector2>());
            registrar.AddFormatter(new Vector3Formatter());
            registrar.AddFormatter(new ListFormatter<Vector3>());
            registrar.AddFormatter(new Vector4Formatter());
            registrar.AddFormatter(new ListFormatter<Vector4>());
            registrar.AddFormatter(new Vector2IntFormatter());
            registrar.AddFormatter(new ListFormatter<Vector2Int>());
            registrar.AddFormatter(new Vector3IntFormatter());
            registrar.AddFormatter(new ListFormatter<Vector3Int>());
            registrar.AddFormatter(new QuaternionFormatter());
            registrar.AddFormatter(new ListFormatter<Quaternion>());
            registrar.AddFormatter(new ColorFormatter());
            registrar.AddFormatter(new ListFormatter<Color>());
            registrar.AddFormatter(new Color32Formatter());
            registrar.AddFormatter(new ListFormatter<Color32>());
            registrar.AddFormatter(new RectFormatter());
            registrar.AddFormatter(new ListFormatter<Rect>());
            registrar.AddFormatter(new BoundsFormatter());
            registrar.AddFormatter(new ListFormatter<Bounds>());
        }
    }
}
