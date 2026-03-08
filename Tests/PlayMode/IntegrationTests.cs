using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DataFerret.Analytics.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration tests for the DataFerret Analytics SDK.
    /// Exercises the full pipeline: Init -> Track/Identify/Reset -> Flush -> Transport.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class IntegrationTests
    {
        private MockTransport _mockTransport;
        private DataFerretConfig _config;

        [SetUp]
        public void SetUp()
        {
            _mockTransport = new MockTransport();
            _config = new DataFerretConfig
            {
                WriteKey = "wk_test_integration",
                FlushSize = 500,
                FlushIntervalSeconds = 9999f,
                MaxQueueSize = 1000,
                MaxRetries = 0,
                TimeoutSeconds = 5,
                Debug = false,
                AutoCapture = new AutoCaptureConfig
                {
                    Sessions = false,
                    SceneChanges = false,
                    Errors = false,
                    Lifecycle = false,
                    PerformanceSamples = false
                }
            };

            // Clean identity state
            PlayerPrefs.DeleteKey("df_anonymous_id");
            PlayerPrefs.DeleteKey("df_first_session_time");
            PlayerPrefs.DeleteKey("df_last_session_time");
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            DataFerretAnalytics.Shutdown();
            FlushScheduler.DestroyInstance();
            PlayerPrefs.DeleteKey("df_anonymous_id");
            PlayerPrefs.DeleteKey("df_first_session_time");
            PlayerPrefs.DeleteKey("df_last_session_time");
            PlayerPrefs.Save();
        }

        /// <summary>
        /// TC-IT-001: Init -> Track multiple events -> Flush -> transport receives all events.
        /// </summary>
        [UnityTest]
        public IEnumerator Init_TrackMultiple_Flush_TransportReceivesAllEvents()
        {
            DataFerretAnalytics.InitForTesting(_config, _mockTransport);

            DataFerretAnalytics.Track("level_start", new Dictionary<string, object>
            {
                { "level", 1 }
            });
            DataFerretAnalytics.Track("item_collected", new Dictionary<string, object>
            {
                { "item", "coin" },
                { "count", 10 }
            });
            DataFerretAnalytics.Track("level_complete");

            yield return null;

            DataFerretAnalytics.Flush();

            // Wait for flush coroutine to complete
            yield return null;
            yield return null;

            Assert.That(_mockTransport.SentBatches.Count, Is.GreaterThan(0),
                "At least one batch should have been sent.");

            var allEvents = new List<EventEnvelope>();
            for (int i = 0; i < _mockTransport.SentBatches.Count; i++)
            {
                allEvents.AddRange(_mockTransport.SentBatches[i]);
            }

            Assert.That(allEvents.Count, Is.EqualTo(3),
                "All 3 tracked events should be in the sent batches.");

            Assert.That(allEvents.Any(e => e.Event == "level_start"), Is.True,
                "level_start event should be present.");
            Assert.That(allEvents.Any(e => e.Event == "item_collected"), Is.True,
                "item_collected event should be present.");
            Assert.That(allEvents.Any(e => e.Event == "level_complete"), Is.True,
                "level_complete event should be present.");

            // Verify each event has required fields
            for (int i = 0; i < allEvents.Count; i++)
            {
                var evt = allEvents[i];
                Assert.That(evt.Type, Is.EqualTo("track"),
                    $"Event {evt.Event} should be of type 'track'.");
                Assert.That(evt.EventId, Is.Not.Null.And.Not.Empty,
                    $"Event {evt.Event} should have an EventId.");
                Assert.That(evt.Timestamp, Is.Not.Null.And.Not.Empty,
                    $"Event {evt.Event} should have a Timestamp.");
                Assert.That(evt.AnonymousId, Is.Not.Null.And.Not.Empty,
                    $"Event {evt.Event} should have an AnonymousId.");
            }
        }

        /// <summary>
        /// TC-IT-002: Identify sets userId; subsequent Track events include the userId.
        /// </summary>
        [UnityTest]
        public IEnumerator Identify_ThenTrack_EventsContainUserId()
        {
            DataFerretAnalytics.InitForTesting(_config, _mockTransport);

            DataFerretAnalytics.Identify("player-99", new Dictionary<string, object>
            {
                { "plan", "premium" },
                { "level", 42 }
            });

            DataFerretAnalytics.Track("purchase", new Dictionary<string, object>
            {
                { "item", "sword" },
                { "price", 9.99 }
            });

            yield return null;

            DataFerretAnalytics.Flush();
            yield return null;
            yield return null;

            Assert.That(_mockTransport.SentBatches.Count, Is.GreaterThan(0));

            var allEvents = new List<EventEnvelope>();
            for (int i = 0; i < _mockTransport.SentBatches.Count; i++)
            {
                allEvents.AddRange(_mockTransport.SentBatches[i]);
            }

            // Verify the identify event
            var identifyEvent = allEvents.Find(e => e.Type == "identify");
            Assert.That(identifyEvent, Is.Not.Null, "Identify event should be present.");
            Assert.That(identifyEvent.UserId, Is.EqualTo("player-99"),
                "Identify event should carry the userId.");

            // Verify the track event after identify carries the userId
            var purchaseEvent = allEvents.Find(e => e.Event == "purchase");
            Assert.That(purchaseEvent, Is.Not.Null, "Purchase track event should be present.");
            Assert.That(purchaseEvent.UserId, Is.EqualTo("player-99"),
                "Track event after Identify should carry the userId.");
            Assert.That(purchaseEvent.Properties, Contains.Key("item"));
            Assert.That(purchaseEvent.Properties["item"], Is.EqualTo("sword"));
        }

        /// <summary>
        /// TC-IT-003: Reset clears userId, generates new anonymousId.
        /// Subsequent Track events have null userId and the new anonymousId.
        /// </summary>
        [UnityTest]
        public IEnumerator Reset_ClearsUserId_GeneratesNewAnonymousId()
        {
            DataFerretAnalytics.InitForTesting(_config, _mockTransport);

            DataFerretAnalytics.Identify("player-99");
            string oldAnonymousId = DataFerretAnalytics.AnonymousId;

            Assert.That(DataFerretAnalytics.UserId, Is.EqualTo("player-99"));

            yield return null;

            // Reset identity
            DataFerretAnalytics.Reset();

            Assert.That(DataFerretAnalytics.UserId, Is.Null,
                "UserId should be null after Reset.");
            Assert.That(DataFerretAnalytics.AnonymousId, Is.Not.Null.And.Not.Empty,
                "AnonymousId should not be null after Reset.");
            Assert.That(DataFerretAnalytics.AnonymousId, Is.Not.EqualTo(oldAnonymousId),
                "AnonymousId should change after Reset.");

            string newAnonymousId = DataFerretAnalytics.AnonymousId;

            // Track an event after reset
            DataFerretAnalytics.Track("post_reset_action");
            DataFerretAnalytics.Flush();
            yield return null;
            yield return null;

            var allEvents = new List<EventEnvelope>();
            for (int i = 0; i < _mockTransport.SentBatches.Count; i++)
            {
                allEvents.AddRange(_mockTransport.SentBatches[i]);
            }

            // Find the post-reset track event (skip the identify event)
            var postResetEvent = allEvents.Find(e => e.Event == "post_reset_action");
            Assert.That(postResetEvent, Is.Not.Null, "post_reset_action event should be present.");
            Assert.That(postResetEvent.UserId, Is.Null,
                "Track event after Reset should have null userId.");
            Assert.That(postResetEvent.AnonymousId, Is.EqualTo(newAnonymousId),
                "Track event after Reset should use the new anonymousId.");
        }

        /// <summary>
        /// TC-IT-004: SetGlobalProperties merges into all subsequent Track events.
        /// Event-specific properties override global properties with the same key.
        /// </summary>
        [UnityTest]
        public IEnumerator SetGlobalProperties_MergedIntoTrackEvents()
        {
            DataFerretAnalytics.InitForTesting(_config, _mockTransport);

            DataFerretAnalytics.SetGlobalProperties(new Dictionary<string, object>
            {
                { "app_version", "2.0.0" },
                { "environment", "staging" },
                { "shared_key", "global_value" }
            });

            DataFerretAnalytics.Track("event_with_globals", new Dictionary<string, object>
            {
                { "custom_prop", "hello" },
                { "shared_key", "event_value" } // should override global
            });

            DataFerretAnalytics.Track("event_globals_only");

            yield return null;

            DataFerretAnalytics.Flush();
            yield return null;
            yield return null;

            Assert.That(_mockTransport.SentBatches.Count, Is.GreaterThan(0));

            var allEvents = new List<EventEnvelope>();
            for (int i = 0; i < _mockTransport.SentBatches.Count; i++)
            {
                allEvents.AddRange(_mockTransport.SentBatches[i]);
            }

            // Event with both global and event-specific properties
            var eventWithGlobals = allEvents.Find(e => e.Event == "event_with_globals");
            Assert.That(eventWithGlobals, Is.Not.Null);
            Assert.That(eventWithGlobals.Properties, Contains.Key("app_version"),
                "Global property 'app_version' should be merged.");
            Assert.That(eventWithGlobals.Properties["app_version"], Is.EqualTo("2.0.0"));
            Assert.That(eventWithGlobals.Properties, Contains.Key("environment"),
                "Global property 'environment' should be merged.");
            Assert.That(eventWithGlobals.Properties["environment"], Is.EqualTo("staging"));
            Assert.That(eventWithGlobals.Properties, Contains.Key("custom_prop"),
                "Event-specific property should be present.");
            Assert.That(eventWithGlobals.Properties["custom_prop"], Is.EqualTo("hello"));
            Assert.That(eventWithGlobals.Properties["shared_key"], Is.EqualTo("event_value"),
                "Event-specific property should override global property with the same key.");

            // Event with only global properties
            var eventGlobalsOnly = allEvents.Find(e => e.Event == "event_globals_only");
            Assert.That(eventGlobalsOnly, Is.Not.Null);
            Assert.That(eventGlobalsOnly.Properties, Contains.Key("app_version"),
                "Global property should be present even without event-specific properties.");
            Assert.That(eventGlobalsOnly.Properties["app_version"], Is.EqualTo("2.0.0"));
            Assert.That(eventGlobalsOnly.Properties, Contains.Key("shared_key"));
            Assert.That(eventGlobalsOnly.Properties["shared_key"], Is.EqualTo("global_value"),
                "Without override, global value should be used.");
        }
    }
}
