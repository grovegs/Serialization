using NUnit.Framework;
using UnityEngine;

namespace GroveGames.Serialization.Unity.Editor.Tests
{
    public sealed class GeneratedSchemaTests
    {
        [Test]
        public void GeneratedFormatter_UnityMember_RoundTrips()
        {
            var serializer = new MessagePackSerializer();
            var value = new GeneratedUnitySample { Name = "spawn", Position = new Vector3(1, 2, 3), Count = 4 };

            var result = serializer.Deserialize<GeneratedUnitySample>(serializer.Serialize(value));

            Assert.AreEqual("spawn", result.Name);
            Assert.AreEqual(new Vector3(1, 2, 3), result.Position);
            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(FieldTypeKind.Object, Formatters.GetSchema<GeneratedUnitySample>().Fields[1].Type.Kind);
        }
    }

    [Schema(version: 2)]
    public sealed class GeneratedUnitySample
    {
        public string Name;
        public Vector3 Position;
        public int Count;
    }
}
