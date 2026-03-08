using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DataFerret.Analytics.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for <see cref="FlushScheduler"/>.
    /// Validates periodic flush, pause/quit behavior, singleton durability, and FlushSize triggers.
    /// </summary>
    [TestFixture]
    [Category("Lifecycle")]
    public class FlushSchedulerTests
    {
        private EventQueue _queue;
        private MockTransport _transport;
        private DataFerretConfig _config;

        [SetUp]
        public void SetUp()
        {
            _queue = new EventQueue(maxSize: 100);
            _transport = new MockTransport();
            _config = new DataFerretConfig
            {
                WriteKey = "wk_test_key",
                FlushSize = 5,
                FlushIntervalSeconds = 9999f, // very long to avoid accidental flushes
                MaxQueueSize = 100,
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
        }

        private static EventEnvelope CreateTestEnvelope(int index)
        {
            return new EventEnvelope
            {
                EventId = $"evt-{index}",
                Type = "track",
                Timestamp = "2026-03-07T12:00:00.0000000Z",
                AnonymousId = "anon-test",
                Event = $"test_event_{index}"
            };
        }

        /// <summary>
        /// TC-FS-001: After flushInterval elapses, queued events are automatically flushed.
        /// Uses a very short FlushIntervalSeconds to trigger within the test.
        /// </summary>
        [UnityTest]
        public IEnumerator FlushInterval_Elapsed_AutoFlushesQueuedEvents()
        {
            _config.FlushIntervalSeconds = 0.3f;
            _config.FlushSize = 9999; // high FlushSize so it does not trigger size-based flush
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            // Enqueue events
            for (int i = 0; i < 3; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            Assert.That(_transport.SentBatches.Count, Is.EqualTo(0),
                "No batches should have been sent yet.");

            // Wait longer than FlushIntervalSeconds to let Update() trigger periodic flush
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(_transport.SentBatches.Count, Is.GreaterThan(0),
                "Periodic flush should have sent at least one batch after flushInterval elapsed.");
            Assert.That(_queue.IsEmpty, Is.True,
                "Queue should be empty after periodic flush.");
        }

        /// <summary>
        /// TC-FS-002: OnApplicationPause(true) triggers an immediate flush of queued events.
        /// </summary>
        [UnityTest]
        public IEnumerator OnApplicationPause_True_TriggersImmediateFlush()
        {
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            for (int i = 0; i < 3; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            Assert.That(_transport.SentBatches.Count, Is.EqualTo(0));

            // Simulate OnApplicationPause(true) via SendMessage
            scheduler.SendMessage("OnApplicationPause", true);

            // Wait a frame for the coroutine to run
            yield return null;
            yield return null;

            Assert.That(_transport.SentBatches.Count, Is.GreaterThan(0),
                "OnApplicationPause(true) should trigger a flush.");
        }

        /// <summary>
        /// TC-FS-003: FlushScheduler GameObject persists across scene loads via DontDestroyOnLoad.
        /// </summary>
        [UnityTest]
        public IEnumerator DontDestroyOnLoad_SurvivesSceneTransition()
        {
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);
            var go = scheduler.gameObject;

            Assert.That(go, Is.Not.Null);
            Assert.That(go.scene.name, Is.EqualTo("DontDestroyOnLoad"));

            // Load the currently active scene again (additive then unload is safest)
            var activeScene = SceneManager.GetActiveScene();
            yield return SceneManager.LoadSceneAsync(activeScene.buildIndex, LoadSceneMode.Single);

            // FlushScheduler should still exist
            Assert.That(FlushScheduler.Instance, Is.Not.Null,
                "FlushScheduler singleton should survive scene transition.");
            Assert.That(FlushScheduler.Instance.gameObject != null, Is.True,
                "FlushScheduler GameObject should not have been destroyed.");
        }

        /// <summary>
        /// TC-FS-004: Manually adding a second FlushScheduler component destroys the duplicate.
        /// </summary>
        [UnityTest]
        public IEnumerator Singleton_DuplicateInstance_DestroysDuplicate()
        {
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);
            var originalInstance = FlushScheduler.Instance;

            // Manually create a second FlushScheduler (simulating duplicate)
            var duplicateGo = new GameObject("[DataFerret] FlushScheduler Duplicate");
            var duplicate = duplicateGo.AddComponent<FlushScheduler>();

            // Wait a frame for Awake to run and destroy the duplicate
            yield return null;

            Assert.That(FlushScheduler.Instance, Is.SameAs(originalInstance),
                "Original singleton should remain.");
            // The duplicate GameObject should be destroyed
            Assert.That(duplicateGo == null, Is.True,
                "Duplicate FlushScheduler GameObject should be destroyed.");
        }

        /// <summary>
        /// TC-FS-005: When the queue count reaches FlushSize, an immediate flush is triggered
        /// without waiting for the periodic interval.
        /// </summary>
        [UnityTest]
        public IEnumerator FlushSize_Exceeded_TriggersImmediateFlush()
        {
            _config.FlushSize = 3;
            _config.FlushIntervalSeconds = 9999f;
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            // Enqueue exactly FlushSize events
            for (int i = 0; i < 3; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            Assert.That(_transport.SentBatches.Count, Is.EqualTo(0),
                "No flush yet before Update runs.");

            // Wait frames for Update() to detect queue count >= FlushSize
            yield return null;
            yield return null;

            Assert.That(_transport.SentBatches.Count, Is.GreaterThan(0),
                "FlushSize threshold should trigger an immediate flush.");
        }

        /// <summary>
        /// TC-FS-006: OnApplicationQuit() triggers FlushSync to synchronously send remaining events.
        /// We verify by calling FlushSync directly since OnApplicationQuit cannot be easily simulated.
        /// </summary>
        [UnityTest]
        public IEnumerator OnApplicationQuit_CallsFlushSync()
        {
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            for (int i = 0; i < 4; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            yield return null;

            // Directly call FlushSync (which OnApplicationQuit calls internally)
            scheduler.FlushSync();

            Assert.That(_transport.SentBatches.Count, Is.GreaterThan(0),
                "FlushSync should send queued events.");

            int totalEvents = 0;
            for (int i = 0; i < _transport.SentBatches.Count; i++)
            {
                totalEvents += _transport.SentBatches[i].Count;
            }

            Assert.That(totalEvents, Is.EqualTo(4),
                "All 4 queued events should have been sent by FlushSync.");
            Assert.That(_queue.IsEmpty, Is.True,
                "Queue should be empty after FlushSync.");
        }
    }
}
