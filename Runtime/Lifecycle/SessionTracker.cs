using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Tracks session start/end events with 30-minute timeout.
    /// Sessions are identified by a GUID and include metrics like duration.
    /// </summary>
    public class SessionTracker
    {
        private const float SessionTimeoutSeconds = 30f * 60f; // 30 minutes
        private const string FirstSessionKey = "df_first_session_time";
        private const string LastSessionKey = "df_last_session_time";

        private string _sessionId;
        private float _sessionStartTime;
        private float _backgroundStartTime = -1f;
        private readonly Action<string, Dictionary<string, object>> _trackEvent;

        /// <summary>
        /// Current session ID. Null if no session is active.
        /// </summary>
        public string SessionId => _sessionId;

        /// <summary>
        /// Whether a session is currently active.
        /// </summary>
        public bool IsSessionActive => _sessionId != null;

        /// <summary>
        /// Creates a new SessionTracker.
        /// </summary>
        /// <param name="trackEvent">Action to track events (eventName, properties).
        /// In production, this delegates to DataFerretAnalytics.Track.</param>
        public SessionTracker(Action<string, Dictionary<string, object>> trackEvent)
        {
            _trackEvent = trackEvent;
        }

        /// <summary>
        /// Starts a new session. Generates a new GUID session ID and tracks a
        /// "session_start" event with session metadata including first-session
        /// detection and days-since metrics.
        /// </summary>
        public void StartSession()
        {
            _sessionId = Guid.NewGuid().ToString();
            _sessionStartTime = Time.realtimeSinceStartup;

            var firstSessionTime = PlayerPrefs.GetFloat(FirstSessionKey, 0f);
            bool isFirstSession = firstSessionTime == 0f;
            if (isFirstSession)
            {
                PlayerPrefs.SetFloat(FirstSessionKey, (float)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }

            var lastSessionTime = PlayerPrefs.GetFloat(LastSessionKey, 0f);
            float daysSinceLastSession = 0f;
            if (lastSessionTime > 0f)
            {
                daysSinceLastSession = (float)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastSessionTime) / 86400f;
            }

            float daysSinceInstall = 0f;
            if (firstSessionTime > 0f)
            {
                daysSinceInstall = (float)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - firstSessionTime) / 86400f;
            }

            _trackEvent?.Invoke("session_start", new Dictionary<string, object>
            {
                { "session_id", _sessionId },
                { "is_first_session", isFirstSession },
                { "days_since_install", Mathf.RoundToInt(daysSinceInstall) },
                { "days_since_last_session", Mathf.RoundToInt(daysSinceLastSession) }
            });

            PlayerPrefs.SetFloat(LastSessionKey, (float)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Ends the current session. Tracks a "session_end" event with the session ID
        /// and duration in seconds. Does nothing if no session is active.
        /// </summary>
        public void EndSession()
        {
            if (_sessionId == null) return;

            var duration = Time.realtimeSinceStartup - _sessionStartTime;
            _trackEvent?.Invoke("session_end", new Dictionary<string, object>
            {
                { "session_id", _sessionId },
                { "duration_seconds", Mathf.RoundToInt(duration) }
            });
        }

        /// <summary>
        /// Handles app pause/resume. Ends the session on pause. On resume, starts a
        /// new session if the background duration exceeded the 30-minute timeout.
        /// </summary>
        /// <param name="isPaused">True when the application is pausing, false when resuming.</param>
        public void OnPause(bool isPaused)
        {
            if (isPaused)
            {
                _backgroundStartTime = Time.realtimeSinceStartup;
                EndSession();
            }
            else
            {
                if (_backgroundStartTime >= 0f)
                {
                    var backgroundDuration = Time.realtimeSinceStartup - _backgroundStartTime;
                    if (backgroundDuration >= SessionTimeoutSeconds)
                    {
                        StartSession(); // New session after timeout
                    }
                    _backgroundStartTime = -1f;
                }
            }
        }
    }
}
