using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Main entry point for the DataFerret Analytics SDK.
    /// Provides static methods for initialization, event tracking, and user identification.
    /// All public methods are static and require <see cref="Init"/> to be called first.
    /// Methods called before initialization are silent no-ops.
    /// </summary>
    public static class DataFerretAnalytics
    {
        private const string SdkVersion = "0.1.0";

        private static DataFerretConfig _config;
        private static EventQueue _queue;
        private static ITransport _transport;
        private static EventBuilder _eventBuilder;
        private static IdentityManager _identityManager;
        private static FlushScheduler _flushScheduler;

        // Lifecycle trackers
        private static SessionTracker _sessionTracker;
        private static SceneTracker _sceneTracker;
        private static LifecycleTracker _lifecycleTracker;
        private static ErrorTracker _errorTracker;

        // Persistence
        private static FilePersistence _filePersistence;

        /// <summary>SDK configuration, or null if not initialized.</summary>
        public static DataFerretConfig Config => _config;

        /// <summary>Whether the SDK has been initialized.</summary>
        public static bool IsInitialized => _config != null;

        /// <summary>Current anonymous ID, or null if not initialized.</summary>
        public static string AnonymousId => _identityManager?.AnonymousId;

        /// <summary>Current user ID, or null if not identified.</summary>
        public static string UserId => _identityManager?.UserId;

        /// <summary>SDK version string.</summary>
        public static string Version => SdkVersion;

        /// <summary>
        /// Initializes the SDK with the given configuration.
        /// Must be called before any other SDK methods.
        /// </summary>
        /// <param name="config">SDK configuration. WriteKey must be valid (wk_ prefix).</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
        /// <exception cref="ArgumentException">Thrown if config is invalid (WriteKey must start with 'wk_').</exception>
        public static void Init(DataFerretConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (!config.IsValid())
                throw new ArgumentException("Invalid configuration. WriteKey must start with 'wk_'.", nameof(config));
            if (IsInitialized)
            {
                if (_config.Debug)
                    Debug.LogWarning("[DataFerret] SDK already initialized. Call Reset() first to reinitialize.");
                return;
            }

            // HTTPS warning
            if (config.ApiHost != null && config.ApiHost.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[DataFerret] ApiHost uses HTTP instead of HTTPS. HTTPS is strongly recommended for security.");
            }

            _config = config;

            // Core components
            _identityManager = new IdentityManager();
            _eventBuilder = new EventBuilder(_identityManager);
            _queue = new EventQueue(config.MaxQueueSize);

            // Transport (platform-specific)
            _transport = CreateTransport(config);

            // Persistence
            #if !UNITY_WEBGL
            _filePersistence = new FilePersistence();
            LoadPersistedEvents();
            #endif

            // FlushScheduler
            _flushScheduler = FlushScheduler.Create(_queue, _transport, config);

            // Lifecycle trackers
            InitializeTrackers(config);

            // Start session
            if (config.AutoCapture.Sessions)
                _sessionTracker.StartSession();

            if (config.Debug)
                Debug.Log($"[DataFerret] SDK initialized. AnonymousId: {_identityManager.AnonymousId}");
        }

        /// <summary>
        /// Initializes the SDK with a custom transport for testing.
        /// Does not create auto-capture trackers (except SessionTracker).
        /// Does not load persisted events or create platform transport.
        /// </summary>
        /// <param name="config">SDK configuration. WriteKey must be valid (wk_ prefix).</param>
        /// <param name="transport">Custom transport implementation (e.g., MockTransport).</param>
        /// <exception cref="ArgumentNullException">Thrown if config or transport is null.</exception>
        /// <exception cref="ArgumentException">Thrown if config is invalid.</exception>
        internal static void InitForTesting(DataFerretConfig config, ITransport transport)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!config.IsValid())
                throw new ArgumentException("Invalid configuration. WriteKey must start with 'wk_'.", nameof(config));

            _config = config;
            _identityManager = new IdentityManager();
            _eventBuilder = new EventBuilder(_identityManager);
            _queue = new EventQueue(config.MaxQueueSize);
            _transport = transport;
            _flushScheduler = FlushScheduler.Create(_queue, _transport, config);

            // Create session tracker for testing but do not auto-start session
            _sessionTracker = new SessionTracker(Track);
        }

        /// <summary>
        /// Tracks a custom event.
        /// </summary>
        /// <param name="eventName">Name of the event (e.g., "button_clicked").</param>
        /// <param name="properties">Optional event properties.</param>
        public static void Track(string eventName, Dictionary<string, object> properties = null)
        {
            if (!IsInitialized) return;
            var envelope = _eventBuilder.BuildTrack(eventName, properties);
            _queue.Enqueue(envelope);
        }

        /// <summary>
        /// Identifies the current user. Sets the userId for all subsequent events
        /// and enqueues an identify event with optional traits.
        /// </summary>
        /// <param name="userId">Unique user identifier.</param>
        /// <param name="traits">Optional user traits (e.g., email, plan).</param>
        public static void Identify(string userId, Dictionary<string, object> traits = null)
        {
            if (!IsInitialized) return;
            _identityManager.SetUserId(userId);
            var envelope = _eventBuilder.BuildIdentify(userId, traits);
            _queue.Enqueue(envelope);
        }

        /// <summary>
        /// Tracks a screen/scene view.
        /// </summary>
        /// <param name="screenName">Name of the screen or scene.</param>
        /// <param name="properties">Optional screen properties.</param>
        public static void Screen(string screenName, Dictionary<string, object> properties = null)
        {
            if (!IsInitialized) return;
            var envelope = _eventBuilder.BuildScreen(screenName, properties);
            _queue.Enqueue(envelope);
        }

        /// <summary>
        /// Associates the current user with a group.
        /// </summary>
        /// <param name="groupId">Group identifier (e.g., guild ID).</param>
        /// <param name="traits">Optional group traits.</param>
        public static void Group(string groupId, Dictionary<string, object> traits = null)
        {
            if (!IsInitialized) return;
            var envelope = _eventBuilder.BuildGroup(groupId, traits);
            _queue.Enqueue(envelope);
        }

        /// <summary>
        /// Sets global properties that are merged into every subsequent event.
        /// Event-specific properties override global properties with the same key.
        /// Pass null to clear global properties.
        /// </summary>
        /// <param name="properties">Properties to merge into all events, or null to clear.</param>
        public static void SetGlobalProperties(Dictionary<string, object> properties)
        {
            if (!IsInitialized) return;
            _eventBuilder.SetGlobalProperties(properties);
        }

        /// <summary>
        /// Triggers an immediate asynchronous flush of queued events.
        /// </summary>
        public static void Flush()
        {
            if (!IsInitialized) return;
            _flushScheduler?.Flush();
        }

        /// <summary>
        /// Triggers a synchronous flush of queued events.
        /// Used during application quit when coroutines cannot be started.
        /// </summary>
        public static void FlushSync()
        {
            if (!IsInitialized) return;
            _flushScheduler?.FlushSync();
        }

        /// <summary>
        /// Resets the SDK identity: clears userId, generates a new anonymousId,
        /// and clears the event queue. Used for logout scenarios.
        /// The SDK remains initialized after reset.
        /// </summary>
        public static void Reset()
        {
            if (!IsInitialized) return;
            _identityManager.Reset();

            if (_config.Debug)
                Debug.Log($"[DataFerret] Identity reset. New AnonymousId: {_identityManager.AnonymousId}");
        }

        /// <summary>
        /// Shuts down the SDK, disposing all trackers and flushing remaining events.
        /// After calling this, <see cref="Init"/> must be called again to use the SDK.
        /// </summary>
        internal static void Shutdown()
        {
            if (!IsInitialized) return;

            // End session
            _sessionTracker?.EndSession();

            // Dispose trackers
            _sceneTracker?.Dispose();
            _errorTracker?.Dispose();

            // Flush remaining events
            _flushScheduler?.FlushSync();

            // Destroy FlushScheduler
            FlushScheduler.DestroyInstance();

            // Clear state
            _config = null;
            _queue = null;
            _transport = null;
            _eventBuilder = null;
            _identityManager = null;
            _flushScheduler = null;
            _sessionTracker = null;
            _sceneTracker = null;
            _lifecycleTracker = null;
            _errorTracker = null;
            _filePersistence = null;
        }

        private static ITransport CreateTransport(DataFerretConfig config)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            var go = new GameObject("[DataFerret] WebGLTransport");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var webglTransport = go.AddComponent<WebGLTransport>();
            webglTransport.Initialize(config.ApiHost, config.WriteKey, config.TimeoutSeconds, config.Debug);
            return webglTransport;
            #else
            return new UnityWebRequestTransport(config.ApiHost, config.WriteKey, config.TimeoutSeconds, config.MaxRetries);
            #endif
        }

        private static void InitializeTrackers(DataFerretConfig config)
        {
            // Session tracker (always created, respects AutoCapture.Sessions for start)
            _sessionTracker = new SessionTracker(Track);

            // Scene tracker
            if (config.AutoCapture.SceneChanges)
            {
                _sceneTracker = new SceneTracker(Screen, true);
                _sceneTracker.Initialize();
            }

            // Lifecycle tracker
            if (config.AutoCapture.Lifecycle)
            {
                _lifecycleTracker = new LifecycleTracker(Track, true);
            }

            // Error tracker
            if (config.AutoCapture.Errors)
            {
                _errorTracker = new ErrorTracker(Track, true);
                _errorTracker.Initialize();
            }

            // Performance tracker (opt-in)
            if (config.AutoCapture.PerformanceSamples)
            {
                var go = FlushScheduler.Instance?.gameObject;
                if (go != null)
                {
                    var perfTracker = go.AddComponent<PerformanceTracker>();
                    perfTracker.Initialize(Track);
                }
            }
        }

        private static void LoadPersistedEvents()
        {
            if (_filePersistence == null) return;

            try
            {
                var events = _filePersistence.LoadAll();
                if (events.Count > 0)
                {
                    for (int i = 0; i < events.Count; i++)
                    {
                        _queue.Enqueue(events[i]);
                    }
                    _filePersistence.DeleteAll();

                    if (_config.Debug)
                        Debug.Log($"[DataFerret] Loaded {events.Count} persisted events.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataFerret] Failed to load persisted events: {ex.Message}");
            }
        }
    }
}
