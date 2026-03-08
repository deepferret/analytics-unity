using System;
using System.Collections;
using System.Collections.Generic;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Abstraction for HTTP transport to the Collector API.
    /// Platform-specific implementations handle retry and error logic.
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Sends a batch of events to the Collector API.
        /// </summary>
        /// <param name="events">Events to send.</param>
        /// <param name="onComplete">Callback invoked on completion: true indicates success,
        /// false indicates a permanent failure (no further retries).</param>
        /// <returns>Coroutine enumerator for use with Unity's coroutine system.</returns>
        IEnumerator SendBatch(List<EventEnvelope> events, Action<bool> onComplete);
    }
}
