# UPM Package Scaffold Design

## Context

Task #1 of Sprint 3B (Unity SDK). PRD: `.taskmaster/docs/029_p1s3b_unity_sdk_prd.txt`

## Decisions

- **Location**: `packages/sdk-unity/com.dataferret.analytics/` (alongside sdk-web in monorepo)
- **Distribution**: CI pushes to separate public repo (`dataferret/analytics-unity`) for Git URL install
- **Schema**: Same EventEnvelope structure as sdk-web, with `context.game` extension for Unity
- **Minimum Unity**: 2021.3 LTS
- **Dependency**: `com.unity.nuget.newtonsoft-json` 3.2.1

## Scope (Task #1 only)

Scaffold the UPM package structure with configuration files. No C# source code.

### Files to create

| File | Purpose |
|------|---------|
| `package.json` | UPM manifest (PRD Section 10.1) |
| `README.md` | Install guide + quick start |
| `CHANGELOG.md` | Keep a Changelog format |
| `LICENSE` | MIT |
| `Runtime/DataFerret.Analytics.asmdef` | Runtime assembly definition |
| `Editor/DataFerret.Analytics.Editor.asmdef` | Editor-only assembly definition |
| `Tests/EditMode/DataFerret.Analytics.Tests.EditMode.asmdef` | EditMode test assembly |
| `Tests/PlayMode/DataFerret.Analytics.Tests.PlayMode.asmdef` | PlayMode test assembly |

### Directories to create (with .gitkeep)

- `Runtime/Core/`, `Runtime/Queue/`, `Runtime/Transport/`
- `Runtime/Lifecycle/`, `Runtime/Platform/`, `Runtime/Plugins/WebGL/`
- `Samples~/BasicUsage/`

## Out of scope

- C# source files (Task #2+)
- CI/CD pipeline for public repo push
- OpenUPM registration
