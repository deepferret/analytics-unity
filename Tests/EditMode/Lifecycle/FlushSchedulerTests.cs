using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DataFerret.Analytics.Tests.EditMode
{
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
                FlushSize = 20,
                FlushIntervalSeconds = 30f,
                Debug = true
            };
        }

        [TearDown]
        public void TearDown()
        {
            FlushScheduler.DestroyInstance();
        }

        /// <summary>
        /// Creates a test EventEnvelope with a unique EventId.
        /// </summary>
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
        /// TC-FS-001: Create() returns a non-null singleton instance.
        /// </summary>
        [Test]
        public void Create_ReturnsSingletonInstance_NotNull()
        {
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            Assert.That(scheduler, Is.Not.Null);
            Assert.That(FlushScheduler.Instance, Is.Not.Null);
            Assert.That(FlushScheduler.Instance, Is.SameAs(scheduler));
        }

        /// <summary>
        /// TC-FS-002: Calling Create() twice returns the same singleton instance.
        /// </summary>
        [Test]
        public void Create_CalledTwice_ReturnsSameInstance()
        {
            var first = FlushScheduler.Create(_queue, _transport, _config);
            var second = FlushScheduler.Create(_queue, _transport, _config);

            Assert.That(second, Is.SameAs(first));
            Assert.That(FlushScheduler.Instance, Is.SameAs(first));
        }

        /// <summary>
        /// TC-FS-003: FlushSync dequeues events from the queue and sends them via transport.
        /// </summary>
        [Test]
        public void FlushSync_WithQueuedEvents_DequeuesToTransport()
        {
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            // Enqueue 3 events
            for (int i = 0; i < 3; i++)
            {
                _queue.Enqueue(CreateTestEnvelope(i));
            }

            Assert.That(_queue.Count, Is.EqualTo(3));

            scheduler.FlushSync();

            // Transport should have received one batch with 3 events
            Assert.That(_transport.SentBatches.Count, Is.EqualTo(1));
            Assert.That(_transport.SentBatches[0].Count, Is.EqualTo(3));
            // Queue should be empty after flush
            Assert.That(_queue.IsEmpty, Is.True);
        }

        /// <summary>
        /// TC-FS-004: Flush does nothing when the queue is empty (no transport calls).
        /// </summary>
        [Test]
        public void Flush_WhenQueueIsEmpty_DoesNotCallTransport()
        {
            var scheduler = FlushScheduler.Create(_queue, _transport, _config);

            Assert.That(_queue.IsEmpty, Is.True);

            scheduler.Flush();

            Assert.That(_transport.SentBatches.Count, Is.EqualTo(0));
        }
    }
}
