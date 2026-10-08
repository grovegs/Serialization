using GroveGames.Serialization.Unity;
using UnityEngine;

[assembly: GroveGames.Serialization.FormatterModule(typeof(MathematicsFormatterModule))]

namespace GroveGames.Serialization.Unity
{
    [GroveGames.Serialization.Preserve]
    internal sealed class MathematicsFormatterModule : IFormatterModule
    {
        [GroveGames.Serialization.Preserve]
        public MathematicsFormatterModule()
        {
        }

        public void Register(FormatterRegistrar registrar)
        {
            registrar.AddFormatter(new Float2Formatter());
            registrar.AddFormatter(new ListFormatter<global::Unity.Mathematics.float2>());
            registrar.AddFormatter(new Float3Formatter());
            registrar.AddFormatter(new ListFormatter<global::Unity.Mathematics.float3>());
            registrar.AddFormatter(new Float4Formatter());
            registrar.AddFormatter(new ListFormatter<global::Unity.Mathematics.float4>());
            registrar.AddFormatter(new Int2Formatter());
            registrar.AddFormatter(new ListFormatter<global::Unity.Mathematics.int2>());
            registrar.AddFormatter(new Int3Formatter());
            registrar.AddFormatter(new ListFormatter<global::Unity.Mathematics.int3>());
            registrar.AddFormatter(new MathematicsQuaternionFormatter());
            registrar.AddFormatter(new ListFormatter<global::Unity.Mathematics.quaternion>());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void Load()
        {
            Formatters.Load(typeof(MathematicsFormatterModule).Assembly);
        }
    }
}
