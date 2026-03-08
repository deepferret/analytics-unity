using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Persists event queue to .jsonl files for offline recovery.
    /// Files are stored in Application.persistentDataPath/dataferret/.
    /// Enforces a configurable maximum total size limit (default 5MB).
    /// </summary>
    public class FilePersistence
    {
        private readonly string _directory;
        private readonly long _maxTotalBytes;

        /// <summary>
        /// Creates a new FilePersistence instance.
        /// </summary>
        /// <param name="basePath">Base directory for persistence storage. Defaults to Application.persistentDataPath.</param>
        /// <param name="maxTotalBytes">Maximum total file size in bytes (default 5MB).</param>
        public FilePersistence(string basePath = null, long maxTotalBytes = 5L * 1024 * 1024)
        {
            _directory = Path.Combine(
                basePath ?? Application.persistentDataPath,
                "dataferret"
            );
            _maxTotalBytes = maxTotalBytes;
            EnsureDirectory();
        }

        /// <summary>
        /// Saves events to a timestamped .jsonl file.
        /// Each event is serialized as a single JSON line.
        /// After saving, enforces the maximum total size limit by deleting oldest files.
        /// </summary>
        /// <param name="events">The events to persist.</param>
        public void Save(IEnumerable<EventEnvelope> events)
        {
            var fileName = $"dataferret_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.jsonl";
            var filePath = Path.Combine(_directory, fileName);

            using (var writer = new StreamWriter(filePath, true))
            {
                foreach (var evt in events)
                {
                    writer.WriteLine(JsonConvert.SerializeObject(evt));
                }
                writer.Flush();
            }
            EnforceMaxSize();
        }

        /// <summary>
        /// Loads all persisted events from .jsonl files, ordered oldest first.
        /// Corrupted or unparseable lines are silently skipped.
        /// </summary>
        /// <returns>List of deserialized EventEnvelope instances.</returns>
        public List<EventEnvelope> LoadAll()
        {
            var events = new List<EventEnvelope>();
            if (!Directory.Exists(_directory)) return events;

            var files = Directory.GetFiles(_directory, "*.jsonl");
            Array.Sort(files); // oldest first

            foreach (var file in files)
            {
                foreach (var line in File.ReadLines(file))
                {
                    try
                    {
                        var envelope = JsonConvert.DeserializeObject<EventEnvelope>(line);
                        if (envelope != null)
                            events.Add(envelope);
                    }
                    catch
                    {
                        // Skip corrupted lines
                    }
                }
            }
            return events;
        }

        /// <summary>
        /// Deletes a specific persistence file.
        /// </summary>
        /// <param name="filePath">Absolute path to the file to delete.</param>
        public void Delete(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        /// <summary>
        /// Deletes all persisted .jsonl files in the dataferret directory.
        /// </summary>
        public void DeleteAll()
        {
            if (!Directory.Exists(_directory)) return;
            foreach (var file in Directory.GetFiles(_directory, "*.jsonl"))
            {
                File.Delete(file);
            }
        }

        private void EnsureDirectory()
        {
            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);
        }

        private void EnforceMaxSize()
        {
            var files = Directory.GetFiles(_directory, "*.jsonl");
            Array.Sort(files); // oldest first

            long totalSize = 0;
            foreach (var file in files)
            {
                totalSize += new FileInfo(file).Length;
            }

            // Delete oldest files until under limit
            int index = 0;
            while (totalSize > _maxTotalBytes && index < files.Length)
            {
                var fileSize = new FileInfo(files[index]).Length;
                File.Delete(files[index]);
                totalSize -= fileSize;
                index++;
            }
        }
    }
}
