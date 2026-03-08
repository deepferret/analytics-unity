using UnityEngine;

namespace DataFerret.Analytics.Editor
{
    /// <summary>
    /// ScriptableObject for persisting DataFerret SDK settings in the Unity Editor.
    /// Stored in Assets/Resources/DataFerretSettings.asset.
    /// </summary>
    public class DataFerretSettings : ScriptableObject
    {
        /// <summary>
        /// The write key used to authenticate with the Collector API.
        /// Must start with the "wk_" prefix.
        /// </summary>
        public string WriteKey = "";

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
        /// Automatically track session_start and session_end events.
        /// </summary>
        public bool AutoCaptureSessions = true;

        /// <summary>
        /// Automatically track screen events on scene changes.
        /// </summary>
        public bool AutoCaptureSceneChanges = true;

        /// <summary>
        /// Automatically capture unhandled exceptions and errors.
        /// </summary>
        public bool AutoCaptureErrors = true;

        /// <summary>
        /// Automatically track app_backgrounded and app_foregrounded events.
        /// </summary>
        public bool AutoCaptureLifecycle = true;

        /// <summary>
        /// Periodically sample FPS, memory usage, and mono heap size.
        /// Disabled by default due to performance overhead.
        /// </summary>
        public bool AutoCapturePerformanceSamples = false;

        private const string AssetPath = "Assets/Resources/DataFerretSettings.asset";

        /// <summary>
        /// Loads the existing settings asset from Resources, or creates a new one
        /// at Assets/Resources/DataFerretSettings.asset if none exists.
        /// </summary>
        /// <returns>The loaded or newly created <see cref="DataFerretSettings"/> instance.</returns>
        public static DataFerretSettings GetOrCreate()
        {
            var settings = Resources.Load<DataFerretSettings>("DataFerretSettings");
            if (settings != null) return settings;

#if UNITY_EDITOR
            settings = CreateInstance<DataFerretSettings>();

            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

            UnityEditor.AssetDatabase.CreateAsset(settings, AssetPath);
            UnityEditor.AssetDatabase.SaveAssets();
#endif

            return settings;
        }

        /// <summary>
        /// Converts this settings asset to a runtime <see cref="DataFerretConfig"/>.
        /// Used to initialize the SDK from Editor-configured values.
        /// </summary>
        /// <returns>A new <see cref="DataFerretConfig"/> populated from these settings.</returns>
        public DataFerretConfig ToRuntimeConfig()
        {
            return new DataFerretConfig
            {
                WriteKey = WriteKey,
                ApiHost = ApiHost,
                FlushSize = FlushSize,
                FlushIntervalSeconds = FlushIntervalSeconds,
                MaxQueueSize = MaxQueueSize,
                MaxRetries = MaxRetries,
                TimeoutSeconds = TimeoutSeconds,
                Debug = Debug,
                AutoCapture = new AutoCaptureConfig
                {
                    Sessions = AutoCaptureSessions,
                    SceneChanges = AutoCaptureSceneChanges,
                    Errors = AutoCaptureErrors,
                    Lifecycle = AutoCaptureLifecycle,
                    PerformanceSamples = AutoCapturePerformanceSamples
                }
            };
        }
    }
}
