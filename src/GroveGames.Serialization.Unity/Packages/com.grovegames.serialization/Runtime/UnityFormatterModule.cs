using GroveGames.Serialization.Unity;
using UnityEngine;

[assembly: GroveGames.Serialization.FormatterModule(typeof(UnityFormatterModule))]

namespace GroveGames.Serialization.Unity
{
    [GroveGames.Serialization.Preserve]
    internal sealed class UnityFormatterModule : IFormatterModule
    {
        [GroveGames.Serialization.Preserve]
        public UnityFormatterModule()
        {
        }

        public void Register(FormatterRegistrar registrar)
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void Load()
        {
            Formatters.Load(typeof(UnityFormatterModule).Assembly);
        }
    }
}
