using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace GroveGames.Serialization.Unity.Editor.Tests
{
    public sealed class UnityFormatterRegistrationTests
    {
        [Test]
        public void Formatters_UnityTypes_AreRegistered()
        {
            Assert.IsTrue(Formatters.TryGet<Vector3>(out _));
            Assert.IsTrue(Formatters.TryGet<Bounds>(out _));
            Assert.IsTrue(Formatters.TryGet<System.Collections.Generic.List<Vector3>>(out _));
        }

        [Test]
        public void UnityFormatters_Bounds_RoundTripsThroughJsonAndMessagePack()
        {
            var bounds = new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
            var json = new JsonSerializer();
            var messagePack = new MessagePackSerializer();

            var fromJson = json.Deserialize<Bounds>(json.Serialize(bounds));
            var fromMessagePack = messagePack.Deserialize<Bounds>(messagePack.Serialize(bounds));

            Assert.AreEqual(bounds, fromJson);
            Assert.AreEqual(bounds, fromMessagePack);
        }

        [Test]
        public void UnityFormatters_Vector3_WritesNamedComponents()
        {
            var json = new JsonSerializer();

            var text = Encoding.UTF8.GetString(json.Serialize(new Vector3(1, 2, 3)));

            Assert.AreEqual("{\"x\":1,\"y\":2,\"z\":3}", text);
        }

        [Test]
        public void UnityFormatters_UnknownAndMissingComponents_SkipsAndDefaults()
        {
            var json = new JsonSerializer();

            var value = json.Deserialize<Vector3>(Encoding.UTF8.GetBytes("{\"w\":9,\"y\":2}"));

            Assert.AreEqual(new Vector3(0, 2, 0), value);
        }

        [Test]
        public void UnityFormatters_Color32_RoundTrips()
        {
            var messagePack = new MessagePackSerializer();
            var color = new Color32(10, 20, 30, 255);

            var result = messagePack.Deserialize<Color32>(messagePack.Serialize(color));

            Assert.AreEqual(color, result);
        }
    }
}
