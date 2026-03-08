using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Singleton MonoBehaviour that manages periodic event flushing.
    /// Persists across scene loads via DontDestroyOnLoad.
    /// Handles background/quit events for data safety.
    /// </summary>
    [DisallowMultipleComponent]
    public class FlushScheduler : MonoBehaviour
    {
        private static FlushScheduler _instance;

        /// <summary>
        /// Gets the singleton instance, or null if not yet created.
        /// </summary>
        public static FlushScheduler Instance => _instance;

        private EventQueue _queue;
        private ITransport _transport;
        private DataFerretConfig _config;
        private float _lastFlushTime;
        private bool _isFlushing;

        /// <summary>
        /// Creates and initializes the FlushScheduler singleton.
        /// If an instance already exists, returns the existing one.
        /// The created GameObject is marked with DontDestroyOnLoad.
        /// </summary>
        /// <param name="queue">The event queue to flush from.</param>
        /// <param name="transport">The transport used to send event batches.</param>
        /// <param name="config">SDK configuration controlling flush behavior.</param>
        /// <returns>The singleton FlushScheduler instance.</returns>
        public static FlushScheduler Create(EventQueue queue, ITransport transport, DataFerretConfig config)
        {
            if (_instance != null)
            {
                return _instance;
            }

            var go = new GameObject("[DataFerret] FlushScheduler");
            _instance = go.AddComponent<FlushScheduler>();
            _instance._queue = queue;
            _instance._transport = transport;
            _instance._config = config;
            DontDestroyOnLoad(go);
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Update()
        {
            if (_config == null || _queue == null) return;

            // Flush when queue reaches FlushSize
            if (_queue.Count >= _config.FlushSize && !_isFlushing)
            {
                Flush();
            }

            // Periodic flush based on FlushIntervalSeconds
            if (Time.realtimeSinceStartup - _lastFlushTime >= _config.FlushIntervalSeconds)
            {
                _lastFlushTime = Time.realtimeSinceStartup;
                if (!_isFlushing)
                {
                    Flush();
                }
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused && _queue != null && !_queue.IsEmpty)
            {
                Flush();
            }
        }

        private void OnApplicationQuit()
        {
            if (_queue != null && !_queue.IsEmpty)
            {
                FlushSync();
            }
        }

        /// <summary>
        /// Triggers an asynchronous flush of queued events.
        /// Does nothing if the queue is empty, transport is null, or a flush is already in progress.
        /// </summary>
        public void Flush()
        {
            if (_queue == null || _transport == null || _queue.IsEmpty || _isFlushing) return;
            StartCoroutine(FlushCoroutine());
        }

        /// <summary>
        /// Performs a synchronous flush by running the transport coroutine to completion.
        /// Used during application quit when coroutines cannot be started.
        /// </summary>
        public void FlushSync()
        {
            if (_queue == null || _transport == null || _queue.IsEmpty) return;

            var batch = _queue.DequeueBatch(_config.FlushSize);
            if (batch.Count == 0) return;

            // Run coroutine synchronously
            var enumerator = _transport.SendBatch(batch, success =>
            {
                if (!success && _config.Debug)
                {
                    Debug.LogWarning("[DataFerret] FlushSync: batch send failed.");
                }
            });

            while (enumerator.MoveNext()) { }
        }

        private IEnumerator FlushCoroutine()
        {
            _isFlushing = true;

            while (!_queue.IsEmpty)
            {
                var batch = _queue.DequeueBatch(_config.FlushSize);
                if (batch.Count == 0) break;

                bool? result = null;
                yield return _transport.SendBatch(batch, success =>
                {
                    result = success;
                });

                if (result == false)
                {
                    if (_config.Debug)
                    {
                        Debug.LogWarning($"[DataFerret] Flush failed for batch of {batch.Count} events.");
                    }
                    break; // Stop flushing on failure, events remain for next attempt
                }
            }

            _isFlushing = false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Destroys the singleton instance and its GameObject.
        /// Used for test cleanup between test runs.
        /// </summary>
        internal static void DestroyInstance()
        {
            if (_instance != null)
            {
                DestroyImmediate(_instance.gameObject);
                _instance = null;
            }
        }
    }
}
