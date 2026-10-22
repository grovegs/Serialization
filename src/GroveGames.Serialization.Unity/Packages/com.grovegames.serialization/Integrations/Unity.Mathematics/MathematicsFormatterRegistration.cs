using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace GroveGames.Serialization.Unity
{
    [Preserve]
    internal static class MathematicsFormatterRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [Preserve]
        internal static void Register()
        {
            Formatters.Register(Configure);
        }

        private static void Configure(FormatterRegistrar registrar)
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
    }
}
