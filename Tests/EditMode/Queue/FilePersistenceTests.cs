using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="FilePersistence"/> — .jsonl file-based event persistence.
    /// Uses temporary directories to avoid polluting Application.persistentDataPath.
    /// </summary>
    [TestFixture]
    public class FilePersistenceTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "dataferret_test_" + Guid.NewGuid());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        /// <summary>
        /// TC-FP-001: Save() creates a .jsonl file in the dataferret subdirectory.
        /// </summary>
        [Test]
        public void Save_CreatesJsonlFileInDirectory()
        {
            var persistence = new FilePersistence(basePath: _tempDir);
            var events = new List<EventEnvelope>
            {
                CreateMinimalEnvelope("evt-001")
            };

            persistence.Save(events);

            var dataferretDir = Path.Combine(_tempDir, "dataferret");
            Assert.That(Directory.Exists(dataferretDir), Is.True, "dataferret subdirectory should exist");

            var files = Directory.GetFiles(dataferretDir, "*.jsonl");
            Assert.That(files.Length, Is.EqualTo(1), "Exactly one .jsonl file should be created");
            Assert.That(new FileInfo(files[0]).Length, Is.GreaterThan(0), "File should not be empty");
        }

        /// <summary>
        /// TC-FP-002: LoadAll() restores saved events with matching EventId values.
        /// </summary>
        [Test]
        public void LoadAll_RestoresSavedEvents_WithMatchingEventIds()
        {
            var persistence = new FilePersistence(basePath: _tempDir);
            var originalEvents = new List<EventEnvelope>
            {
                CreateMinimalEnvelope("evt-001"),
                CreateMinimalEnvelope("evt-002"),
                CreateMinimalEnvelope("evt-003")
            };

            persistence.Save(originalEvents);
            var loaded = persistence.LoadAll();

            Assert.That(loaded.Count, Is.EqualTo(3));
            Assert.That(loaded[0].EventId, Is.EqualTo("evt-001"));
            Assert.That(loaded[1].EventId, Is.EqualTo("evt-002"));
            Assert.That(loaded[2].EventId, Is.EqualTo("evt-003"));
        }

        /// <summary>
        /// TC-FP-003: When total file size exceeds the 5MB limit, oldest files are deleted.
        /// Uses a small maxTotalBytes to make the test practical.
        /// </summary>
        [Test]
        public void Save_EnforcesMaxSize_DeletesOldestFiles()
        {
            // Use a very small limit (500 bytes) so we can trigger eviction easily
            var persistence = new FilePersistence(basePath: _tempDir, maxTotalBytes: 500);

            // Save first batch — should create file 1
            var batch1 = new List<EventEnvelope>();
            for (int i = 0; i < 5; i++)
            {
                batch1.Add(CreateMinimalEnvelope($"batch1-evt-{i}"));
            }
            persistence.Save(batch1);

            // Small delay to ensure different timestamp in filename
            System.Threading.Thread.Sleep(5);

            // Save second batch — should create file 2
            var batch2 = new List<EventEnvelope>();
            for (int i = 0; i < 5; i++)
            {
                batch2.Add(CreateMinimalEnvelope($"batch2-evt-{i}"));
            }
            persistence.Save(batch2);

            System.Threading.Thread.Sleep(5);

            // Save third batch — this should push us over the limit and evict oldest
            var batch3 = new List<EventEnvelope>();
            for (int i = 0; i < 5; i++)
            {
                batch3.Add(CreateMinimalEnvelope($"batch3-evt-{i}"));
            }
            persistence.Save(batch3);

            var dataferretDir = Path.Combine(_tempDir, "dataferret");
            var remainingFiles = Directory.GetFiles(dataferretDir, "*.jsonl");

            // At least the oldest file should have been deleted
            // Total size of remaining files should be within limit
            long totalSize = 0;
            foreach (var file in remainingFiles)
            {
                totalSize += new FileInfo(file).Length;
            }
            Assert.That(totalSize, Is.LessThanOrEqualTo(500),
                "Total remaining file size should be within the max limit");

            // The latest batch should still be loadable
            var loaded = persistence.LoadAll();
            bool hasLatestBatch = false;
            foreach (var evt in loaded)
            {
                if (evt.EventId != null && evt.EventId.StartsWith("batch3"))
                {
                    hasLatestBatch = true;
                    break;
                }
            }
            Assert.That(hasLatestBatch, Is.True, "Most recent batch events should survive eviction");
        }

        /// <summary>
        /// TC-FP-004: Corrupted lines are skipped while valid lines are still loaded.
        /// </summary>
        [Test]
        public void LoadAll_SkipsCorruptedLines_LoadsValidOnes()
        {
            // Manually create the directory and write a file with mixed valid/invalid lines
            var dataferretDir = Path.Combine(_tempDir, "dataferret");
            Directory.CreateDirectory(dataferretDir);

            var filePath = Path.Combine(dataferretDir, "dataferret_000001.jsonl");
            var validEnvelope = CreateMinimalEnvelope("valid-evt-001");
            var validJson = Newtonsoft.Json.JsonConvert.SerializeObject(validEnvelope);

            var lines = new[]
            {
                validJson,
                "THIS IS NOT VALID JSON {{{",
                Newtonsoft.Json.JsonConvert.SerializeObject(CreateMinimalEnvelope("valid-evt-002")),
                "{\"broken\": true, \"missing_fields\": }",
                Newtonsoft.Json.JsonConvert.SerializeObject(CreateMinimalEnvelope("valid-evt-003"))
            };
            File.WriteAllLines(filePath, lines);

            var persistence = new FilePersistence(basePath: _tempDir);
            var loaded = persistence.LoadAll();

            Assert.That(loaded.Count, Is.EqualTo(3), "Should load 3 valid events and skip 2 corrupted lines");
            Assert.That(loaded[0].EventId, Is.EqualTo("valid-evt-001"));
            Assert.That(loaded[1].EventId, Is.EqualTo("valid-evt-002"));
            Assert.That(loaded[2].EventId, Is.EqualTo("valid-evt-003"));
        }

        /// <summary>
        /// TC-FP-005: Save() auto-creates the directory when it does not exist.
        /// </summary>
        [Test]
        public void Save_AutoCreatesDirectory_WhenNotExists()
        {
            // Ensure the base temp directory doesn't have a dataferret subdirectory
            var dataferretDir = Path.Combine(_tempDir, "dataferret");
            Assert.That(Directory.Exists(dataferretDir), Is.False, "Directory should not exist before Save");

            var persistence = new FilePersistence(basePath: _tempDir);
            persistence.Save(new List<EventEnvelope> { CreateMinimalEnvelope("evt-auto") });

            Assert.That(Directory.Exists(dataferretDir), Is.True, "Directory should be auto-created by Save");

            var files = Directory.GetFiles(dataferretDir, "*.jsonl");
            Assert.That(files.Length, Is.EqualTo(1));
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
