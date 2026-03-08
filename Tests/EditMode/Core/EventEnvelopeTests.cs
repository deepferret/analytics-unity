using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Core")]
    public class EventEnvelopeTests
    {
        private JsonSerializerSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        // --- Serialization ---

        [Test]
        public void Serialize_AllFieldsPopulated_ProducesValidJson()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            Assert.That(() => JObject.Parse(json), Throws.Nothing);
        }

        [Test]
        public void Serialize_WithNullUserId_OmitsUserIdField()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);
            Assert.That(jObj.ContainsKey("userId"), Is.False);
        }

        [Test]
        public void Serialize_WithNullEvent_OmitsEventField()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);
            Assert.That(jObj.ContainsKey("event"), Is.False);
        }

        [Test]
        public void Serialize_WithNullProperties_OmitsPropertiesField()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);
            Assert.That(jObj.ContainsKey("properties"), Is.False);
        }

        [Test]
        public void Serialize_RequiredFields_AlwaysPresent()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("eventId"), Is.True, "eventId missing");
            Assert.That(jObj.ContainsKey("type"), Is.True, "type missing");
            Assert.That(jObj.ContainsKey("timestamp"), Is.True, "timestamp missing");
            Assert.That(jObj.ContainsKey("anonymousId"), Is.True, "anonymousId missing");
            Assert.That(jObj.ContainsKey("context"), Is.True, "context missing");
        }

        [Test]
        public void Serialize_JsonPropertyNames_AreCamelCase()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);

            foreach (var prop in jObj.Properties())
            {
                Assert.That(char.IsLower(prop.Name[0]), Is.True,
                    $"Property '{prop.Name}' is not camelCase");
            }
        }

        [Test]
        public void Deserialize_ValidJson_ReconstructsEnvelope()
        {
            var original = EventTestFactory.CreateFullEnvelope();
            var json = JsonConvert.SerializeObject(original, _settings);
            var restored = JsonConvert.DeserializeObject<EventEnvelope>(json);

            Assert.That(restored.EventId, Is.EqualTo(original.EventId));
            Assert.That(restored.Type, Is.EqualTo(original.Type));
            Assert.That(restored.Timestamp, Is.EqualTo(original.Timestamp));
            Assert.That(restored.AnonymousId, Is.EqualTo(original.AnonymousId));
            Assert.That(restored.UserId, Is.EqualTo(original.UserId));
            Assert.That(restored.Event, Is.EqualTo(original.Event));
        }

        // --- Timestamp ---

        [Test]
        public void Timestamp_WhenSet_IsIso8601UtcFormat()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            Assert.That(
                DateTimeOffset.TryParse(envelope.Timestamp, out var dto), Is.True,
                "Timestamp should be parseable as ISO 8601");
            Assert.That(dto.Offset, Is.EqualTo(TimeSpan.Zero),
                "Timestamp should be UTC");
        }

        // --- Reset (Object Pooling) ---

        [Test]
        public void Reset_AfterPopulation_ClearsAllFields()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            envelope.Reset();

            Assert.That(envelope.EventId, Is.Null);
            Assert.That(envelope.Type, Is.Null);
            Assert.That(envelope.Timestamp, Is.Null);
            Assert.That(envelope.AnonymousId, Is.Null);
            Assert.That(envelope.UserId, Is.Null);
            Assert.That(envelope.Event, Is.Null);
            Assert.That(envelope.Properties, Is.Null);
            Assert.That(envelope.Context, Is.Null);
        }

        [Test]
        public void Reset_CalledTwice_NoException()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            Assert.That(() =>
            {
                envelope.Reset();
                envelope.Reset();
            }, Throws.Nothing);
        }

        // --- Edge Cases ---

        [Test]
        public void Serialize_WithEmptyProperties_IncludesEmptyObject()
        {
            var envelope = EventTestFactory.CreateEnvelopeWithEmptyProperties();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("properties"), Is.True,
                "Empty properties should be serialized (not omitted like null)");
            Assert.That(jObj["properties"].Type, Is.EqualTo(JTokenType.Object));
            Assert.That(jObj["properties"].HasValues, Is.False);
        }

        [Test]
        public void Serialize_PropertiesWithNestedObject_SerializesCorrectly()
        {
            var envelope = EventTestFactory.CreateEnvelopeWithNestedProperties();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);

            var props = jObj["properties"];
            Assert.That(props, Is.Not.Null);
            Assert.That(props["simple"]?.Value<string>(), Is.EqualTo("value"));
            Assert.That(props["number"]?.Value<int>(), Is.EqualTo(42));
            Assert.That(props["nested"]?["inner_key"]?.Value<string>(),
                Is.EqualTo("inner_value"));
        }
    }
}
