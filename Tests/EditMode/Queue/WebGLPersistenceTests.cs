using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DataFerret.Analytics.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="WebGLPersistence"/> — PlayerPrefs-based event persistence for WebGL.
    /// </summary>
    [TestFixture]
    public class WebGLPersistenceTests
    {
        private WebGLPersistence _persistence;

        [SetUp]
        public void SetUp()
        {
            _persistence = new WebGLPersistence();
            _persistence.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _persistence.Clear();
        }

        /// <summary>
        /// Save then LoadAll returns correct events with matching EventId values.
        /// </summary>
        [Test]
        public void SaveThenLoadAll_ReturnsCorrectEvents()
        {
            var events = new List<EventEnvelope>
            {
                CreateMinimalEnvelope("webgl-evt-001"),
                CreateMinimalEnvelope("webgl-evt-002"),
                CreateMinimalEnvelope("webgl-evt-003")
            };

            _persistence.Save(events);
            var loaded = _persistence.LoadAll();

            Assert.That(loaded.Count, Is.EqualTo(3));
            Assert.That(loaded[0].EventId, Is.EqualTo("webgl-evt-001"));
            Assert.That(loaded[1].EventId, Is.EqualTo("webgl-evt-002"));
            Assert.That(loaded[2].EventId, Is.EqualTo("webgl-evt-003"));
        }

        /// <summary>
        /// MaxEntries (200) limit is respected — saving 201 events only persists 200.
        /// </summary>
        [Test]
        public void Save_RespectsMaxEntriesLimit_Of200()
        {
            var events = new List<EventEnvelope>();
            for (int i = 0; i < 201; i++)
            {
                events.Add(CreateMinimalEnvelope($"limit-evt-{i:D4}"));
            }

            _persistence.Save(events);
            var loaded = _persistence.LoadAll();

            Assert.That(loaded.Count, Is.EqualTo(200), "Only 200 events should be persisted");
            Assert.That(loaded[0].EventId, Is.EqualTo("limit-evt-0000"), "First event should be preserved");
            Assert.That(loaded[199].EventId, Is.EqualTo("limit-evt-0199"), "200th event should be preserved");
        }

        /// <summary>
        /// Clear() then LoadAll() returns an empty list.
        /// </summary>
        [Test]
        public void Clear_ThenLoadAll_ReturnsEmptyList()
        {
            // First save some events
            var events = new List<EventEnvelope>
            {
                CreateMinimalEnvelope("clear-evt-001"),
                CreateMinimalEnvelope("clear-evt-002")
            };
            _persistence.Save(events);

            // Verify they were saved
            var beforeClear = _persistence.LoadAll();
            Assert.That(beforeClear.Count, Is.EqualTo(2), "Events should be saved before Clear");

            // Clear and verify empty
            _persistence.Clear();
            var afterClear = _persistence.LoadAll();

            Assert.That(afterClear, Is.Empty, "LoadAll should return empty list after Clear");
            Assert.That(PlayerPrefs.GetInt("df_queue_count", -1), Is.EqualTo(0),
                "Count key should be reset to 0");
        }

        /// <summary>
        /// Creates a minimal EventEnvelope with just EventId and Type set.
        /// </summary>
        private static EventEnvelope CreateMinimalEnvelope(string eventId)
        {
            return new EventEnvelope
            {
                EventId = eventId,
                Type = "track"
            };
        }
    }
}
