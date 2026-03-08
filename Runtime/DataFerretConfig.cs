using System;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Configuration for the DataFerret Analytics SDK.
    /// Pass an instance to <see cref="DataFerretAnalytics.Init"/> to initialize the SDK.
    /// </summary>
    [Serializable]
    public class DataFerretConfig
    {
        /// <summary>
        /// The write key used to authenticate with the Collector API.
        /// Must start with the "wk_" prefix.
        /// </summary>
        public string WriteKey;

        /// <summary>
        /// Base URL of the DataFerret Collector API.
        /// </summary>
        public string ApiHost = "https://collect.dataferret.io";

        /// <summary>
        /// Number of events that triggers an automatic flush.
        /// </summary>
        public int FlushSize = 20;

        /// <summary>
        /// Interval in seconds between automatic flush attempts.
        /// </summary>
        public float FlushIntervalSeconds = 30f;

        /// <summary>
        /// Maximum number of events held in the in-memory queue.
        /// Oldest events are dropped when exceeded.
        /// </summary>
        public int MaxQueueSize = 1000;

        /// <summary>
        /// Maximum number of retry attempts for failed batch sends.
        /// </summary>
        public int MaxRetries = 3;

        /// <summary>
        /// HTTP request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds = 10;

        /// <summary>
        /// When true, enables verbose logging to the Unity console.
        /// Should be false in release builds.
        /// </summary>
        public bool Debug = false;

        /// <summary>
        /// Configuration for automatic event capture features.
        /// </summary>
        public AutoCaptureConfig AutoCapture = new AutoCaptureConfig();

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <returns>True if the configuration is valid and the SDK can be initialized.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(WriteKey) && WriteKey.StartsWith("wk_");
        }
    }

    /// <summary>
    /// Controls which events are automatically captured by the SDK.
    /// </summary>
    [Serializable]
    public class AutoCaptureConfig
    {
        /// <summary>
        /// Automatically track session_start and session_end events.
        /// </summary>
        public bool Sessions = true;

        /// <summary>
        /// Automatically track screen events on scene changes.
        /// </summary>
        public bool SceneChanges = true;

        /// <summary>
        /// Automatically capture unhandled exceptions and errors.
        /// </summary>
        public bool Errors = true;

        /// <summary>
        /// Automatically track app_backgrounded and app_foregrounded events.
        /// </summary>
        public bool Lifecycle = true;

        /// <summary>
        /// Periodically sample FPS, memory usage, and mono heap size.
        /// Disabled by default due to performance overhead.
        /// </summary>
        public bool PerformanceSamples = false;
    }
}
