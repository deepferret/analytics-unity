using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace DataFerret.Analytics
{
    /// <summary>
    /// WebGL-specific transport that sends event batches to the Collector API
    /// using the browser <c>fetch()</c> API via the DataFerretBridge.jslib plugin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This transport is a <see cref="MonoBehaviour"/> because it relies on Unity's
    /// <c>SendMessage</c> mechanism to receive asynchronous callbacks from JavaScript.
    /// The hosting GameObject's name is passed to the .jslib bridge so that
    /// <c>SendMessage</c> can route the response back to <see cref="OnSendBatchComplete"/>.
    /// </para>
    /// <para>
    /// For page-unload scenarios (e.g. browser tab close, navigation away),
    /// use <see cref="SendBeacon"/> which fires via <c>navigator.sendBeacon()</c>
    /// and does not wait for a response.
    /// </para>
    /// </remarks>
    public class WebGLTransport : MonoBehaviour, ITransport
    {
        private string _endpoint;
        private string _writeKey;
        private float _timeoutSeconds;
        private bool _debug;
        private bool _completed;
        private bool _success;

        /// <summary>
        /// Initializes the WebGL transport with API configuration.
        /// Must be called before <see cref="SendBatch"/> or <see cref="SendBeacon"/>.
        /// </summary>
        /// <param name="endpoint">Base API host URL (e.g. "https://collect.dataferret.io").</param>
        /// <param name="writeKey">Write key for Bearer token authentication.</param>
        /// <param name="timeoutSeconds">Maximum time in seconds to wait for a fetch response.</param>
        /// <param name="debug">When true, enables verbose logging.</param>
        public void Initialize(string endpoint, string writeKey, float timeoutSeconds = 30f, bool debug = false)
        {
            _endpoint = endpoint;
            _writeKey = writeKey;
            _timeoutSeconds = timeoutSeconds;
            _debug = debug;
        }

        /// <inheritdoc/>
        /// <summary>
        /// Sends a batch of events to the Collector API using the browser <c>fetch()</c> API.
        /// Waits for the JavaScript callback or timeout before invoking <paramref name="onComplete"/>.
        /// </summary>
        /// <param name="events">The list of event envelopes to send.</param>
        /// <param name="onComplete">Callback invoked with <c>true</c> on success,
        /// <c>false</c> on failure or timeout.</param>
        /// <returns>Coroutine enumerator for use with Unity's coroutine system.</returns>
        public IEnumerator SendBatch(List<EventEnvelope> events, Action<bool> onComplete)
        {
            var payload = JsonConvert.SerializeObject(new
            {
                batch = events,
                sentAt = DateTime.UtcNow.ToString("o")
            });

            var url = $"{_endpoint}/v1/collect/batch";
            var auth = $"Bearer {_writeKey}";

            _completed = false;
            _success = false;

            if (_debug)
            {
                Debug.Log($"[DataFerret] WebGLTransport sending batch of {events.Count} events to {url}");
            }

            WebGLBridge.DataFerret_SendBatch(url, payload, auth, gameObject.name, nameof(OnSendBatchComplete));

            float elapsed = 0f;
            while (!_completed && elapsed < _timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_completed)
            {
                if (_debug)
                {
                    Debug.LogWarning($"[DataFerret] WebGLTransport timed out after {_timeoutSeconds}s");
                }
            }

            onComplete?.Invoke(_completed && _success);
        }

        /// <summary>
        /// Sends events using <c>navigator.sendBeacon()</c> for fire-and-forget delivery.
        /// Used during page unload or visibility change when a full fetch round-trip
        /// cannot be guaranteed.
        /// </summary>
        /// <remarks>
        /// <c>sendBeacon</c> does not provide a response, so there is no success/failure callback.
        /// The browser guarantees best-effort delivery even if the page is being unloaded.
        /// Note that <c>sendBeacon</c> does not support custom headers, so the write key
        /// cannot be sent as a Bearer token. The backend must support alternative
        /// authentication for beacon requests (e.g. query parameter or payload field).
        /// </remarks>
        /// <param name="events">The list of event envelopes to send.</param>
        public void SendBeacon(List<EventEnvelope> events)
        {
            var payload = JsonConvert.SerializeObject(new
            {
                batch = events,
                sentAt = DateTime.UtcNow.ToString("o")
            });

            var url = $"{_endpoint}/v1/collect/batch";

            if (_debug)
            {
                Debug.Log($"[DataFerret] WebGLTransport sending beacon with {events.Count} events");
            }

            WebGLBridge.DataFerret_SendBeacon(url, payload);
        }

        /// <summary>
        /// Callback invoked from JavaScript via <c>SendMessage</c> when the
        /// <c>fetch()</c> request completes.
        /// </summary>
        /// <param name="result">"1" for success (HTTP 2xx), "0" for failure.</param>
        public void OnSendBatchComplete(string result)
        {
            _success = result == "1";
            _completed = true;

            if (_debug)
            {
                Debug.Log($"[DataFerret] WebGLTransport batch complete: {(_success ? "success" : "failure")}");
            }
        }
    }
}
