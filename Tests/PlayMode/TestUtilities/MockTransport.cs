using System;
using System.Collections;
using System.Collections.Generic;

namespace DataFerret.Analytics.Tests.PlayMode
{
    /// <summary>
    /// Mock implementation of <see cref="ITransport"/> for PlayMode tests.
    /// Records all sent batches and allows controlling success/failure responses,
    /// including per-attempt response sequences for retry testing.
    /// </summary>
    internal class MockTransport : ITransport
    {
        /// <summary>
        /// All batches that have been sent through this transport.
        /// </summary>
        public List<List<EventEnvelope>> SentBatches = new List<List<EventEnvelope>>();

        /// <summary>
        /// Controls whether SendBatch reports success or failure.
        /// Ignored when <see cref="ResponseSequence"/> is non-empty.
        /// </summary>
        public bool ShouldSucceed = true;

        /// <summary>
        /// When non-empty, each call to SendBatch pops the first value and uses it
        /// as the result, enabling retry-sequence simulation (e.g., fail, fail, succeed).
        /// </summary>
        public Queue<bool> ResponseSequence = new Queue<bool>();

        /// <summary>
        /// Number of times SendBatch has been called (useful for retry counting).
        /// </summary>
        public int SendBatchCallCount;

        /// <summary>
        /// Optional delay (in frames) before invoking onComplete.
        /// Set to 0 for immediate completion, > 0 to simulate async latency.
        /// </summary>
        public int DelayFrames;

        /// <inheritdoc/>
        public IEnumerator SendBatch(List<EventEnvelope> events, Action<bool> onComplete)
        {
            SendBatchCallCount++;
            SentBatches.Add(new List<EventEnvelope>(events));

            for (int i = 0; i < DelayFrames; i++)
            {
                yield return null;
            }

            bool result = ResponseSequence.Count > 0
                ? ResponseSequence.Dequeue()
                : ShouldSucceed;

            onComplete?.Invoke(result);
        }
    }
}
