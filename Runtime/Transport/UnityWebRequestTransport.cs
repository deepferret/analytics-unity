using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace DataFerret.Analytics
{
    /// <summary>
    /// HTTP transport using UnityWebRequest with exponential backoff retry.
    /// Used on all platforms except WebGL.
    /// </summary>
    public class UnityWebRequestTransport : ITransport
    {
        private readonly string _endpoint;
        private readonly string _writeKey;
        private readonly int _timeoutSeconds;
        private readonly int _maxRetries;

        /// <summary>
        /// Creates a new UnityWebRequestTransport.
        /// </summary>
        /// <param name="endpoint">API host URL (e.g., "https://collect.dataferret.io").</param>
        /// <param name="writeKey">Write key for authentication.</param>
        /// <param name="timeoutSeconds">HTTP request timeout in seconds. Default is 10.</param>
        /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
        public UnityWebRequestTransport(string endpoint, string writeKey, int timeoutSeconds = 10, int maxRetries = 3)
        {
            _endpoint = endpoint;
            _writeKey = writeKey;
            _timeoutSeconds = timeoutSeconds;
            _maxRetries = maxRetries;
        }

        /// <summary>
        /// Builds the JSON payload for a batch of events.
        /// </summary>
        /// <param name="events">The events to include in the batch payload.</param>
        /// <returns>A JSON string containing the batch array and sentAt timestamp.</returns>
        internal string BuildPayload(List<EventEnvelope> events)
        {
            return JsonConvert.SerializeObject(new
            {
                batch = events,
                sentAt = DateTime.UtcNow.ToString("o")
            });
        }

        /// <inheritdoc/>
        public IEnumerator SendBatch(List<EventEnvelope> events, Action<bool> onComplete)
        {
            var payload = BuildPayload(events);
            var url = $"{_endpoint}/v1/collect/batch";

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                // Exponential backoff: 1s, 2s, 4s
                if (attempt > 0)
                {
                    yield return new WaitForSecondsRealtime(Mathf.Pow(2f, attempt - 1));
                }

                using (var request = new UnityWebRequest(url, "POST"))
                {
                    var bodyRaw = Encoding.UTF8.GetBytes(payload);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Authorization", $"Bearer {_writeKey}");
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = _timeoutSeconds;

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        onComplete?.Invoke(true);
                        yield break;
                    }

                    var responseCode = request.responseCode;

                    // Permanent failures: no retry
                    if (responseCode == 401 || responseCode == 413)
                    {
                        Debug.LogError($"[DataFerret] Permanent failure ({responseCode}): {request.error}");
                        onComplete?.Invoke(false);
                        yield break;
                    }

                    // Log retry attempt
                    if (attempt < _maxRetries)
                    {
                        Debug.LogWarning($"[DataFerret] Retry {attempt + 1}/{_maxRetries} after error ({responseCode}): {request.error}");
                    }
                }
            }

            // All retries exhausted
            Debug.LogError($"[DataFerret] All {_maxRetries} retries exhausted for batch of {events.Count} events.");
            onComplete?.Invoke(false);
        }
    }
}
