using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace DataFerret.Analytics
{
    /// <summary>
    /// WebGL-specific persistence using PlayerPrefs.
    /// Used as a fallback when file system access is not available (WebGL builds).
    /// Limited to <see cref="MaxEntries"/> (200) entries due to PlayerPrefs size constraints.
    /// </summary>
    public class WebGLPersistence
    {
        private const string KeyPrefix = "df_queue_";
        private const string CountKey = "df_queue_count";
        private const int MaxEntries = 200;

        /// <summary>
        /// Saves events to PlayerPrefs, up to <see cref="MaxEntries"/> limit.
        /// Events beyond the limit are silently dropped.
        /// </summary>
        /// <param name="events">The events to persist.</param>
        public void Save(IEnumerable<EventEnvelope> events)
        {
            var currentCount = PlayerPrefs.GetInt(CountKey, 0);

            foreach (var evt in events)
            {
                if (currentCount >= MaxEntries) break;

                var json = JsonConvert.SerializeObject(evt);
                PlayerPrefs.SetString($"{KeyPrefix}{currentCount}", json);
                currentCount++;
            }

            PlayerPrefs.SetInt(CountKey, currentCount);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads all persisted events from PlayerPrefs.
        /// Corrupted or unparseable entries are silently skipped.
        /// </summary>
        /// <returns>List of deserialized EventEnvelope instances.</returns>
        public List<EventEnvelope> LoadAll()
        {
            var events = new List<EventEnvelope>();
            var count = PlayerPrefs.GetInt(CountKey, 0);

            for (int i = 0; i < count; i++)
            {
                var key = $"{KeyPrefix}{i}";
                var json = PlayerPrefs.GetString(key);
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var envelope = JsonConvert.DeserializeObject<EventEnvelope>(json);
                        if (envelope != null)
                            events.Add(envelope);
                    }
                    catch
                    {
                        // Skip corrupted entries
                    }
                }
            }
            return events;
        }

        /// <summary>
        /// Clears all persisted events from PlayerPrefs and resets the count.
        /// </summary>
        public void Clear()
        {
            var count = PlayerPrefs.GetInt(CountKey, 0);
            for (int i = 0; i < count; i++)
            {
                PlayerPrefs.DeleteKey($"{KeyPrefix}{i}");
            }
            PlayerPrefs.SetInt(CountKey, 0);
            PlayerPrefs.Save();
        }
    }
}
