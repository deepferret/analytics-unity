using System.Collections.Generic;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Lifecycle")]
    public class LifecycleTrackerTests
    {
        private List<(string eventName, Dictionary<string, object> properties)> _trackedEvents;
        private LifecycleTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _trackedEvents = new List<(string, Dictionary<string, object>)>();
            _tracker = new LifecycleTracker(
                (eventName, props) => _trackedEvents.Add((eventName, props)),
                enabled: true
            );
        }

        /// <summary>
        /// TC-LT-001: OnPause(true) tracks "app_backgrounded" with session_length property.
        /// </summary>
        [Test]
        public void OnPause_True_TracksAppBackgrounded_WithSessionLength()
        {
            _tracker.OnPause(true);

            Assert.That(_trackedEvents.Count, Is.EqualTo(1));
            Assert.That(_trackedEvents[0].eventName, Is.EqualTo("app_backgrounded"));
            Assert.That(_trackedEvents[0].properties, Contains.Key("session_length"));
            Assert.That(_trackedEvents[0].properties["session_length"], Is.TypeOf<int>());
        }

        /// <summary>
        /// TC-LT-002: OnPause(false) tracks "app_foregrounded" with background_duration_seconds property.
        /// </summary>
        [Test]
        public void OnPause_False_TracksAppForegrounded_WithBackgroundDuration()
        {
            _tracker.OnPause(false);

            Assert.That(_trackedEvents.Count, Is.EqualTo(1));
            Assert.That(_trackedEvents[0].eventName, Is.EqualTo("app_foregrounded"));
            Assert.That(_trackedEvents[0].properties, Contains.Key("background_duration_seconds"));
            Assert.That(_trackedEvents[0].properties["background_duration_seconds"], Is.TypeOf<int>());
        }

        /// <summary>
        /// TC-LT-003: OnPause(true) then OnPause(false) yields background_duration_seconds >= 0.
        /// </summary>
        [Test]
        public void OnPause_TrueThenFalse_BackgroundDurationIsNonNegative()
        {
            _tracker.OnPause(true);
            _tracker.OnPause(false);

            Assert.That(_trackedEvents.Count, Is.EqualTo(2));

            // First event: app_backgrounded
            Assert.That(_trackedEvents[0].eventName, Is.EqualTo("app_backgrounded"));

            // Second event: app_foregrounded with non-negative duration
            Assert.That(_trackedEvents[1].eventName, Is.EqualTo("app_foregrounded"));
            var duration = (int)_trackedEvents[1].properties["background_duration_seconds"];
            Assert.That(duration, Is.GreaterThanOrEqualTo(0));
        }

        /// <summary>
        /// TC-LT-004: When Enabled=false, OnPause does NOT invoke trackEvent.
        /// </summary>
        [Test]
        public void OnPause_WhenDisabled_DoesNotInvokeTrackEvent()
        {
            _tracker.Enabled = false;

            _tracker.OnPause(true);
            _tracker.OnPause(false);

            Assert.That(_trackedEvents.Count, Is.EqualTo(0));
        }
    }
}
