# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **DataFerret Analytics Unity SDK** (`com.dataferret.analytics`) — a native event analytics SDK for Unity games. It collects player behavior events and sends them to the DataFerret Collector API, enabling AI-powered analysis on the DataFerret platform.

**Key Characteristics:**

- Unity package distributed via Unity Package Manager (UPM)
- Supports Unity 2021.3 LTS+ (.NET Standard 2.1)
- Current version: 0.1.0
- Supported platforms: Android, iOS, WebGL, Windows, macOS, Linux
- Dependency: `com.unity.nuget.newtonsoft-json` 3.2.1

## Architecture

### Core Design Pattern

The SDK uses a **static facade** pattern with MonoBehaviour lifecycle management:

- **`DataFerretAnalytics` (static facade)**: Public API surface — Init, Track, Identify, Screen, Group, Reset, Flush
- **`FlushScheduler` (MonoBehaviour singleton)**: DontDestroyOnLoad, Update loop for periodic flush, OnApplicationPause/Quit handling
- **`EventQueue` (ConcurrentQueue wrapper)**: Thread-safe event queue with MaxQueueSize (1000) and oldest-drop policy
- **`EventBuilder`**: Constructs EventEnvelope with auto-collected context (library, device, game)
- **`IdentityManager`**: userId / anonymousId management, PlayerPrefs persistence for anonymousId

### Package Structure

```
com.dataferret.analytics/
├── package.json
├── Runtime/
│   ├── DataFerret.Analytics.asmdef
│   ├── DataFerretAnalytics.cs         (Static facade - main entry point)
│   ├── DataFerretConfig.cs            (Configuration class)
│   ├── Core/                          (EventBuilder, EventEnvelope, EventContext, EventType, IdentityManager)
│   ├── Queue/                         (EventQueue, FilePersistence, WebGLPersistence)
│   ├── Transport/                     (ITransport, UnityWebRequestTransport, WebGLTransport)
│   ├── Lifecycle/                     (FlushScheduler, SessionTracker, SceneTracker, LifecycleTracker, ErrorTracker, PerformanceTracker)
│   ├── Platform/                      (PlatformInfo, WebGLBridge)
│   └── Plugins/WebGL/                 (DataFerretBridge.jslib)
├── Editor/
│   ├── DataFerret.Analytics.Editor.asmdef
│   ├── DataFerretEditorWindow.cs      (Window > DataFerret Analytics)
│   └── BuildProcessor.cs             (Write Key validation on build)
├── Tests/
│   ├── EditMode/                      (~32 tests: EventBuilder, EventQueue, IdentityManager, PlatformInfo, FilePersistence, Config)
│   └── PlayMode/                      (~23 tests: FlushScheduler, SessionTracker, SceneTracker, Transport, Integration)
└── Samples~/BasicUsage/
```

### Key Components

**DataFerretAnalytics.cs**: Main entry point (static facade)

- `Init(DataFerretConfig config)` — SDK initialization
- `Track(string eventName, Dictionary<string, object> properties)` — Event tracking
- `Identify(string userId, Dictionary<string, object> traits)` — User identification
- `Screen(string screenName, Dictionary<string, object> properties)` — Scene/screen tracking
- `Group(string groupId, Dictionary<string, object> traits)` — Group association
- `SetGlobalProperties(Dictionary<string, object> properties)` — Properties merged into all events
- `Flush()` / `FlushSync()` — Immediate send
- `Reset()` — Clear userId + regenerate anonymousId

**DataFerretConfig.cs**: Configuration class

- `WriteKey` (required, `wk_` prefix)
- `ApiHost` (default: `https://collect.dataferret.io`)
- `FlushSize` (20), `FlushIntervalSeconds` (30f), `MaxQueueSize` (1000), `MaxRetries` (3), `TimeoutSeconds` (10)
- `AutoCaptureConfig`: Sessions, SceneChanges, Errors, Lifecycle (default true), PerformanceSamples (opt-in)

**Transport Layer**: Platform-specific HTTP transport

- `UnityWebRequestTransport`: Coroutine-based, exponential backoff (1s→2s→4s), max 3 retries
- `WebGLTransport`: .jslib bridge → fetch() / sendBeacon()
- 401/413: permanent failure (no retry), 429/5xx: retry with backoff

**Persistence Layer**: Offline queue recovery

- `FilePersistence`: .jsonl files in `Application.persistentDataPath/dataferret/`, 5MB max
- `WebGLPersistence`: PlayerPrefs fallback, 200 entries max
- On Init(): load persisted events → merge into queue → retry on next flush

### Auto-Capture (Lifecycle MonoBehaviours)

- **SessionTracker**: session_start / session_end, 30-min timeout, GUID session ID
- **SceneTracker**: SceneManager.sceneLoaded → screen event with previous_scene
- **LifecycleTracker**: app_backgrounded / app_foregrounded via OnApplicationPause
- **ErrorTracker**: Application.logMessageReceived (Exception/Error), 60s dedup, 1000-char stackTrace truncation
- **PerformanceTracker** (opt-in): fps, memory_used, mono_heap every 30s

### Data Flow

```
Track() → EventBuilder.BuildTrack() → EventQueue.Enqueue() → FlushScheduler (periodic) → ITransport.SendBatch()
                                                                                              ↓ (failure)
                                                                                         FilePersistence.Save()
                                                                                              ↓ (next Init)
                                                                                         FilePersistence.LoadAll() → queue merge → retry
```

### Collector API Endpoint

```
POST /v1/collect/batch
Authorization: Bearer {writeKey}
Content-Type: application/json

{ "batch": [ ...EventEnvelope[] ], "sentAt": "ISO8601" }
```

- Batch max: 500 events or 4MB
- Single event max: 32KB
- Properties keys max: 200
- Rate limit: 100req/10s (IP), 1000req/min (API Key)

## Development Commands

### Package Installation

**Via UPM Git URL (recommended)**:
```json
"com.dataferret.analytics": "https://github.com/dataferret/analytics-unity.git#v0.1.0"
```

**Local development** (add to `Packages/manifest.json`):
```json
"com.dataferret.analytics": "file:/path/to/cloned/analytics-unity"
```

### Version Management

- Update version in `Runtime/DataFerretAnalytics.cs` (SDK version constant)
- Update version in `package.json`
- Update `CHANGELOG.md`

### Test Execution

```
Unity Menu: Window > General > Test Runner
  ├─ EditMode tab → Run All (~32 tests, fast)
  └─ PlayMode tab → Run All (~23 tests, requires game loop)
```

Target: 55 total tests (EditMode 32 + PlayMode 23)

## Code Conventions

### Namespace Structure

- Runtime namespace: `DataFerret.Analytics`
- Editor namespace: `DataFerret.Analytics.Editor`

### API Patterns

- All public API methods are static on `DataFerretAnalytics` class
- Use `IsInitialized` guard at start of every public method
- Properties are passed via `Dictionary<string, object>`
- WriteKey must have `wk_` prefix — validated on Init()

### Unity-Specific Patterns

- `DontDestroyOnLoad` for FlushScheduler singleton
- `DisallowMultipleComponent` to prevent duplicate instances
- `#if UNITY_WEBGL` for platform-specific code paths (WebGLTransport, WebGLPersistence)
- `.meta` files must accompany all assets
- `.asmdef` files define assembly boundaries (Runtime, Editor, Tests.EditMode, Tests.PlayMode)

### GC Allocation Target

- Track() 1 call < 1KB GC Alloc (Unity Profiler measurement)
- EventEnvelope pooling (Stack-based object pool)
- StringBuilder reuse for JSON serialization
- No LINQ in hot paths — use for loops
- struct for internal intermediate results

### Serialization

- Use `Newtonsoft.Json` (`com.unity.nuget.newtonsoft-json`) for all JSON serialization
- `[JsonProperty]` attributes on EventEnvelope fields with camelCase naming
- `NullValueHandling.Ignore` for optional fields
- EventId: UUID v7 format (timestamp-based)
- Timestamp: ISO 8601 UTC

### Documentation

- XML documentation comments (`///`) for all public APIs
- Follow existing style with `<summary>`, `<param>` tags

## Important Context

### Backend Dependencies (Already Implemented)

- **Collector API**: `POST /v1/collect/batch` — accepts EventEnvelope batch with Write Key auth
- **Write Key System**: `wk_` prefix, `event:write` permission
- **PostgreSQL Queue**: `raw_events` table (7-day TTL, dedup)
- **Batch Processor**: 5-min cron → Parquet → S3

### Platform Considerations

| Feature | Android/iOS/PC | WebGL |
|---------|---------------|-------|
| HTTP Transport | UnityWebRequest (coroutine) | .jslib → fetch() / sendBeacon() |
| Offline Queue | .jsonl files | PlayerPrefs (1MB limit) |
| Background Thread | Available | Main thread only |
| CORS | N/A | Required (allowedOrigins) |
| App Quit | OnApplicationQuit() | visibilitychange → sendBeacon |

### Security

- Write Key is write-only (cannot read/manage data)
- HTTPS enforced — warn/error on `http://` ApiHost
- No automatic PII collection
- `link.xml` needed for IL2CPP stripping protection (Newtonsoft.Json)
- BuildProcessor forces `Debug = false` on Release builds
- ErrorTracker stackTrace may contain PII — truncated to 1000 chars

### IL2CPP Stripping Protection

```xml
<linker>
  <assembly fullname="Newtonsoft.Json" preserve="all" />
  <assembly fullname="DataFerret.Analytics" preserve="all" />
</linker>
```

## Common Tasks

### Adding a New API Method

1. Add static method to `DataFerretAnalytics.cs`
2. Include `IsInitialized` check
3. Add XML documentation
4. Build EventEnvelope via `EventBuilder` and enqueue

### Adding a New Auto-Capture Tracker

1. Create class in `Runtime/Lifecycle/`
2. Add toggle to `DataFerretConfig.AutoCaptureConfig`
3. Initialize/dispose in FlushScheduler or DataFerretAnalytics.Init()
4. Track events via `DataFerretAnalytics.Track()`

### Adding a New Platform

1. Add platform detection in `PlatformInfo.cs`
2. If needed, create platform-specific Transport implementing `ITransport`
3. If needed, create platform-specific Persistence
4. Add `#if` platform guards in FlushScheduler

### Working with EventEnvelope

- Use `EventBuilder.BuildTrack/BuildIdentify/BuildScreen/BuildGroup()`
- Context auto-collected: library info, device info, game info (engine, platform, scene, store)
- Global properties merged automatically
- Object pooling: `Rent()` / `Return()` pattern

## API Reference

Collector API spec: See PRD Appendix A (`POST /v1/collect/batch`)
