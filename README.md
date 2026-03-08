# DataFerret Analytics SDK for Unity

> Unity 게임을 위한 이벤트 분석 SDK

플레이어 행동 이벤트를 수집하여 DataFerret Collector API로 전송합니다. DataFerret 플랫폼에서 AI 기반 분석을 활용할 수 있습니다.

## 요구 사항

- Unity 2021.3 LTS 이상
- .NET Standard 2.1
- `com.unity.nuget.newtonsoft-json` 3.2.1 (자동 설치)

## 설치

### UPM Git URL (권장)

1. Unity 에디터에서 **Window > Package Manager** 열기
2. **+** 버튼 > **Add package from git URL** 선택
3. 아래 URL 입력:

```
https://github.com/dataferret/analytics-unity.git#v0.1.0
```

또는 `Packages/manifest.json`에 직접 추가:

```json
{
  "dependencies": {
    "com.dataferret.analytics": "https://github.com/dataferret/analytics-unity.git#v0.1.0"
  }
}
```

### OpenUPM

```bash
openupm add com.dataferret.analytics
```

### 로컬 개발

```json
{
  "dependencies": {
    "com.dataferret.analytics": "file:/path/to/cloned/analytics-unity"
  }
}
```

## 빠른 시작

```csharp
using DataFerret.Analytics;
using System.Collections.Generic;
using UnityEngine;

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
        DataFerretAnalytics.Identify(userId, new Dictionary<string, object>
        {
            { "level", 25 },
        });
    }

    public void OnLogout()
    {
        DataFerretAnalytics.Reset();
    }
}
```

## API 레퍼런스

| 메서드 | 설명 |
|--------|------|
| `Init(DataFerretConfig config)` | SDK 초기화. WriteKey 필수 (`wk_` 접두사) |
| `Track(string eventName, Dictionary<string, object> properties)` | 커스텀 이벤트 추적 |
| `Identify(string userId, Dictionary<string, object> traits)` | 유저 식별 |
| `Screen(string screenName, Dictionary<string, object> properties)` | 씬/화면 전환 추적 |
| `Group(string groupId, Dictionary<string, object> traits)` | 그룹(길드, 팀 등) 연결 |
| `SetGlobalProperties(Dictionary<string, object> properties)` | 전역 속성 설정 |
| `Reset()` | userId 초기화 + 새 anonymousId 생성 |
| `Flush()` | 큐에 쌓인 이벤트 즉시 전송 |

## 설정 옵션

```csharp
new DataFerretConfig
{
    WriteKey = "wk_proj_xxxxxxxx",             // 필수
    ApiHost = "https://collect.dataferret.io", // 기본값
    FlushSize = 20,                            // 배치 크기
    FlushIntervalSeconds = 30f,                // 자동 플러시 간격
    MaxQueueSize = 1000,                       // 최대 큐 크기
    MaxRetries = 3,                            // 재시도 횟수
    TimeoutSeconds = 10,                       // HTTP 타임아웃
    Debug = false,                             // 디버그 로그
    AutoCapture = new DataFerretConfig.AutoCaptureConfig
    {
        Sessions = true,                       // 세션 시작/종료
        SceneChanges = true,                   // 씬 전환
        Errors = true,                         // 예외/에러
        Lifecycle = true,                      // 앱 백그라운드/포그라운드
        PerformanceSamples = false,            // FPS/메모리 (opt-in)
    },
};
```

## 자동 수집 이벤트

| 이벤트 | 설명 | 설정 | 기본값 |
|--------|------|------|--------|
| `session_start` / `session_end` | 세션 시작/종료 (30분 타임아웃) | `Sessions` | 활성 |
| `screen` | 씬 전환 시 (previous_scene 포함) | `SceneChanges` | 활성 |
| `error_occurred` | 예외/에러 발생 시 (60초 중복 제거) | `Errors` | 활성 |
| `app_backgrounded` / `app_foregrounded` | 앱 라이프사이클 | `Lifecycle` | 활성 |
| `performance_sample` | FPS, 메모리 사용량 (30초 주기) | `PerformanceSamples` | 비활성 |

## 지원 플랫폼

| 플랫폼 | HTTP 전송 | 오프라인 큐 |
|--------|-----------|------------|
| Android | UnityWebRequest | .jsonl 파일 (5MB) |
| iOS | UnityWebRequest | .jsonl 파일 (5MB) |
| WebGL | fetch() / sendBeacon() | PlayerPrefs (200건) |
| Windows / macOS / Linux | UnityWebRequest | .jsonl 파일 (5MB) |

## 오프라인 지원

네트워크 연결이 불가능할 때 이벤트는 자동으로 로컬에 저장됩니다. 다음 앱 시작 시 자동으로 큐에 복원되어 재전송됩니다.

## 성능

- Track() 1회 호출당 GC Alloc < 1KB
- EventEnvelope 오브젝트 풀링
- 핫 패스에서 LINQ 미사용
- IL2CPP 빌드 지원 (link.xml 포함)

## 문서

전체 문서: https://docs.dataferret.io/sdk/unity

## 라이선스

MIT License. See [LICENSE](LICENSE) for details.
