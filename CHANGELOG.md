# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-03-07

### Added

- Core API: `Init`, `Track`, `Identify`, `Screen`, `Group`, `Reset`, `Flush`
- `SetGlobalProperties` for properties merged into all events
- Auto-capture: Sessions (30-min timeout), SceneChanges, Errors (60s dedup), Lifecycle
- Optional auto-capture: PerformanceSamples (FPS, memory every 30s)
- Offline queue persistence (.jsonl files 5MB limit, WebGL PlayerPrefs 200 entries)
- Multi-platform support: Android, iOS, WebGL, Windows, macOS, Linux
- WebGL transport via .jslib bridge (fetch / sendBeacon)
- Exponential backoff retry (1s, 2s, 4s) with permanent failure handling (401, 413)
- EventEnvelope object pooling for GC optimization (< 1KB per Track call)
- UUID v7 event IDs (timestamp-based, sortable)
- IL2CPP stripping protection (link.xml)
- Unity Editor window (Window > DataFerret Analytics)
- Build processor for Write Key validation on release builds
- UPM sample: BasicUsage with SampleTracker
- EditMode and PlayMode test suites
