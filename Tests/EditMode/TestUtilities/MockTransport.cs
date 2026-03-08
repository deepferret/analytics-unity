using System;
using System.Collections;
using System.Collections.Generic;

namespace DataFerret.Analytics.Tests.EditMode
{
    /// <summary>
    /// Mock implementation of <see cref="ITransport"/> for use in edit-mode tests.
    /// Records all sent batches and allows controlling success/failure responses.
    /// </summary>
    internal class MockTransport : ITransport
    {
        /// <summary>
        /// All batches that have been sent through this transport.
        /// </summary>
        public List<List<EventEnvelope>> SentBatches = new List<List<EventEnvelope>>();

        /// <summary>
        /// Controls whether SendBatch reports success or permanent failure.
        /// </summary>
        public bool ShouldSucceed = true;

        /// <inheritdoc/>
        public IEnumerator SendBatch(List<EventEnvelope> events, Action<bool> onComplete)
        {
            SentBatches.Add(new List<EventEnvelope>(events));
            onComplete?.Invoke(ShouldSucceed);
            yield break;
        }
    }
}
