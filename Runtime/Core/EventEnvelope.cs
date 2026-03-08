using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DataFerret.Analytics
{
    /// <summary>
    /// The main event data container sent to the DataFerret Collector API.
    /// Supports object pooling via <see cref="Reset"/>.
    /// </summary>
    [Serializable]
    public class EventEnvelope
    {
        [JsonProperty("eventId")]
        public string EventId;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("timestamp")]
        public string Timestamp;

        [JsonProperty("anonymousId")]
        public string AnonymousId;

        [JsonProperty("userId", NullValueHandling = NullValueHandling.Ignore)]
        public string UserId;

        [JsonProperty("event", NullValueHandling = NullValueHandling.Ignore)]
        public string Event;

        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Properties;

        [JsonProperty("context")]
        public EventContext Context;

        /// <summary>
        /// Resets all fields to their default values for object pooling reuse.
        /// </summary>
        public void Reset()
        {
            EventId = null;
            Type = null;
            Timestamp = null;
            AnonymousId = null;
            UserId = null;
            Event = null;
            Properties = null;
            Context = null;
        }
    }
}
