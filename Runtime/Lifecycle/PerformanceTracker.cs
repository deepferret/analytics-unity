using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Samples performance metrics (FPS, memory, mono heap) every 30 seconds.
    /// Opt-in feature, disabled by default via AutoCaptureConfig.PerformanceSamples.
    /// Attaches as a MonoBehaviour and uses InvokeRepeating for periodic sampling.
    /// </summary>
    public class PerformanceTracker : MonoBehaviour
    {
        private const float SampleIntervalSeconds = 30f;
        private Action<string, Dictionary<string, object>> _trackEvent;

        /// <summary>
        /// Initializes the performance tracker with a tracking delegate and starts
        /// periodic sampling at 30-second intervals.
        /// </summary>
        /// <param name="trackEvent">Action to track events (eventName, properties).
        /// In production, this delegates to DataFerretAnalytics.Track.</param>
        public void Initialize(Action<string, Dictionary<string, object>> trackEvent)
        {
            _trackEvent = trackEvent;
            InvokeRepeating(nameof(Sample), SampleIntervalSeconds, SampleIntervalSeconds);
        }

        /// <summary>
        /// Collects and tracks a performance sample including FPS, total managed memory,
        /// and Mono heap size. Can be called directly for testing.
        /// </summary>
        internal void Sample()
        {
            _trackEvent?.Invoke("performance_sample", new Dictionary<string, object>
            {
                { "fps", Mathf.RoundToInt(1f / Time.unscaledDeltaTime) },
                { "memory_used_mb", GC.GetTotalMemory(false) / (1024f * 1024f) },
                { "mono_heap_mb", Profiler.GetMonoHeapSizeLong() / (1024f * 1024f) }
            });
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(Sample));
        }
    }
}
