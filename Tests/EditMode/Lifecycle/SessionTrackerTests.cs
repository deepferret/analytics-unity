using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Lifecycle")]
    public class SessionTrackerTests
    {
        private List<(string eventName, Dictionary<string, object> properties)> _trackedEvents;
        private SessionTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _trackedEvents = new List<(string, Dictionary<string, object>)>();
            _tracker = new SessionTracker((name, props) => _trackedEvents.Add((name, props)));

            // Clean up PlayerPrefs keys used by SessionTracker
            PlayerPrefs.DeleteKey("df_first_session_time");
            PlayerPrefs.DeleteKey("df_last_session_time");
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("df_first_session_time");
            PlayerPrefs.DeleteKey("df_last_session_time");
            PlayerPrefs.Save();
        }

        /// <summary>
        /// TC-ST-001: StartSession() invokes trackEvent with "session_start" and session_id.
        /// </summary>
        [Test]
        public void StartSession_InvokesTrackEvent_WithSessionStartAndSessionId()
        {
            _tracker.StartSession();

            Assert.That(_trackedEvents.Count, Is.EqualTo(1));
            Assert.That(_trackedEvents[0].eventName, Is.EqualTo("session_start"));
            Assert.That(_trackedEvents[0].properties, Contains.Key("session_id"));
            Assert.That(_trackedEvents[0].properties["session_id"], Is.Not.Null);
            Assert.That(_trackedEvents[0].properties["session_id"], Is.Not.Empty);
        }

        /// <summary>
        /// TC-ST-002: EndSession() invokes trackEvent with "session_end" and session_id matching
        /// the session_id from StartSession().
        /// </summary>
        [Test]
        public void EndSession_InvokesTrackEvent_WithSessionEndAndMatchingSessionId()
        {
            _tracker.StartSession();
            var startSessionId = _trackedEvents[0].properties["session_id"];

            _tracker.EndSession();

            Assert.That(_trackedEvents.Count, Is.EqualTo(2));
            Assert.That(_trackedEvents[1].eventName, Is.EqualTo("session_end"));
            Assert.That(_trackedEvents[1].properties, Contains.Key("session_id"));
            Assert.That(_trackedEvents[1].properties["session_id"], Is.EqualTo(startSessionId));
            Assert.That(_trackedEvents[1].properties, Contains.Key("duration_seconds"));
        }

        /// <summary>
        /// TC-ST-003: SessionId is valid GUID format after StartSession().
        /// </summary>
        [Test]
        public void StartSession_SessionId_IsValidGuidFormat()
        {
            _tracker.StartSession();

            Assert.That(_tracker.SessionId, Is.Not.Null);
            Assert.That(_tracker.IsSessionActive, Is.True);

            bool isValidGuid = Guid.TryParse(_tracker.SessionId, out _);
            Assert.That(isValidGuid, Is.True, "SessionId should be a valid GUID format");
        }

        /// <summary>
        /// TC-ST-004: OnPause(true) calls EndSession (session_end event tracked).
        /// </summary>
        [Test]
        public void OnPause_WhenPaused_TracksSessionEndEvent()
        {
            _tracker.StartSession();
            Assert.That(_trackedEvents.Count, Is.EqualTo(1));

            _tracker.OnPause(true);

            Assert.That(_trackedEvents.Count, Is.EqualTo(2));
            Assert.That(_trackedEvents[1].eventName, Is.EqualTo("session_end"));
            Assert.That(_trackedEvents[1].properties, Contains.Key("session_id"));
            Assert.That(_trackedEvents[1].properties, Contains.Key("duration_seconds"));
        }

        /// <summary>
        /// TC-ST-005: is_first_session is true on first call, false on second call.
        /// </summary>
        [Test]
        public void StartSession_IsFirstSession_TrueOnFirstCall_FalseOnSecond()
        {
            _tracker.StartSession();

            Assert.That(_trackedEvents[0].properties, Contains.Key("is_first_session"));
            Assert.That(_trackedEvents[0].properties["is_first_session"], Is.True,
                "First StartSession() should set is_first_session to true");

            // Create a new tracker (simulating app restart) but keep PlayerPrefs
            var trackedEvents2 = new List<(string eventName, Dictionary<string, object> properties)>();
            var tracker2 = new SessionTracker((name, props) => trackedEvents2.Add((name, props)));

            tracker2.StartSession();

            Assert.That(trackedEvents2[0].properties, Contains.Key("is_first_session"));
            Assert.That(trackedEvents2[0].properties["is_first_session"], Is.False,
                "Second StartSession() should set is_first_session to false");
        }
    }
}
