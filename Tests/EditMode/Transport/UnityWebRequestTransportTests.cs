using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for UnityWebRequestTransport.
    /// Validates payload serialization, URL construction, and constructor behavior.
    /// HTTP/coroutine behavior is tested in PlayMode.
    /// </summary>
    [TestFixture]
    [Category("Transport")]
    public class UnityWebRequestTransportTests
    {
        private UnityWebRequestTransport _transport;

        private const string TestEndpoint = "https://collect.dataferret.io";
        private const string TestWriteKey = "wk_test_key_123";
        private const int TestTimeout = 15;
        private const int TestMaxRetries = 5;

        [SetUp]
        public void SetUp()
        {
            _transport = new UnityWebRequestTransport(
                TestEndpoint, TestWriteKey, TestTimeout, TestMaxRetries);
        }

        // --- TC-TR-EDIT-001: BuildPayload returns valid JSON with "batch" array and "sentAt" field ---

        [Test]
        public void BuildPayload_WithEvents_ReturnsJsonWithBatchArrayAndSentAt()
        {
            var events = new List<EventEnvelope>
            {
                EventTestFactory.CreateFullEnvelope()
            };

            var payload = _transport.BuildPayload(events);
            var jObj = JObject.Parse(payload);

            Assert.That(jObj.ContainsKey("batch"), Is.True, "Payload must contain 'batch' field");
            Assert.That(jObj["batch"].Type, Is.EqualTo(JTokenType.Array), "'batch' must be an array");
            Assert.That(jObj.ContainsKey("sentAt"), Is.True, "Payload must contain 'sentAt' field");

            // sentAt should be a valid ISO 8601 timestamp
            var sentAt = jObj["sentAt"].Value<string>();
            Assert.That(
                DateTimeOffset.TryParse(sentAt, out var dto), Is.True,
                "sentAt should be parseable as ISO 8601");
            Assert.That(dto.Offset, Is.EqualTo(TimeSpan.Zero),
                "sentAt should be UTC");
        }

        // --- TC-TR-EDIT-002: BuildPayload with empty list produces empty batch array ---

        [Test]
        public void BuildPayload_WithEmptyList_ProducesEmptyBatchArray()
        {
            var events = new List<EventEnvelope>();

            var payload = _transport.BuildPayload(events);
            var jObj = JObject.Parse(payload);

            Assert.That(jObj["batch"].Type, Is.EqualTo(JTokenType.Array));
            Assert.That(jObj["batch"].HasValues, Is.False,
                "Empty event list should produce empty batch array");
            Assert.That(jObj.ContainsKey("sentAt"), Is.True,
                "sentAt must still be present even with empty batch");
        }

        // --- TC-TR-EDIT-003: BuildPayload preserves EventEnvelope fields in serialized output ---

        [Test]
        public void BuildPayload_PreservesEventEnvelopeFields()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            var events = new List<EventEnvelope> { envelope };

            var payload = _transport.BuildPayload(events);
            var jObj = JObject.Parse(payload);
            var batchArray = (JArray)jObj["batch"];

            Assert.That(batchArray.Count, Is.EqualTo(1));

            var serializedEvent = batchArray[0];
            Assert.That(serializedEvent["eventId"]?.Value<string>(),
                Is.EqualTo(EventTestFactory.ValidEventId),
                "EventId must be preserved");
            Assert.That(serializedEvent["type"]?.Value<string>(),
                Is.EqualTo("track"),
                "Type must be preserved");
            Assert.That(serializedEvent["timestamp"]?.Value<string>(),
                Is.EqualTo(EventTestFactory.ValidTimestamp),
                "Timestamp must be preserved");
            Assert.That(serializedEvent["anonymousId"]?.Value<string>(),
                Is.EqualTo(EventTestFactory.ValidAnonymousId),
                "AnonymousId must be preserved");
            Assert.That(serializedEvent["userId"]?.Value<string>(),
                Is.EqualTo(EventTestFactory.ValidUserId),
                "UserId must be preserved");
            Assert.That(serializedEvent["event"]?.Value<string>(),
                Is.EqualTo(EventTestFactory.ValidEventName),
                "Event name must be preserved");
            Assert.That(serializedEvent["context"], Is.Not.Null,
                "Context must be present");
        }

        // --- TC-TR-EDIT-004: Constructor accepts parameters without exception ---

        [Test]
        public void Constructor_WithValidParameters_DoesNotThrow()
        {
            Assert.That(() => new UnityWebRequestTransport(
                "https://example.com", "wk_abc", 10, 3), Throws.Nothing);
        }

        [Test]
        public void Constructor_WithDefaultOptionalParameters_DoesNotThrow()
        {
            Assert.That(() => new UnityWebRequestTransport(
                "https://example.com", "wk_abc"), Throws.Nothing);
        }
    }
}
