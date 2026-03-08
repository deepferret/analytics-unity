using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Thread-safe event queue with maximum size limit and oldest-drop policy.
    /// Uses <see cref="ConcurrentQueue{T}"/> for safe access from multiple threads.
    /// </summary>
    public class EventQueue
    {
        private readonly ConcurrentQueue<EventEnvelope> _queue;
        private readonly int _maxSize;

        /// <summary>
        /// Creates a new EventQueue with the specified maximum size.
        /// </summary>
        /// <param name="maxSize">Maximum number of events the queue can hold. Default is 1000.</param>
        public EventQueue(int maxSize = 1000)
        {
            _queue = new ConcurrentQueue<EventEnvelope>();
            _maxSize = maxSize;
        }

        /// <summary>
        /// Enqueues an event. If the queue is at or above capacity, the oldest event
        /// is dropped to make room (oldest-drop policy).
        /// </summary>
        /// <param name="envelope">The event envelope to enqueue.</param>
        public void Enqueue(EventEnvelope envelope)
        {
            while (_queue.Count >= _maxSize)
            {
                _queue.TryDequeue(out _);
            }
            _queue.Enqueue(envelope);
        }

        /// <summary>
        /// Dequeues up to <paramref name="batchSize"/> events from the queue.
        /// </summary>
        /// <param name="batchSize">Maximum number of events to dequeue. Default is 500.</param>
        /// <returns>A list of dequeued event envelopes, which may be empty if the queue is empty.</returns>
        public List<EventEnvelope> DequeueBatch(int batchSize = 500)
        {
            var batch = new List<EventEnvelope>(batchSize);
            while (batch.Count < batchSize && _queue.TryDequeue(out var item))
            {
                batch.Add(item);
            }
            return batch;
        }

        /// <summary>
        /// Gets the current number of events in the queue.
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// Gets whether the queue contains no events.
        /// </summary>
        public bool IsEmpty => _queue.IsEmpty;
    }
}
