using System;
using Newtonsoft.Json;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Contextual information automatically collected with each event.
    /// </summary>
    [Serializable]
    public class EventContext
    {
        [JsonProperty("library")]
        public LibraryContext Library;

        [JsonProperty("device", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceContext Device;

        [JsonProperty("game", NullValueHandling = NullValueHandling.Ignore)]
        public GameContext Game;
    }

    /// <summary>
    /// Information about the analytics library.
    /// </summary>
    [Serializable]
    public class LibraryContext
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("version")]
        public string Version;
    }

    /// <summary>
    /// Information about the device running the game.
    /// </summary>
    [Serializable]
    public class DeviceContext
    {
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type;

        [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
        public string Model;

        [JsonProperty("gpu", NullValueHandling = NullValueHandling.Ignore)]
        public string Gpu;

        [JsonProperty("os", NullValueHandling = NullValueHandling.Ignore)]
        public string Os;
    }

    /// <summary>
    /// Information about the game and its runtime environment.
    /// </summary>
    [Serializable]
    public class GameContext
    {
        [JsonProperty("engine", NullValueHandling = NullValueHandling.Ignore)]
        public string Engine;

        [JsonProperty("engineVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string EngineVersion;

        [JsonProperty("appVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string AppVersion;

        [JsonProperty("platform", NullValueHandling = NullValueHandling.Ignore)]
        public string Platform;

        [JsonProperty("scene", NullValueHandling = NullValueHandling.Ignore)]
        public string Scene;

        [JsonProperty("store", NullValueHandling = NullValueHandling.Ignore)]
        public string Store;
    }
}
