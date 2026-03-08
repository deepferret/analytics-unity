using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Tracks application backgrounded and foregrounded events.
    /// Designed to be called from <see cref="MonoBehaviour.OnApplicationPause"/>
    /// on the FlushScheduler or similar MonoBehaviour.
    /// </summary>
    public class LifecycleTracker
    {
        private float _backgroundStartTime = -1f;
        private readonly Action<string, Dictionary<string, object>> _trackEvent;
        private bool _enabled;

        /// <summary>
        /// Creates a new LifecycleTracker.
        /// </summary>
        /// <param name="trackEvent">
        /// Action invoked to track lifecycle events. Receives the event name
        /// (<c>app_backgrounded</c> or <c>app_foregrounded</c>) and a properties dictionary.
        /// </param>
        /// <param name="enabled">Whether lifecycle tracking is enabled.</param>
        public LifecycleTracker(Action<string, Dictionary<string, object>> trackEvent, bool enabled = true)
        {
            _trackEvent = trackEvent;
            _enabled = enabled;
        }

        /// <summary>
        /// Whether lifecycle tracking is enabled. When disabled, pause/resume
        /// events are silently ignored.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Handles application pause and resume events.
        /// When <paramref name="isPaused"/> is <c>true</c>, tracks an <c>app_backgrounded</c>
        /// event with <c>session_length</c> (seconds since app start).
        /// When <paramref name="isPaused"/> is <c>false</c>, tracks an <c>app_foregrounded</c>
        /// event with <c>background_duration_seconds</c>.
        /// </summary>
        /// <param name="isPaused">
        /// <c>true</c> when the application enters the background;
        /// <c>false</c> when it returns to the foreground.
        /// </param>
        public void OnPause(bool isPaused)
        {
            if (!_enabled) return;

            if (isPaused)
            {
                _backgroundStartTime = Time.realtimeSinceStartup;
                _trackEvent?.Invoke("app_backgrounded", new Dictionary<string, object>
                {
                    { "session_length", Mathf.RoundToInt(Time.realtimeSinceStartup) }
                });
            }
            else
            {
                var backgroundDuration = _backgroundStartTime >= 0f
                    ? Time.realtimeSinceStartup - _backgroundStartTime
                    : 0f;

                _trackEvent?.Invoke("app_foregrounded", new Dictionary<string, object>
                {
                    { "background_duration_seconds", Mathf.RoundToInt(backgroundDuration) }
                });
                _backgroundStartTime = -1f;
            }
        }
    }
}
