using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace GroveGames.Serialization.Unity.Editor.Tests
{
    public sealed class UnityFormatterRegistryBuilderExtensionsTests
    {
        [Test]
        public void AddUnityFormatters_Bounds_RoundTripsThroughJsonAndMessagePack()
        {
            var registry = new FormatterRegistryBuilder().AddUnityFormatters().Build();
            var bounds = new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
            var json = new JsonSerializer(registry);
            var messagePack = new MessagePackSerializer(registry);

            var fromJson = json.Deserialize<Bounds>(json.Serialize(bounds));
            var fromMessagePack = messagePack.Deserialize<Bounds>(messagePack.Serialize(bounds));

            Assert.AreEqual(bounds, fromJson);
            Assert.AreEqual(bounds, fromMessagePack);
        }

        [Test]
        public void AddUnityFormatters_Vector3_WritesNamedComponents()
        {
            var registry = new FormatterRegistryBuilder().AddUnityFormatters().Build();
            var json = new JsonSerializer(registry);

            var text = Encoding.UTF8.GetString(json.Serialize(new Vector3(1, 2, 3)));

            Assert.AreEqual("{\"x\":1,\"y\":2,\"z\":3}", text);
        }

        [Test]
        public void AddUnityFormatters_UnknownAndMissingComponents_SkipsAndDefaults()
        {
            var registry = new FormatterRegistryBuilder().AddUnityFormatters().Build();
            var json = new JsonSerializer(registry);

            var value = json.Deserialize<Vector3>(Encoding.UTF8.GetBytes("{\"w\":9,\"y\":2}"));

            Assert.AreEqual(new Vector3(0, 2, 0), value);
        }

        [Test]
        public void AddUnityFormatters_Color32_RoundTrips()
        {
            var registry = new FormatterRegistryBuilder().AddUnityFormatters().Build();
            var messagePack = new MessagePackSerializer(registry);
            var color = new Color32(10, 20, 30, 255);

            var result = messagePack.Deserialize<Color32>(messagePack.Serialize(color));

            Assert.AreEqual(color, result);
        }
    }
}
