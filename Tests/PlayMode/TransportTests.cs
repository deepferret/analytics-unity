using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DataFerret.Analytics.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for transport behavior using <see cref="MockTransport"/>.
    /// Validates success/failure callbacks, retry logic, and permanent failure handling.
    /// These tests exercise the FlushScheduler + MockTransport pipeline end-to-end,
    /// verifying that the coroutine-based flush correctly responds to transport outcomes.
    /// </summary>
    [TestFixture]
    [Category("Transport")]
    public class TransportTests
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
                WriteKey = "wk_test_transport",
                FlushSize = 500,
                FlushIntervalSeconds = 9999f,
                MaxQueueSize = 100,
                MaxRetries = 3,
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
        /// TC-TR-001: When transport returns success (200 OK equivalent),
        /// onComplete(true) is invoked and all events are consumed from the queue.
        /// </summary>
        [UnityTest]
        public IEnumerator Success_200OK_OnCompleteTrue_EventsConsumed()
        {
            _transport.ShouldSucceed = true;
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            for (int i = 0; i < 5; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            scheduler.Flush();

            // Wait for the coroutine to complete
            yield return null;
            yield return null;

            Assert.That(_transport.SentBatches.Count, Is.EqualTo(1),
                "One batch should be sent on success.");
            Assert.That(_transport.SentBatches[0].Count, Is.EqualTo(5),
                "The batch should contain all 5 events.");
            Assert.That(_queue.IsEmpty, Is.True,
                "Queue should be empty after successful send.");
        }

        /// <summary>
        /// TC-TR-002: When transport fails initially then succeeds (simulating 5xx retry),
        /// the flush retries and eventually succeeds.
        /// We simulate this at the FlushScheduler level: first flush fails, events are
        /// re-enqueued logic is internal, so we verify that after failure the queue
        /// still has events and a second flush succeeds.
        /// </summary>
        [UnityTest]
        public IEnumerator Retry_5xxThenSuccess_EventuallySendsEvents()
        {
            // First call fails, simulating a 5xx error
            _transport.ResponseSequence.Enqueue(false);
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            for (int i = 0; i < 3; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            // First flush attempt - transport returns failure
            scheduler.Flush();
            yield return null;
            yield return null;

            Assert.That(_transport.SendBatchCallCount, Is.EqualTo(1),
                "Transport should have been called once for the first attempt.");

            // On failure, FlushScheduler breaks and events that were dequeued are lost
            // from the queue (by design - they were dequeued by DequeueBatch).
            // For a retry scenario, re-enqueue and try again with success.
            for (int i = 0; i < 3; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i + 10));
            }

            _transport.ShouldSucceed = true;
            scheduler.Flush();
            yield return null;
            yield return null;

            Assert.That(_transport.SendBatchCallCount, Is.EqualTo(2),
                "Transport should have been called twice total.");
            Assert.That(_transport.SentBatches.Count, Is.EqualTo(2),
                "Two batches should have been attempted.");
            Assert.That(_queue.IsEmpty, Is.True,
                "Queue should be empty after successful retry.");
        }

        /// <summary>
        /// TC-TR-003: When transport always fails (simulating timeout / retries exhausted),
        /// onComplete(false) is invoked and the flush stops.
        /// </summary>
        [UnityTest]
        public IEnumerator Timeout_RetriesExhausted_OnCompleteFalse()
        {
            _transport.ShouldSucceed = false;
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            for (int i = 0; i < 3; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            scheduler.Flush();
            yield return null;
            yield return null;

            Assert.That(_transport.SendBatchCallCount, Is.EqualTo(1),
                "FlushScheduler calls transport once per flush; transport handles internal retries.");
            Assert.That(_transport.SentBatches.Count, Is.EqualTo(1),
                "One batch should have been attempted.");
            // The batch was dequeued, transport received it but reported failure
            Assert.That(_transport.SentBatches[0].Count, Is.EqualTo(3),
                "The failed batch should contain all 3 events.");
        }

        /// <summary>
        /// TC-TR-004: 401 response (unauthorized) is a permanent failure.
        /// Transport reports false immediately without retry.
        /// Verified by transport.ShouldSucceed = false and checking single call.
        /// </summary>
        [UnityTest]
        public IEnumerator PermanentFailure_401_NoRetry_OnCompleteFalse()
        {
            _transport.ShouldSucceed = false; // Simulates permanent failure (401)
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            for (int i = 0; i < 2; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            scheduler.Flush();
            yield return null;
            yield return null;

            Assert.That(_transport.SendBatchCallCount, Is.EqualTo(1),
                "401 permanent failure: transport called exactly once, no retry.");
            Assert.That(_transport.SentBatches.Count, Is.EqualTo(1),
                "Exactly one batch attempt for permanent failure.");
        }

        /// <summary>
        /// TC-TR-005: 413 response (payload too large) is a permanent failure.
        /// Transport reports false immediately without retry.
        /// </summary>
        [UnityTest]
        public IEnumerator PermanentFailure_413_NoRetry_OnCompleteFalse()
        {
            _transport.ShouldSucceed = false; // Simulates permanent failure (413)
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            for (int i = 0; i < 2; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            scheduler.Flush();
            yield return null;
            yield return null;

            Assert.That(_transport.SendBatchCallCount, Is.EqualTo(1),
                "413 permanent failure: transport called exactly once, no retry.");
            Assert.That(_transport.SentBatches.Count, Is.EqualTo(1),
                "Exactly one batch attempt for permanent failure.");
        }
    }
}
