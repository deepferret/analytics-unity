# DataFerret Analytics SDK for Unity

Event analytics SDK for Unity games. Track player behavior, sessions, errors, and performance with automatic collection. Integrates with the DataFerret platform for AI-powered analysis.

## Requirements

- Unity 2021.3 LTS or later
- Newtonsoft.Json for Unity (`com.unity.nuget.newtonsoft-json` 3.2.1)

## Installation

### Option 1: Git URL (Recommended)

Open Unity Package Manager (`Window > Package Manager > + > Add package from git URL`) and enter:

```
https://github.com/dataferret/analytics-unity.git
```

To pin a specific version:

```
https://github.com/dataferret/analytics-unity.git#v0.1.0
```

Or edit `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.dataferret.analytics": "https://github.com/dataferret/analytics-unity.git#v0.1.0"
  }
}
```

### Option 2: OpenUPM

```bash
openupm add com.dataferret.analytics
```

## Quick Start

```csharp
using DataFerret.Analytics;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        DataFerretAnalytics.Init(new DataFerretConfig
        {
            WriteKey = "wk_proj_YOUR_KEY_HERE",
        });
    }

    public void OnStageClear(int stageId, int stars)
    {
        DataFerretAnalytics.Track("stage_cleared", new Dictionary<string, object>
        {
            { "stage_id", stageId },
            { "stars", stars },
        });
    }

    public void OnLogin(string userId)
    {
        DataFerretAnalytics.Identify(userId);
    }
}
```

## Auto-Captured Events

| Event | Description | Default |
|-------|-------------|---------|
| `session_start` / `session_end` | Session lifecycle | Enabled |
| `screen` (scene name) | Scene transitions | Enabled |
| `error_occurred` | Exceptions and errors | Enabled |
| `app_backgrounded` / `app_foregrounded` | App lifecycle | Enabled |
| `performance_sample` | FPS, memory usage | Opt-in |

## Documentation

Full documentation: https://docs.dataferret.io/sdk/unity

## License

MIT License. See [LICENSE](LICENSE) for details.
