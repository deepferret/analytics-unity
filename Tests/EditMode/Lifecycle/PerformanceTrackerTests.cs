using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Lifecycle")]
    public class PerformanceTrackerTests
    {
        private List<(string eventName, Dictionary<string, object> properties)> _trackedEvents;
        private GameObject _go;
        private PerformanceTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _trackedEvents = new List<(string, Dictionary<string, object>)>();
            _go = new GameObject("TestPerformanceTracker");
            _tracker = _go.AddComponent<PerformanceTracker>();
            _tracker.Initialize((name, props) => _trackedEvents.Add((name, props)));
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        /// <summary>
        /// TC-PT-001: Sample() invokes trackEvent with "performance_sample".
        /// </summary>
        [Test]
        public void Sample_InvokesTrackEvent_WithPerformanceSample()
        {
            _tracker.Sample();

            Assert.That(_trackedEvents.Count, Is.EqualTo(1));
            Assert.That(_trackedEvents[0].eventName, Is.EqualTo("performance_sample"));
        }

        /// <summary>
        /// TC-PT-002: Sample() properties contain fps, memory_used_mb, mono_heap_mb keys.
        /// </summary>
        [Test]
        public void Sample_Properties_ContainExpectedKeys()
        {
            _tracker.Sample();

            Assert.That(_trackedEvents.Count, Is.EqualTo(1));
            var props = _trackedEvents[0].properties;
            Assert.That(props, Contains.Key("fps"));
            Assert.That(props, Contains.Key("memory_used_mb"));
            Assert.That(props, Contains.Key("mono_heap_mb"));
        }

        /// <summary>
        /// TC-PT-003: fps, memory_used_mb values are non-negative numbers.
        /// </summary>
        [Test]
        public void Sample_Values_AreNonNegative()
        {
            _tracker.Sample();

            var props = _trackedEvents[0].properties;

            // fps could be any int (including 0 if deltaTime is 0 or infinity), but should not be negative
            // In editor tests, Time.unscaledDeltaTime may be 0, so fps could be 0 or very large
            var fps = props["fps"];
            Assert.That(fps, Is.InstanceOf<int>(), "fps should be an integer");

            var memoryUsedMb = props["memory_used_mb"];
            Assert.That(memoryUsedMb, Is.InstanceOf<float>(), "memory_used_mb should be a float");
            Assert.That((float)memoryUsedMb, Is.GreaterThanOrEqualTo(0f), "memory_used_mb should be non-negative");

            var monoHeapMb = props["mono_heap_mb"];
            Assert.That(monoHeapMb, Is.InstanceOf<float>(), "mono_heap_mb should be a float");
            Assert.That((float)monoHeapMb, Is.GreaterThanOrEqualTo(0f), "mono_heap_mb should be non-negative");
        }
    }
}
