using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace DataFerret.Analytics.Tests.EditMode
{
    /// <summary>
    /// Tests to verify GC allocation targets are met for hot-path operations.
    /// Target: Track() call should produce less than 1KB of GC allocations after warm-up.
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class GCAllocationTests
    {
        private const string AnonymousIdKey = "df_anonymous_id";
        private const int WarmUpIterations = 5;
        private const int MeasureIterations = 10;
        private const long MaxAllowedBytesPerCall = 1024; // 1KB target

        private IdentityManager _identityManager;
        private EventBuilder _eventBuilder;
        private EventQueue _queue;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(AnonymousIdKey);
            EventBuilder.ResetStaticCache();
            _identityManager = new IdentityManager();
            _eventBuilder = new EventBuilder(_identityManager);
            _queue = new EventQueue(1000);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(AnonymousIdKey);
            EventBuilder.ResetStaticCache();
        }

        /// <summary>
        /// TC-GC-001: Track (BuildTrack + Enqueue) produces less than 1KB GC allocation
        /// per call after warm-up. Measures the hot path: BuildTrack -> Enqueue.
        /// </summary>
        [Test]
        public void Track_GCAlloc_LessThan1KB()
        {
            // Pre-allocate properties dictionary (simulating typical Track call)
            var properties = new Dictionary<string, object>
            {
                { "button", "play" },
                { "level", 3 },
                { "score", 1500.5 }
            };

            // Warm up: first calls allocate caches, ThreadStatic arrays, pool envelopes, etc.
            for (int i = 0; i < WarmUpIterations; i++)
            {
                var warmUpEnvelope = _eventBuilder.BuildTrack("warm_up", properties);
                _queue.Enqueue(warmUpEnvelope);
                // Return envelopes to pool so they can be reused
                _eventBuilder.ReturnEnvelope(warmUpEnvelope);
            }

            // Drain the queue from warm-up
            _queue.DequeueBatch(WarmUpIterations);

            // Force a GC collection to get a clean baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Measure GC allocations for subsequent calls
            long totalAllocated = 0;

            for (int i = 0; i < MeasureIterations; i++)
            {
                long before = Profiler.GetTotalAllocatedMemoryLong();

                var envelope = _eventBuilder.BuildTrack("measured_event", properties);
                _queue.Enqueue(envelope);

                long after = Profiler.GetTotalAllocatedMemoryLong();
                long delta = after - before;

                // Return envelope to pool for reuse in next iteration
                _eventBuilder.ReturnEnvelope(envelope);

                if (delta > 0)
                {
                    totalAllocated += delta;
                }
            }

            // Drain the measurement queue
            _queue.DequeueBatch(MeasureIterations);

            long averagePerCall = totalAllocated / MeasureIterations;

            // Log the results for profiling visibility
            Debug.Log($"[GCAllocationTest] Total GC across {MeasureIterations} calls: {totalAllocated} bytes");
            Debug.Log($"[GCAllocationTest] Average GC per Track() call: {averagePerCall} bytes (target: <{MaxAllowedBytesPerCall} bytes)");

            Assert.That(averagePerCall, Is.LessThan(MaxAllowedBytesPerCall),
                $"Average GC allocation per Track() call was {averagePerCall} bytes, exceeding the {MaxAllowedBytesPerCall}-byte target. " +
                $"Total: {totalAllocated} bytes across {MeasureIterations} calls.");
        }

        /// <summary>
        /// TC-GC-002: Track with null properties produces less than 1KB GC allocation.
        /// This tests the simplest hot path with no property merging overhead.
        /// </summary>
        [Test]
        public void Track_NullProperties_GCAlloc_LessThan1KB()
        {
            // Warm up
            for (int i = 0; i < WarmUpIterations; i++)
            {
                var warmUpEnvelope = _eventBuilder.BuildTrack("warm_up", null);
                _queue.Enqueue(warmUpEnvelope);
                _eventBuilder.ReturnEnvelope(warmUpEnvelope);
            }
            _queue.DequeueBatch(WarmUpIterations);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long totalAllocated = 0;

            for (int i = 0; i < MeasureIterations; i++)
            {
                long before = Profiler.GetTotalAllocatedMemoryLong();

                var envelope = _eventBuilder.BuildTrack("null_props_event", null);
                _queue.Enqueue(envelope);

                long after = Profiler.GetTotalAllocatedMemoryLong();
                long delta = after - before;

                _eventBuilder.ReturnEnvelope(envelope);

                if (delta > 0)
                {
                    totalAllocated += delta;
                }
            }

            _queue.DequeueBatch(MeasureIterations);

            long averagePerCall = totalAllocated / MeasureIterations;

            Debug.Log($"[GCAllocationTest] Null props - Average GC per Track() call: {averagePerCall} bytes");

            Assert.That(averagePerCall, Is.LessThan(MaxAllowedBytesPerCall),
                $"Average GC allocation per Track(null) call was {averagePerCall} bytes, exceeding the {MaxAllowedBytesPerCall}-byte target.");
        }

        /// <summary>
        /// TC-GC-003: GenerateUUIDv7 reuses byte arrays after warm-up (ThreadStatic optimization).
        /// </summary>
        [Test]
        public void GenerateUUIDv7_AfterWarmUp_MinimalAllocation()
        {
            // Warm up ThreadStatic arrays
            for (int i = 0; i < 3; i++)
            {
                EventBuilder.GenerateUUIDv7();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long totalAllocated = 0;
            const int iterations = 20;

            for (int i = 0; i < iterations; i++)
            {
                long before = Profiler.GetTotalAllocatedMemoryLong();
                string uuid = EventBuilder.GenerateUUIDv7();
                long after = Profiler.GetTotalAllocatedMemoryLong();

                long delta = after - before;
                if (delta > 0)
                {
                    totalAllocated += delta;
                }

                // Verify the UUID is still valid
                Assert.That(Guid.TryParse(uuid, out _), Is.True,
                    $"UUID '{uuid}' should be valid after optimization");
            }

            long averagePerCall = totalAllocated / iterations;

            // After warm-up, the only allocation should be the returned string (~72 bytes for UUID string)
            // and the Guid.ToString() allocation. No byte[] allocations.
            Debug.Log($"[GCAllocationTest] UUID generation - Average per call: {averagePerCall} bytes");

            // UUID string is ~36 chars = ~72 bytes + string overhead. Should be well under 200 bytes.
            Assert.That(averagePerCall, Is.LessThan(200),
                $"UUID generation should allocate less than 200 bytes per call (was {averagePerCall} bytes). " +
                "Byte arrays should be reused via ThreadStatic.");
        }

        /// <summary>
        /// TC-GC-004: BuildContext caches DeviceContext and LibraryContext (no per-call allocation).
        /// </summary>
        [Test]
        public void BuildContext_CachesDeviceAndLibrary()
        {
            // Build two envelopes and verify they share the same DeviceContext and LibraryContext references
            var envelope1 = _eventBuilder.BuildTrack("test1", null);
            var envelope2 = _eventBuilder.BuildTrack("test2", null);

            // LibraryContext should be the same static instance
            Assert.That(ReferenceEquals(envelope1.Context.Library, envelope2.Context.Library), Is.True,
                "LibraryContext should be a cached static instance shared across events");

            // DeviceContext should be the same cached instance
            if (envelope1.Context.Device != null && envelope2.Context.Device != null)
            {
                Assert.That(ReferenceEquals(envelope1.Context.Device, envelope2.Context.Device), Is.True,
                    "DeviceContext should be a cached static instance shared across events");
            }
        }

        /// <summary>
        /// TC-GC-005: MergeAndTruncateProperties with no globals returns the original dictionary
        /// (zero-copy when no merging needed and count is within limit).
        /// </summary>
        [Test]
        public void MergeProperties_NoGlobals_ReturnsOriginalDictionary()
        {
            var properties = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            };

            var envelope = _eventBuilder.BuildTrack("test", properties);

            // With no global properties set and count <= 200, should return the same reference
            Assert.That(ReferenceEquals(envelope.Properties, properties), Is.True,
                "Without global properties, the original dictionary should be returned directly (zero-copy)");
        }

        /// <summary>
        /// TC-GC-006: Object pooling correctly reuses EventContext and GameContext objects.
        /// </summary>
        [Test]
        public void ObjectPooling_ReusesContextObjects()
        {
            var envelope1 = _eventBuilder.BuildTrack("first", null);
            var context1 = envelope1.Context;
            var game1 = envelope1.Context?.Game;

            // Return to pool
            _eventBuilder.ReturnEnvelope(envelope1);

            // Next build should reuse pooled context
            var envelope2 = _eventBuilder.BuildTrack("second", null);

            // The envelope itself should be reused
            Assert.That(ReferenceEquals(envelope1, envelope2), Is.True,
                "Envelope should be reused from pool");

            // The EventContext should be reused from pool
            Assert.That(ReferenceEquals(context1, envelope2.Context), Is.True,
                "EventContext should be reused from pool");

            // The GameContext should be reused from pool (if both are non-null)
            if (game1 != null && envelope2.Context?.Game != null)
            {
                Assert.That(ReferenceEquals(game1, envelope2.Context.Game), Is.True,
                    "GameContext should be reused from pool");
            }
        }
    }
}
