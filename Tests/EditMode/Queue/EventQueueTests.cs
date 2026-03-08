using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Queue")]
    public class EventQueueTests
    {
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
        /// TC-EQ-001: Enqueue then DequeueBatch returns same event (verify EventId matches).
        /// </summary>
        [Test]
        public void Enqueue_ThenDequeueBatch_ReturnsSameEvent()
        {
            var queue = new EventQueue(maxSize: 10);
            var envelope = CreateTestEnvelope(1);

            queue.Enqueue(envelope);
            var batch = queue.DequeueBatch();

            Assert.That(batch.Count, Is.EqualTo(1));
            Assert.That(batch[0].EventId, Is.EqualTo("evt-1"));
        }

        /// <summary>
        /// TC-EQ-002: DequeueBatch with batchSize=3 returns at most 3 when queue has 5 events.
        /// </summary>
        [Test]
        public void DequeueBatch_WithBatchSize3_ReturnsAtMost3WhenQueueHas5()
        {
            var queue = new EventQueue(maxSize: 10);
            for (int i = 0; i < 5; i++)
            {
                queue.Enqueue(CreateTestEnvelope(i));
            }

            var batch = queue.DequeueBatch(batchSize: 3);

            Assert.That(batch.Count, Is.EqualTo(3));
            Assert.That(queue.Count, Is.EqualTo(2), "Remaining events should stay in queue");
        }

        /// <summary>
        /// TC-EQ-003: MaxQueueSize=5 with 6 enqueues -> Count remains 5.
        /// </summary>
        [Test]
        public void Enqueue_ExceedsMaxSize_CountRemainsAtMax()
        {
            var queue = new EventQueue(maxSize: 5);
            for (int i = 0; i < 6; i++)
            {
                queue.Enqueue(CreateTestEnvelope(i));
            }

            Assert.That(queue.Count, Is.EqualTo(5));
        }

        /// <summary>
        /// TC-EQ-004: MaxQueueSize=3 with 4 enqueues -> oldest event dropped (first enqueued EventId is gone).
        /// </summary>
        [Test]
        public void Enqueue_ExceedsMaxSize_DropsOldestEvent()
        {
            var queue = new EventQueue(maxSize: 3);
            for (int i = 0; i < 4; i++)
            {
                queue.Enqueue(CreateTestEnvelope(i));
            }

            var batch = queue.DequeueBatch(batchSize: 10);

            Assert.That(batch.Count, Is.EqualTo(3));

            var eventIds = new List<string>();
            for (int i = 0; i < batch.Count; i++)
            {
                eventIds.Add(batch[i].EventId);
            }

            Assert.That(eventIds, Does.Not.Contain("evt-0"),
                "Oldest event (evt-0) should have been dropped");
            Assert.That(eventIds, Does.Contain("evt-1"));
            Assert.That(eventIds, Does.Contain("evt-2"));
            Assert.That(eventIds, Does.Contain("evt-3"));
        }

        /// <summary>
        /// TC-EQ-005: Parallel Enqueue from multiple threads -> no exceptions, Count &lt;= maxSize.
        /// </summary>
        [Test]
        public void Enqueue_ParallelFromMultipleThreads_NoExceptionsAndCountWithinMax()
        {
            const int maxSize = 50;
            const int iterations = 100;
            var queue = new EventQueue(maxSize: maxSize);

            Assert.That(() =>
            {
                Parallel.For(0, iterations, i =>
                {
                    queue.Enqueue(CreateTestEnvelope(i));
                });
            }, Throws.Nothing, "Parallel enqueue should not throw exceptions");

            Assert.That(queue.Count, Is.LessThanOrEqualTo(maxSize),
                "Queue count should not exceed maxSize after parallel enqueue");
        }

        /// <summary>
        /// TC-EQ-006: DequeueBatch on empty queue returns empty list (Count=0).
        /// </summary>
        [Test]
        public void DequeueBatch_OnEmptyQueue_ReturnsEmptyList()
        {
            var queue = new EventQueue(maxSize: 10);

            var batch = queue.DequeueBatch();

            Assert.That(batch, Is.Not.Null);
            Assert.That(batch.Count, Is.EqualTo(0));
            Assert.That(queue.IsEmpty, Is.True);
        }
    }
}
