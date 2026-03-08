using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Constructs <see cref="EventEnvelope"/> instances for different event types.
    /// Handles UUID v7 generation, ISO 8601 timestamps, context auto-collection,
    /// global property merging, and object pooling for GC-friendly operation.
    /// </summary>
    public class EventBuilder
    {
        private const string LibraryName = "com.dataferret.analytics";
        private const string LibraryVersion = "0.1.0";
        private const int MaxPropertiesCount = 200;

        private readonly IdentityManager _identityManager;
        private Dictionary<string, object> _globalProperties;

        private readonly ConcurrentStack<EventEnvelope> _pool = new ConcurrentStack<EventEnvelope>();

        // --- GC Optimization: Cached Random instance (shared, no per-call allocation) ---
        private static readonly System.Random s_random = new System.Random();

        // --- GC Optimization: ThreadStatic byte arrays for UUID generation ---
        [ThreadStatic] private static byte[] s_uuidBytes;
        [ThreadStatic] private static byte[] s_guidBytes;
        [ThreadStatic] private static byte[] s_randomBytes;

        // --- GC Optimization: Cached device context (device info is static at runtime) ---
        private static DeviceContext s_cachedDeviceContext;
        private static bool s_deviceContextInitialized;

        // --- GC Optimization: Cached library context (constant across lifetime) ---
        private static readonly LibraryContext s_cachedLibraryContext = new LibraryContext
        {
            Name = LibraryName,
            Version = LibraryVersion
        };

        // --- GC Optimization: Cached game context fields that rarely change ---
        private static string s_cachedEngineVersion;
        private static string s_cachedPlatform;

        // --- GC Optimization: Reusable dictionary for merged properties ---
        private readonly Dictionary<string, object> _mergedPropertiesBuffer = new Dictionary<string, object>();

        // --- GC Optimization: Reusable context objects (pooled per-builder) ---
        private readonly ConcurrentStack<EventContext> _contextPool = new ConcurrentStack<EventContext>();
        private readonly ConcurrentStack<GameContext> _gameContextPool = new ConcurrentStack<GameContext>();

        /// <summary>
        /// Creates a new EventBuilder that uses the given <see cref="IdentityManager"/>
        /// to populate anonymousId and userId on every envelope.
        /// </summary>
        /// <param name="identityManager">The identity manager providing user identity state.</param>
        public EventBuilder(IdentityManager identityManager)
        {
            _identityManager = identityManager ?? throw new ArgumentNullException(nameof(identityManager));
        }

        /// <summary>
        /// Sets global properties that are merged into every event envelope.
        /// Event-specific properties override global properties with the same key.
        /// Pass null to clear global properties.
        /// </summary>
        /// <param name="properties">Properties to merge into all events, or null to clear.</param>
        public void SetGlobalProperties(Dictionary<string, object> properties)
        {
            _globalProperties = properties != null
                ? new Dictionary<string, object>(properties)
                : null;
        }

        /// <summary>
        /// Builds a track event envelope.
        /// </summary>
        /// <param name="eventName">The name of the event (e.g. "button_clicked").</param>
        /// <param name="properties">Optional event properties.</param>
        /// <returns>A populated <see cref="EventEnvelope"/> ready for queuing.</returns>
        public EventEnvelope BuildTrack(string eventName, Dictionary<string, object> properties)
        {
            var envelope = RentEnvelope();
            envelope.Type = "track";
            envelope.Event = eventName;
            envelope.Properties = MergeAndTruncateProperties(properties);
            FillCommonFields(envelope);
            return envelope;
        }

        /// <summary>
        /// Builds an identify event envelope.
        /// </summary>
        /// <param name="userId">The user ID to associate.</param>
        /// <param name="traits">Optional user traits (e.g. email, plan).</param>
        /// <returns>A populated <see cref="EventEnvelope"/> ready for queuing.</returns>
        public EventEnvelope BuildIdentify(string userId, Dictionary<string, object> traits)
        {
            var envelope = RentEnvelope();
            envelope.Type = "identify";
            envelope.UserId = userId;
            envelope.Properties = MergeAndTruncateProperties(traits);
            FillCommonFields(envelope);
            return envelope;
        }

        /// <summary>
        /// Builds a screen event envelope.
        /// </summary>
        /// <param name="screenName">The name of the screen or scene.</param>
        /// <param name="properties">Optional screen properties.</param>
        /// <returns>A populated <see cref="EventEnvelope"/> ready for queuing.</returns>
        public EventEnvelope BuildScreen(string screenName, Dictionary<string, object> properties)
        {
            var envelope = RentEnvelope();
            envelope.Type = "screen";
            envelope.Event = screenName;
            envelope.Properties = MergeAndTruncateProperties(properties);
            FillCommonFields(envelope);
            return envelope;
        }

        /// <summary>
        /// Builds a group event envelope.
        /// </summary>
        /// <param name="groupId">The group identifier (e.g. guild ID, team ID).</param>
        /// <param name="traits">Optional group traits.</param>
        /// <returns>A populated <see cref="EventEnvelope"/> ready for queuing.</returns>
        public EventEnvelope BuildGroup(string groupId, Dictionary<string, object> traits)
        {
            var envelope = RentEnvelope();
            envelope.Type = "group";
            envelope.Event = groupId;
            envelope.Properties = MergeAndTruncateProperties(traits);
            FillCommonFields(envelope);
            return envelope;
        }

        /// <summary>
        /// Returns an envelope to the object pool for reuse, reducing GC allocations.
        /// Also returns the associated context objects to their respective pools.
        /// </summary>
        /// <param name="envelope">The envelope to return to the pool.</param>
        public void ReturnEnvelope(EventEnvelope envelope)
        {
            if (envelope == null) return;

            // Return context objects to pools before resetting envelope
            if (envelope.Context != null)
            {
                if (envelope.Context.Game != null)
                {
                    envelope.Context.Game.Engine = null;
                    envelope.Context.Game.EngineVersion = null;
                    envelope.Context.Game.AppVersion = null;
                    envelope.Context.Game.Platform = null;
                    envelope.Context.Game.Scene = null;
                    envelope.Context.Game.Store = null;
                    _gameContextPool.Push(envelope.Context.Game);
                }
                envelope.Context.Library = null;
                envelope.Context.Device = null;
                envelope.Context.Game = null;
                _contextPool.Push(envelope.Context);
            }

            envelope.Reset();
            _pool.Push(envelope);
        }

        private EventEnvelope RentEnvelope()
        {
            if (_pool.TryPop(out var envelope))
            {
                envelope.Reset();
                return envelope;
            }
            return new EventEnvelope();
        }

        private EventContext RentContext()
        {
            if (_contextPool.TryPop(out var context))
            {
                return context;
            }
            return new EventContext();
        }

        private GameContext RentGameContext()
        {
            if (_gameContextPool.TryPop(out var gameContext))
            {
                return gameContext;
            }
            return new GameContext();
        }

        private void FillCommonFields(EventEnvelope envelope)
        {
            envelope.EventId = GenerateUUIDv7();
            envelope.Timestamp = DateTime.UtcNow.ToString("o");
            envelope.AnonymousId = _identityManager.AnonymousId;
            // Preserve UserId if already set (e.g. by BuildIdentify), otherwise use IdentityManager
            if (envelope.UserId == null)
            {
                envelope.UserId = _identityManager.UserId;
            }
            envelope.Context = BuildContext();
        }

        private EventContext BuildContext()
        {
            var context = RentContext();
            context.Library = s_cachedLibraryContext;

            try
            {
                // Cache device context on first access (device info never changes at runtime)
                if (!s_deviceContextInitialized)
                {
                    s_cachedDeviceContext = new DeviceContext
                    {
                        Type = SystemInfo.deviceType.ToString(),
                        Model = SystemInfo.deviceModel,
                        Gpu = SystemInfo.graphicsDeviceName,
                        Os = SystemInfo.operatingSystem
                    };
                    s_deviceContextInitialized = true;
                }
                context.Device = s_cachedDeviceContext;

                // Cache engine version and platform (these don't change at runtime)
                if (s_cachedEngineVersion == null)
                {
                    s_cachedEngineVersion = Application.unityVersion;
                    s_cachedPlatform = Application.platform.ToString();
                }

                var gameContext = RentGameContext();
                gameContext.Engine = "Unity";
                gameContext.EngineVersion = s_cachedEngineVersion;
                gameContext.Platform = s_cachedPlatform;
                // Scene can change - but SceneManager.GetActiveScene().name returns
                // a cached string from Unity internals, so no extra GC alloc
                gameContext.Scene = SceneManager.GetActiveScene().name;
                context.Game = gameContext;
            }
            catch
            {
                // Gracefully handle when Unity APIs are unavailable (e.g. batch mode tests)
            }

            return context;
        }

        private Dictionary<string, object> MergeAndTruncateProperties(Dictionary<string, object> properties)
        {
            if (_globalProperties == null && properties == null)
                return null;

            // When no globals, avoid merge overhead - just validate count
            if (_globalProperties == null && properties != null)
            {
                if (properties.Count <= MaxPropertiesCount)
                    return properties;

                // Need to truncate - use buffer
                _mergedPropertiesBuffer.Clear();
                int count = 0;
                foreach (var kvp in properties)
                {
                    if (count >= MaxPropertiesCount) break;
                    _mergedPropertiesBuffer[kvp.Key] = kvp.Value;
                    count++;
                }
                return new Dictionary<string, object>(_mergedPropertiesBuffer);
            }

            // Need to merge globals with event properties
            _mergedPropertiesBuffer.Clear();

            // Global properties first (can be overridden by event-specific properties)
            if (_globalProperties != null)
            {
                foreach (var kvp in _globalProperties)
                    _mergedPropertiesBuffer[kvp.Key] = kvp.Value;
            }

            // Event-specific properties override globals
            if (properties != null)
            {
                foreach (var kvp in properties)
                    _mergedPropertiesBuffer[kvp.Key] = kvp.Value;
            }

            // Truncate to max 200 keys per API spec
            if (_mergedPropertiesBuffer.Count > MaxPropertiesCount)
            {
                var truncated = new Dictionary<string, object>();
                int count = 0;
                foreach (var kvp in _mergedPropertiesBuffer)
                {
                    if (count >= MaxPropertiesCount) break;
                    truncated[kvp.Key] = kvp.Value;
                    count++;
                }
                return truncated;
            }

            // Return a new dictionary from the buffer (caller owns the result)
            return new Dictionary<string, object>(_mergedPropertiesBuffer);
        }

        /// <summary>
        /// Generates a UUID v7 (timestamp-based, sortable).
        /// First 48 bits encode Unix timestamp in milliseconds (big-endian).
        /// Version nibble is set to 7, variant bits to RFC 4122.
        /// Uses cached byte arrays and a shared Random instance to minimize GC allocations.
        /// </summary>
        /// <returns>A UUID v7 string in standard format.</returns>
        internal static string GenerateUUIDv7()
        {
            // Lazily initialize ThreadStatic arrays (avoids allocation on subsequent calls)
            if (s_uuidBytes == null)
            {
                s_uuidBytes = new byte[16];
                s_guidBytes = new byte[16];
                s_randomBytes = new byte[10];
            }

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Timestamp (48 bits, big-endian) in bytes[0..5]
            s_uuidBytes[0] = (byte)((timestamp >> 40) & 0xFF);
            s_uuidBytes[1] = (byte)((timestamp >> 32) & 0xFF);
            s_uuidBytes[2] = (byte)((timestamp >> 24) & 0xFF);
            s_uuidBytes[3] = (byte)((timestamp >> 16) & 0xFF);
            s_uuidBytes[4] = (byte)((timestamp >> 8) & 0xFF);
            s_uuidBytes[5] = (byte)(timestamp & 0xFF);

            // Fill remaining 10 bytes with random data using cached Random and byte array
            lock (s_random)
            {
                s_random.NextBytes(s_randomBytes);
            }
            Array.Copy(s_randomBytes, 0, s_uuidBytes, 6, 10);

            // Set version nibble to 7 (0111xxxx in byte 6)
            s_uuidBytes[6] = (byte)((s_uuidBytes[6] & 0x0F) | 0x70);

            // Set variant bits to RFC 4122 (10xxxxxx in byte 8)
            s_uuidBytes[8] = (byte)((s_uuidBytes[8] & 0x3F) | 0x80);

            // .NET Guid constructor expects mixed-endian layout for first 3 groups.
            // We need to swap bytes for correct string representation.
            // Group 1 (bytes 0-3): little-endian in Guid
            // Group 2 (bytes 4-5): little-endian in Guid
            // Group 3 (bytes 6-7): little-endian in Guid
            // Groups 4-5 (bytes 8-15): big-endian in Guid

            // Swap group 1 (4 bytes)
            s_guidBytes[0] = s_uuidBytes[3];
            s_guidBytes[1] = s_uuidBytes[2];
            s_guidBytes[2] = s_uuidBytes[1];
            s_guidBytes[3] = s_uuidBytes[0];

            // Swap group 2 (2 bytes)
            s_guidBytes[4] = s_uuidBytes[5];
            s_guidBytes[5] = s_uuidBytes[4];

            // Swap group 3 (2 bytes)
            s_guidBytes[6] = s_uuidBytes[7];
            s_guidBytes[7] = s_uuidBytes[6];

            // Groups 4-5 remain big-endian
            Array.Copy(s_uuidBytes, 8, s_guidBytes, 8, 8);

            return new Guid(s_guidBytes).ToString();
        }

        /// <summary>
        /// Resets cached static state. Used for testing to ensure clean state between tests.
        /// </summary>
        internal static void ResetStaticCache()
        {
            s_deviceContextInitialized = false;
            s_cachedDeviceContext = null;
            s_cachedEngineVersion = null;
            s_cachedPlatform = null;
        }
    }
}
