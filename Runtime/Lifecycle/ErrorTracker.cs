using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Automatically tracks unhandled exceptions and errors via Application.logMessageReceived.
    /// Includes 60-second deduplication and 1000-character stack trace truncation.
    /// </summary>
    public class ErrorTracker
    {
        private const float DedupWindowSeconds = 60f;
        private const int MaxStackTraceLength = 1000;

        private readonly Dictionary<string, float> _recentErrors = new Dictionary<string, float>();
        private readonly Action<string, Dictionary<string, object>> _trackEvent;
        private bool _enabled;

        /// <summary>
        /// Creates a new ErrorTracker.
        /// </summary>
        /// <param name="trackEvent">Action to track events (eventName, properties).
        /// In production, this delegates to DataFerretAnalytics.Track.</param>
        /// <param name="enabled">Whether error tracking is enabled. Defaults to true.</param>
        public ErrorTracker(Action<string, Dictionary<string, object>> trackEvent, bool enabled = true)
        {
            _trackEvent = trackEvent;
            _enabled = enabled;
        }

        /// <summary>
        /// Starts listening for log messages via Application.logMessageReceived.
        /// Call <see cref="Dispose"/> to stop listening.
        /// </summary>
        public void Initialize()
        {
            Application.logMessageReceived += OnLogMessage;
        }

        /// <summary>
        /// Stops listening for log messages and cleans up the deduplication cache.
        /// </summary>
        public void Dispose()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        /// <summary>
        /// Whether error tracking is enabled. When false, no events are tracked
        /// even if log messages are received.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Handles a log message. Only processes Exception and Error types.
        /// Applies 60-second deduplication based on the condition string and
        /// truncates stack traces exceeding 1000 characters.
        /// Exposed as internal for direct testing without subscribing to Application.logMessageReceived.
        /// </summary>
        /// <param name="condition">The log message condition string.</param>
        /// <param name="stackTrace">The stack trace associated with the log message.</param>
        /// <param name="type">The Unity LogType of the message.</param>
        internal void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!_enabled) return;

            // Only track exceptions and errors
            if (type != LogType.Exception && type != LogType.Error) return;

            // 60-second dedup based on condition string
            if (_recentErrors.TryGetValue(condition, out var lastTime))
            {
                if (Time.realtimeSinceStartup - lastTime < DedupWindowSeconds) return;
            }
            _recentErrors[condition] = Time.realtimeSinceStartup;

            // Truncate stack trace to MaxStackTraceLength
            string truncatedStack = stackTrace;
            if (stackTrace != null && stackTrace.Length > MaxStackTraceLength)
            {
                truncatedStack = stackTrace.Substring(0, MaxStackTraceLength) + "...";
            }

            _trackEvent?.Invoke("error_occurred", new Dictionary<string, object>
            {
                { "message", condition },
                { "stack_trace", truncatedStack },
                { "log_type", type.ToString() }
            });
        }
    }
}
