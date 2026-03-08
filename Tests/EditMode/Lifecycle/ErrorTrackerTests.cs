using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Lifecycle")]
    public class ErrorTrackerTests
    {
        private List<(string eventName, Dictionary<string, object> properties)> _trackedEvents;
        private ErrorTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _trackedEvents = new List<(string, Dictionary<string, object>)>();
            _tracker = new ErrorTracker((name, props) => _trackedEvents.Add((name, props)));
        }

        /// <summary>
        /// TC-ET-001: OnLogMessage with LogType.Exception tracks "error_occurred" with message.
        /// </summary>
        [Test]
        public void OnLogMessage_Exception_TracksErrorOccurredWithMessage()
        {
            _tracker.OnLogMessage("NullReferenceException: Object reference not set", "at Foo.Bar()", LogType.Exception);

            Assert.That(_trackedEvents.Count, Is.EqualTo(1));
            Assert.That(_trackedEvents[0].eventName, Is.EqualTo("error_occurred"));
            Assert.That(_trackedEvents[0].properties, Contains.Key("message"));
            Assert.That(_trackedEvents[0].properties["message"], Is.EqualTo("NullReferenceException: Object reference not set"));
            Assert.That(_trackedEvents[0].properties, Contains.Key("stack_trace"));
            Assert.That(_trackedEvents[0].properties, Contains.Key("log_type"));
            Assert.That(_trackedEvents[0].properties["log_type"], Is.EqualTo("Exception"));
        }

        /// <summary>
        /// TC-ET-002: OnLogMessage with LogType.Warning does NOT track (only Exception/Error).
        /// </summary>
        [Test]
        public void OnLogMessage_Warning_DoesNotTrack()
        {
            _tracker.OnLogMessage("Some warning message", "at Foo.Bar()", LogType.Warning);

            Assert.That(_trackedEvents.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// TC-ET-003: Same error message within 60 seconds is deduplicated (only tracked once).
        /// </summary>
        [Test]
        public void OnLogMessage_SameMessageTwice_DeduplicatesWithinWindow()
        {
            _tracker.OnLogMessage("NullReferenceException: Object not set", "at Foo.Bar()", LogType.Exception);
            _tracker.OnLogMessage("NullReferenceException: Object not set", "at Foo.Bar()", LogType.Exception);

            Assert.That(_trackedEvents.Count, Is.EqualTo(1),
                "Same error message within dedup window should only be tracked once");
        }

        /// <summary>
        /// TC-ET-004: stackTrace over 1000 chars is truncated to 1000 + "...".
        /// </summary>
        [Test]
        public void OnLogMessage_LongStackTrace_TruncatedTo1000PlusEllipsis()
        {
            var longStackTrace = new string('x', 1500);

            _tracker.OnLogMessage("SomeException", longStackTrace, LogType.Exception);

            Assert.That(_trackedEvents.Count, Is.EqualTo(1));
            var stackTrace = _trackedEvents[0].properties["stack_trace"] as string;
            Assert.That(stackTrace, Is.Not.Null);
            Assert.That(stackTrace.Length, Is.EqualTo(1003), "Truncated stack trace should be 1000 chars + '...'");
            Assert.That(stackTrace.EndsWith("..."), Is.True, "Truncated stack trace should end with '...'");
        }

        /// <summary>
        /// TC-ET-005: When Enabled=false, OnLogMessage does NOT invoke trackEvent.
        /// </summary>
        [Test]
        public void OnLogMessage_WhenDisabled_DoesNotInvokeTrackEvent()
        {
            _tracker.Enabled = false;

            _tracker.OnLogMessage("SomeException", "at Foo.Bar()", LogType.Exception);

            Assert.That(_trackedEvents.Count, Is.EqualTo(0),
                "ErrorTracker should not track events when disabled");
        }
    }
}
