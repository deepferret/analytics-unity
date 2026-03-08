using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DataFerret.Analytics.Tests.EditMode
{
    /// <summary>
    /// Integration-style EditMode tests for the <see cref="DataFerretAnalytics"/> static facade.
    /// Validates initialization, event tracking, identity management, and reset behavior.
    /// </summary>
    [TestFixture]
    internal class DataFerretAnalyticsTests
    {
        private MockTransport _mockTransport;
        private DataFerretConfig _validConfig;

        [SetUp]
        public void SetUp()
        {
            _mockTransport = new MockTransport();
            _validConfig = new DataFerretConfig
            {
                WriteKey = "wk_test_key_123",
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
        /// TC-IT-001: Init, Track multiple events, FlushSync sends all events via transport.
        /// </summary>
        [Test]
        public void Init_TrackMultipleEvents_FlushSync_SendsAllEventsViaTransport()
        {
            // Arrange
            DataFerretAnalytics.InitForTesting(_validConfig, _mockTransport);

            // Act
            DataFerretAnalytics.Track("event1", new Dictionary<string, object> { { "key1", "value1" } });
            DataFerretAnalytics.Track("event2", new Dictionary<string, object> { { "key2", "value2" } });
            DataFerretAnalytics.Track("event3");
            DataFerretAnalytics.FlushSync();

            // Assert
            Assert.That(_mockTransport.SentBatches.Count, Is.GreaterThan(0),
                "At least one batch should have been sent.");

            int totalEvents = _mockTransport.SentBatches.Sum(b => b.Count);
            Assert.AreEqual(3, totalEvents,
                "All 3 tracked events should have been sent.");

            var allEvents = _mockTransport.SentBatches.SelectMany(b => b).ToList();
            Assert.IsTrue(allEvents.Any(e => e.Event == "event1"), "event1 should be in sent batches.");
            Assert.IsTrue(allEvents.Any(e => e.Event == "event2"), "event2 should be in sent batches.");
            Assert.IsTrue(allEvents.Any(e => e.Event == "event3"), "event3 should be in sent batches.");
        }

        /// <summary>
        /// TC-IT-002: Init with invalid WriteKey throws ArgumentException.
        /// </summary>
        [Test]
        public void Init_InvalidWriteKey_ThrowsArgumentException()
        {
            // Arrange
            var invalidConfig = new DataFerretConfig
            {
                WriteKey = "invalid_key_no_wk_prefix"
            };

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
            {
                DataFerretAnalytics.Init(invalidConfig);
            });
        }

        /// <summary>
        /// TC-IT-003: Identify sets userId, subsequent Track events include the userId.
        /// </summary>
        [Test]
        public void Identify_ThenTrack_EventContainsUserId()
        {
            // Arrange
            DataFerretAnalytics.InitForTesting(_validConfig, _mockTransport);

            // Act
            DataFerretAnalytics.Identify("user-42", new Dictionary<string, object>
            {
                { "plan", "premium" }
            });
            DataFerretAnalytics.Track("purchase", new Dictionary<string, object>
            {
                { "item", "sword" }
            });
            DataFerretAnalytics.FlushSync();

            // Assert
            Assert.That(_mockTransport.SentBatches.Count, Is.GreaterThan(0),
                "At least one batch should have been sent.");

            var allEvents = _mockTransport.SentBatches.SelectMany(b => b).ToList();

            // Find the "purchase" track event
            var purchaseEvent = allEvents.FirstOrDefault(e => e.Event == "purchase");
            Assert.IsNotNull(purchaseEvent, "purchase event should exist in sent batches.");
            Assert.AreEqual("user-42", purchaseEvent.UserId,
                "purchase event should carry the identified userId.");

            // Verify the identify event was also sent
            var identifyEvent = allEvents.FirstOrDefault(e => e.Type == "identify");
            Assert.IsNotNull(identifyEvent, "identify event should exist in sent batches.");
            Assert.AreEqual("user-42", identifyEvent.UserId,
                "identify event should carry the userId.");
        }

        /// <summary>
        /// TC-IT-004: Reset clears userId, generates new anonymousId, and subsequent
        /// Track events have null userId.
        /// </summary>
        [Test]
        public void Reset_ClearsUserId_GeneratesNewAnonymousId()
        {
            // Arrange
            DataFerretAnalytics.InitForTesting(_validConfig, _mockTransport);
            DataFerretAnalytics.Identify("user-42");
            string oldAnonymousId = DataFerretAnalytics.AnonymousId;

            // Act
            DataFerretAnalytics.Reset();

            // Assert identity state
            Assert.IsNull(DataFerretAnalytics.UserId,
                "UserId should be null after Reset.");
            Assert.IsNotNull(DataFerretAnalytics.AnonymousId,
                "AnonymousId should not be null after Reset.");
            Assert.AreNotEqual(oldAnonymousId, DataFerretAnalytics.AnonymousId,
                "AnonymousId should change after Reset.");

            // Track an event after reset and verify userId is null
            DataFerretAnalytics.Track("post_reset");
            DataFerretAnalytics.FlushSync();

            var allEvents = _mockTransport.SentBatches.SelectMany(b => b).ToList();
            var postResetEvent = allEvents.FirstOrDefault(e => e.Event == "post_reset");
            Assert.IsNotNull(postResetEvent, "post_reset event should exist in sent batches.");
            Assert.IsNull(postResetEvent.UserId,
                "post_reset event should have null userId after Reset.");
        }

        /// <summary>
        /// TC-IT-005: Track before Init is a silent no-op (no exception).
        /// </summary>
        [Test]
        public void Track_BeforeInit_SilentNoOp_NoException()
        {
            // Arrange: ensure SDK is not initialized
            DataFerretAnalytics.Shutdown();

            // Act & Assert: should not throw
            Assert.DoesNotThrow(() =>
            {
                DataFerretAnalytics.Track("should_not_crash", new Dictionary<string, object>
                {
                    { "key", "value" }
                });
            });

            Assert.IsFalse(DataFerretAnalytics.IsInitialized,
                "SDK should not be initialized.");
        }
    }
}
