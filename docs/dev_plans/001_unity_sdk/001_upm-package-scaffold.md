# UPM Package Scaffold Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Unity SDK (`com.dataferret.analytics`) UPM 패키지의 디렉토리 구조와 설정 파일을 생성한다.

**Architecture:** `packages/sdk-unity/com.dataferret.analytics/` 아래에 UPM 표준 구조(Runtime, Editor, Tests, Samples~)를 생성. 4개 Assembly Definition, package.json, README, CHANGELOG, LICENSE를 포함. 소스 코드(.cs)는 후속 Task에서 작성.

**Tech Stack:** Unity Package Manager (UPM), Assembly Definition (.asmdef), Newtonsoft.Json for Unity

**PRD Reference:** `.taskmaster/docs/029_p1s3b_unity_sdk_prd.txt` Section 2.1, 10.1-10.3

---

### Task 1: Create directory structure

**Files:**
- Create: `packages/sdk-unity/com.dataferret.analytics/Runtime/Core/.gitkeep`
- Create: `packages/sdk-unity/com.dataferret.analytics/Runtime/Queue/.gitkeep`
- Create: `packages/sdk-unity/com.dataferret.analytics/Runtime/Transport/.gitkeep`
- Create: `packages/sdk-unity/com.dataferret.analytics/Runtime/Lifecycle/.gitkeep`
- Create: `packages/sdk-unity/com.dataferret.analytics/Runtime/Platform/.gitkeep`
- Create: `packages/sdk-unity/com.dataferret.analytics/Runtime/Plugins/WebGL/.gitkeep`
- Create: `packages/sdk-unity/com.dataferret.analytics/Editor/.gitkeep`
- Create: `packages/sdk-unity/com.dataferret.analytics/Tests/EditMode/.gitkeep`
- Create: `packages/sdk-unity/com.dataferret.analytics/Tests/PlayMode/.gitkeep`
- Create: `packages/sdk-unity/com.dataferret.analytics/Samples~/BasicUsage/.gitkeep`

**Step 1: Create all directories with .gitkeep files**

```bash
BASE=packages/sdk-unity/com.dataferret.analytics
mkdir -p $BASE/Runtime/{Core,Queue,Transport,Lifecycle,Platform,Plugins/WebGL}
mkdir -p $BASE/Editor
mkdir -p $BASE/Tests/{EditMode,PlayMode}
mkdir -p "$BASE/Samples~/BasicUsage"

for dir in \
  $BASE/Runtime/Core \
  $BASE/Runtime/Queue \
  $BASE/Runtime/Transport \
  $BASE/Runtime/Lifecycle \
  $BASE/Runtime/Platform \
  $BASE/Runtime/Plugins/WebGL \
  $BASE/Editor \
  $BASE/Tests/EditMode \
  $BASE/Tests/PlayMode \
  "$BASE/Samples~/BasicUsage"; do
  touch "$dir/.gitkeep"
done
```

**Step 2: Verify structure**

```bash
find packages/sdk-unity -type f | sort
```

Expected: 10 `.gitkeep` files across the directory tree.

---

### Task 2: Create package.json

**Files:**
- Create: `packages/sdk-unity/com.dataferret.analytics/package.json`

**Step 1: Write package.json**

PRD Section 10.1 spec:

```json
{
  "name": "com.dataferret.analytics",
  "version": "0.1.0",
  "displayName": "DataFerret Analytics",
  "description": "Event analytics SDK for Unity games. Track player behavior, sessions, errors, and performance with automatic collection. Integrates with DataFerret platform for AI-powered analysis.",
  "unity": "2021.3",
  "unityRelease": "0f1",
  "documentationUrl": "https://docs.dataferret.io/sdk/unity",
  "changelogUrl": "https://github.com/dataferret/analytics-unity/blob/main/CHANGELOG.md",
  "licensesUrl": "https://github.com/dataferret/analytics-unity/blob/main/LICENSE",
  "dependencies": {
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  },
  "keywords": [
    "analytics",
    "events",
    "tracking",
    "dataferret",
    "telemetry",
    "session",
    "retention"
  ],
  "author": {
    "name": "CookApps",
    "email": "sdk@dataferret.io",
    "url": "https://dataferret.io"
  },
  "samples": [
    {
      "displayName": "Basic Usage",
      "description": "Basic event tracking example scene with initialization, identify, track, and screen calls.",
      "path": "Samples~/BasicUsage"
    }
  ]
}
```

**Step 2: Validate JSON**

```bash
cat packages/sdk-unity/com.dataferret.analytics/package.json | python3 -m json.tool > /dev/null && echo "Valid JSON"
```

Expected: `Valid JSON`

---

### Task 3: Create Assembly Definition files

**Files:**
- Create: `packages/sdk-unity/com.dataferret.analytics/Runtime/DataFerret.Analytics.asmdef`
- Create: `packages/sdk-unity/com.dataferret.analytics/Editor/DataFerret.Analytics.Editor.asmdef`
- Create: `packages/sdk-unity/com.dataferret.analytics/Tests/EditMode/DataFerret.Analytics.Tests.EditMode.asmdef`
- Create: `packages/sdk-unity/com.dataferret.analytics/Tests/PlayMode/DataFerret.Analytics.Tests.PlayMode.asmdef`

**Step 1: Write Runtime asmdef**

```json
{
  "name": "DataFerret.Analytics",
  "rootNamespace": "DataFerret.Analytics",
  "references": [
    "com.unity.nuget.newtonsoft-json"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

**Step 2: Write Editor asmdef**

```json
{
  "name": "DataFerret.Analytics.Editor",
  "rootNamespace": "DataFerret.Analytics.Editor",
  "references": [
    "DataFerret.Analytics"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

**Step 3: Write EditMode test asmdef**

```json
{
  "name": "DataFerret.Analytics.Tests.EditMode",
  "rootNamespace": "DataFerret.Analytics.Tests.EditMode",
  "references": [
    "DataFerret.Analytics",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false,
  "optionalUnityReferences": [
    "TestAssemblies"
  ]
}
```

**Step 4: Write PlayMode test asmdef**

```json
{
  "name": "DataFerret.Analytics.Tests.PlayMode",
  "rootNamespace": "DataFerret.Analytics.Tests.PlayMode",
  "references": [
    "DataFerret.Analytics",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false,
  "optionalUnityReferences": [
    "TestAssemblies"
  ]
}
```

**Step 5: Validate all asmdef files are valid JSON**

```bash
for f in packages/sdk-unity/com.dataferret.analytics/{Runtime,Editor,Tests/EditMode,Tests/PlayMode}/*.asmdef; do
  python3 -m json.tool "$f" > /dev/null && echo "OK: $f" || echo "FAIL: $f"
done
```

Expected: 4x `OK`

---

### Task 4: Create README.md

**Files:**
- Create: `packages/sdk-unity/com.dataferret.analytics/README.md`

**Step 1: Write README**

Include: package description, installation methods (Git URL, OpenUPM, .unitypackage), quick start code, minimum requirements. Reference PRD Section 3.3 for usage example and Section 10.3 for install methods.

---

### Task 5: Create CHANGELOG.md and LICENSE

**Files:**
- Create: `packages/sdk-unity/com.dataferret.analytics/CHANGELOG.md`
- Create: `packages/sdk-unity/com.dataferret.analytics/LICENSE`

**Step 1: Write CHANGELOG.md**

Keep a Changelog format, initial `[0.1.0] - Unreleased` entry with "Initial package scaffold".

**Step 2: Write LICENSE**

MIT license, copyright CookApps.

---

### Task 6: Verify and commit

**Step 1: Verify complete structure**

```bash
find packages/sdk-unity -type f | sort
```

Expected files (15 total):
- 10 `.gitkeep`
- `package.json`
- 4 `.asmdef`
- `README.md`
- `CHANGELOG.md`
- `LICENSE`

**Step 2: Set TaskMaster status to done**

```
mcp taskmaster set-task-status --id 1 --status done
```

**Step 3: Commit**

```bash
git add packages/sdk-unity/
git commit -m "feat(sdk-unity): scaffold UPM package structure

Create com.dataferret.analytics UPM package with:
- package.json (Unity 2021.3+, Newtonsoft.Json dependency)
- 4 Assembly Definition files (Runtime, Editor, EditMode, PlayMode)
- Directory structure (Core, Queue, Transport, Lifecycle, Platform, WebGL)
- README, CHANGELOG, LICENSE"
```
