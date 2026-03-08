using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    /// <summary>
    /// Integration and edge case tests for the event data model.
    /// Tests full EventEnvelope with nested EventContext round-trips,
    /// batch payload structure, and stress/edge scenarios.
    /// </summary>
    [TestFixture]
    [Category("Core")]
    public class EventIntegrationTests
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

        // --- Full Round-trip ---

        [Test]
        public void FullEnvelope_RoundTrip_PreservesAllFields()
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
            Assert.That(restored.Context, Is.Not.Null);
            Assert.That(restored.Context.Library, Is.Not.Null);
            Assert.That(restored.Context.Library.Name,
                Is.EqualTo(original.Context.Library.Name));
            Assert.That(restored.Context.Library.Version,
                Is.EqualTo(original.Context.Library.Version));
            Assert.That(restored.Context.Device.Type,
                Is.EqualTo(original.Context.Device.Type));
            Assert.That(restored.Context.Game.Engine,
                Is.EqualTo(original.Context.Game.Engine));
        }

        [Test]
        public void MinimalEnvelope_RoundTrip_PreservesRequiredFields()
        {
            var original = EventTestFactory.CreateMinimalEnvelope();
            var json = JsonConvert.SerializeObject(original, _settings);
            var restored = JsonConvert.DeserializeObject<EventEnvelope>(json);

            Assert.That(restored.EventId, Is.EqualTo(original.EventId));
            Assert.That(restored.UserId, Is.Null);
            Assert.That(restored.Event, Is.Null);
            Assert.That(restored.Properties, Is.Null);
            Assert.That(restored.Context, Is.Not.Null);
            Assert.That(restored.Context.Device, Is.Null);
            Assert.That(restored.Context.Game, Is.Null);
        }

        // --- Batch Payload ---

        [Test]
        public void BatchPayload_Serialize_ProducesCorrectStructure()
        {
            var batch = new List<EventEnvelope>
            {
                EventTestFactory.CreateFullEnvelope(),
                EventTestFactory.CreateMinimalEnvelope()
            };

            var payload = new
            {
                batch = batch,
                sentAt = DateTime.UtcNow.ToString("o")
            };

            var json = JsonConvert.SerializeObject(payload, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("batch"), Is.True);
            Assert.That(jObj.ContainsKey("sentAt"), Is.True);
            Assert.That(jObj["batch"].Type, Is.EqualTo(JTokenType.Array));
            Assert.That(jObj["batch"].Count(), Is.EqualTo(2));
        }

        [Test]
        public void BatchPayload_SentAt_IsIso8601Utc()
        {
            var sentAt = DateTime.UtcNow.ToString("o");

            Assert.That(DateTimeOffset.TryParse(sentAt, out var dto), Is.True);
            Assert.That(dto.Offset, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void BatchPayload_EmptyBatch_SerializesCorrectly()
        {
            var payload = new
            {
                batch = new List<EventEnvelope>(),
                sentAt = DateTime.UtcNow.ToString("o")
            };

            var json = JsonConvert.SerializeObject(payload, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj["batch"].Count(), Is.EqualTo(0));
        }

        // --- Edge Cases: Unicode and Special Characters ---

        [Test]
        public void Properties_WithUnicode_SerializesCorrectly()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            envelope.Properties = new Dictionary<string, object>
            {
                { "korean", "한국어 테스트" },
                { "emoji", "\U0001F600\U0001F680" },
                { "japanese", "日本語テスト" }
            };

            var json = JsonConvert.SerializeObject(envelope, _settings);
            var restored = JsonConvert.DeserializeObject<EventEnvelope>(json);

            Assert.That(restored.Properties["korean"], Is.EqualTo("한국어 테스트"));
            Assert.That(restored.Properties["emoji"], Is.EqualTo("\U0001F600\U0001F680"));
        }

        [Test]
        public void Properties_WithSpecialCharacters_SerializesCorrectly()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            envelope.Properties = new Dictionary<string, object>
            {
                { "quotes", "He said \"hello\"" },
                { "backslash", "path\\to\\file" },
                { "newline", "line1\nline2" },
                { "tab", "col1\tcol2" }
            };

            var json = JsonConvert.SerializeObject(envelope, _settings);
            var restored = JsonConvert.DeserializeObject<EventEnvelope>(json);

            Assert.That(restored.Properties["quotes"], Is.EqualTo("He said \"hello\""));
            Assert.That(restored.Properties["backslash"], Is.EqualTo("path\\to\\file"));
        }

        // --- Edge Cases: Property Value Types ---

        [Test]
        public void Properties_WithVariousTypes_SerializesCorrectly()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            envelope.Properties = new Dictionary<string, object>
            {
                { "int_val", 42 },
                { "float_val", 3.14 },
                { "bool_val", true },
                { "string_val", "hello" },
                { "null_val", null },
                { "long_val", 9999999999L }
            };

            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);
            var props = jObj["properties"];

            Assert.That(props["int_val"].Value<int>(), Is.EqualTo(42));
            Assert.That(props["float_val"].Value<double>(), Is.EqualTo(3.14).Within(0.001));
            Assert.That(props["bool_val"].Value<bool>(), Is.True);
            Assert.That(props["string_val"].Value<string>(), Is.EqualTo("hello"));
        }

        // --- Object Pooling: Reset and Reuse ---

        [Test]
        public void Reset_ThenReuse_ProducesCleanEnvelope()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            envelope.Reset();

            // Reuse with new values
            envelope.EventId = "new-id";
            envelope.Type = "identify";
            envelope.Timestamp = "2026-03-07T13:00:00.0000000Z";
            envelope.AnonymousId = "new-anon-id";
            envelope.Context = EventTestFactory.CreateMinimalContext();

            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj["eventId"].Value<string>(), Is.EqualTo("new-id"));
            Assert.That(jObj["type"].Value<string>(), Is.EqualTo("identify"));
            Assert.That(jObj.ContainsKey("userId"), Is.False, "Old userId should not persist");
            Assert.That(jObj.ContainsKey("event"), Is.False, "Old event should not persist");
            Assert.That(jObj.ContainsKey("properties"), Is.False, "Old properties should not persist");
        }

        // --- EventType Integration ---

        [Test]
        public void AllEventTypes_CanBeUsedInEnvelope()
        {
            foreach (EventType eventType in Enum.GetValues(typeof(EventType)))
            {
                var envelope = EventTestFactory.CreateMinimalEnvelope();
                envelope.Type = eventType.ToString().ToLowerInvariant();

                var json = JsonConvert.SerializeObject(envelope, _settings);
                var jObj = JObject.Parse(json);

                Assert.That(jObj["type"].Value<string>(),
                    Is.EqualTo(eventType.ToString().ToLowerInvariant()),
                    $"EventType.{eventType} should serialize correctly");
            }
        }
    }
}
