# Task 2: 이벤트 데이터 모델 테스트 아키텍처 설계

## 1. 테스트 구조 및 디렉토리 레이아웃

### 디렉토리 구조

```
Tests/EditMode/
├── DataFerret.Analytics.Tests.EditMode.asmdef   (기존)
├── Core/
│   ├── EventEnvelopeTests.cs      -- EventEnvelope 직렬화, Reset, 필수 필드 검증
│   ├── EventContextTests.cs       -- EventContext 및 하위 Context 직렬화 검증
│   └── EventTypeTests.cs          -- EventType enum 값/문자열 매핑 검증
└── TestUtilities/
    └── EventTestFactory.cs        -- 테스트 데이터 팩토리 (공용)
```

### 네이밍 규칙

| 항목 | 규칙 | 예시 |
|------|------|------|
| 테스트 클래스 | `{대상클래스}Tests` | `EventEnvelopeTests` |
| 테스트 메서드 | `{Method}_{Scenario}_{ExpectedResult}` | `Serialize_WithNullUserId_OmitsUserIdField` |
| 테스트 카테고리 | `[Category("Core")]` | 전체 데이터 모델 테스트에 적용 |
| 네임스페이스 | `DataFerret.Analytics.Tests.EditMode` | asmdef rootNamespace와 일치 |

---

## 2. 테스트 대상 클래스별 테스트 케이스 설계

### 2.1 EventEnvelopeTests (12개 테스트)

#### 직렬화/역직렬화 (Serialization)

| ID | 메서드명 | 검증 내용 |
|----|---------|-----------|
| TC-EE-001 | `Serialize_AllFieldsPopulated_ProducesValidJson` | 모든 필드가 채워진 envelope을 직렬화하면 유효한 JSON 문자열이 생성되는지 검증 |
| TC-EE-002 | `Serialize_WithNullUserId_OmitsUserIdField` | `UserId`가 null이면 JSON 출력에 `"userId"` 키가 포함되지 않음 (NullValueHandling.Ignore) |
| TC-EE-003 | `Serialize_WithNullEvent_OmitsEventField` | `Event`가 null이면 JSON 출력에 `"event"` 키가 포함되지 않음 |
| TC-EE-004 | `Serialize_WithNullProperties_OmitsPropertiesField` | `Properties`가 null이면 JSON 출력에 `"properties"` 키가 포함되지 않음 |
| TC-EE-005 | `Serialize_RequiredFields_AlwaysPresent` | `eventId`, `type`, `timestamp`, `anonymousId`, `context` 키가 항상 JSON에 존재 |
| TC-EE-006 | `Serialize_JsonPropertyNames_AreCamelCase` | 모든 JSON 키가 camelCase (eventId, anonymousId 등) |
| TC-EE-007 | `Deserialize_ValidJson_ReconstructsEnvelope` | 유효한 JSON 문자열에서 EventEnvelope 역직렬화 후 필드값 일치 검증 |

#### Timestamp 형식

| ID | 메서드명 | 검증 내용 |
|----|---------|-----------|
| TC-EE-008 | `Timestamp_WhenSet_IsIso8601UtcFormat` | timestamp 필드가 ISO 8601 UTC 형식(`yyyy-MM-ddTHH:mm:ss.fffffffZ`) 파싱 가능한지 검증 |

#### Reset (Object Pooling)

| ID | 메서드명 | 검증 내용 |
|----|---------|-----------|
| TC-EE-009 | `Reset_AfterPopulation_ClearsAllFields` | 모든 필드를 채운 후 `Reset()` 호출 시 string 필드는 null, Properties는 null, Context는 null |
| TC-EE-010 | `Reset_CalledTwice_NoException` | Reset()을 연속 두 번 호출해도 예외 없음 (멱등성) |

#### Edge Cases

| ID | 메서드명 | 검증 내용 |
|----|---------|-----------|
| TC-EE-011 | `Serialize_WithEmptyProperties_IncludesEmptyObject` | Properties가 빈 Dictionary이면 `"properties": {}` 로 출력 (null과 구분) |
| TC-EE-012 | `Serialize_PropertiesWithNestedObject_SerializesCorrectly` | Properties에 중첩 Dictionary/List가 포함되어도 정상 직렬화 |

---

### 2.2 EventContextTests (10개 테스트)

#### LibraryContext

| ID | 메서드명 | 검증 내용 |
|----|---------|-----------|
| TC-EC-001 | `LibraryContext_Serialize_ContainsNameAndVersion` | `name`과 `version` 필드가 JSON에 존재 |
| TC-EC-002 | `LibraryContext_DefaultValues_NameIsPackageName` | name의 기본값이 `"com.dataferret.analytics"` |

#### DeviceContext

| ID | 메서드명 | 검증 내용 |
|----|---------|-----------|
| TC-EC-003 | `DeviceContext_Serialize_ContainsAllFields` | `type`, `model`, `gpu`, `os` 4개 필드 모두 JSON에 존재 |
| TC-EC-004 | `DeviceContext_WithNullFields_OmitsNullValues` | null 필드는 JSON에서 생략 |

#### GameContext

| ID | 메서드명 | 검증 내용 |
|----|---------|-----------|
| TC-EC-005 | `GameContext_Serialize_ContainsAllFields` | `engine`, `engineVersion`, `appVersion`, `platform`, `scene`, `store` 필드 존재 |
| TC-EC-006 | `GameContext_WithNullOptionalFields_OmitsNulls` | `scene`, `store` 등 선택 필드가 null이면 생략 |

#### EventContext 통합

| ID | 메서드명 | 검증 내용 |
|----|---------|-----------|
| TC-EC-007 | `EventContext_Serialize_ContainsLibraryDeviceGame` | 최상위 `library`, `device`, `game` 키가 존재 |
| TC-EC-008 | `EventContext_Deserialize_ReconstructsNestedContexts` | JSON에서 역직렬화 후 하위 Context 객체가 올바르게 복원 |
| TC-EC-009 | `EventContext_WithNullDevice_OmitsDeviceField` | DeviceContext가 null이면 `"device"` 키 생략 |
| TC-EC-010 | `EventContext_WithNullGame_OmitsGameField` | GameContext가 null이면 `"game"` 키 생략 |

---

### 2.3 EventTypeTests (4개 테스트)

| ID | 메서드명 | 검증 내용 |
|----|---------|-----------|
| TC-ET-001 | `EventType_HasExpectedValues` | Identify, Track, Page, Screen, Group 5개 값 존재 |
| TC-ET-002 | `EventType_Count_IsFive` | enum 값의 총 개수가 5개 |
| TC-ET-003 | `EventType_ToString_ReturnsExpectedNames` | 각 값의 `ToString()`이 PascalCase 이름과 일치 |
| TC-ET-004 | `EventType_UsedInEnvelope_SerializesAsLowercaseString` | EventEnvelope의 Type 필드에 사용 시 소문자 문자열로 직렬화 (`"track"`, `"identify"` 등) |

---

## 3. Fixture 설계: 공용 셋업, 팩토리, 상수

### 3.1 TestUtilities/EventTestFactory.cs

테스트 데이터를 일관되게 생성하는 정적 팩토리 클래스입니다. 각 테스트 클래스에서 공통으로 사용합니다.

```csharp
namespace DataFerret.Analytics.Tests.EditMode
{
    /// <summary>
    /// 테스트용 EventEnvelope, EventContext 객체를 생성하는 팩토리.
    /// 모든 테스트에서 일관된 테스트 데이터를 사용하기 위한 단일 소스.
    /// </summary>
    public static class EventTestFactory
    {
        // --- 상수 (테스트 전체에서 재사용) ---
        public const string ValidEventId = "0190a6e0-7b3a-7000-8000-000000000001";
        public const string ValidTimestamp = "2026-03-07T12:00:00.0000000Z";
        public const string ValidAnonymousId = "550e8400-e29b-41d4-a716-446655440000";
        public const string ValidUserId = "user-123";
        public const string ValidEventName = "button_clicked";
        public const string ValidWriteKey = "wk_test_abc123";

        /// <summary>
        /// 모든 필드가 채워진 완전한 EventEnvelope 생성.
        /// 직렬화 통합 테스트, 필수 필드 검증에 사용.
        /// </summary>
        public static EventEnvelope CreateFullEnvelope()
        {
            return new EventEnvelope
            {
                EventId = ValidEventId,
                Type = "track",
                Timestamp = ValidTimestamp,
                AnonymousId = ValidAnonymousId,
                UserId = ValidUserId,
                Event = ValidEventName,
                Properties = CreateSampleProperties(),
                Context = CreateFullContext()
            };
        }

        /// <summary>
        /// 필수 필드만 채워진 최소 EventEnvelope 생성.
        /// null 필드 생략 테스트에 사용.
        /// </summary>
        public static EventEnvelope CreateMinimalEnvelope()
        {
            return new EventEnvelope
            {
                EventId = ValidEventId,
                Type = "track",
                Timestamp = ValidTimestamp,
                AnonymousId = ValidAnonymousId,
                Context = CreateMinimalContext()
            };
            // UserId = null, Event = null, Properties = null
        }

        /// <summary>
        /// 빈 Properties Dictionary를 가진 envelope (null과 구분 테스트용).
        /// </summary>
        public static EventEnvelope CreateEnvelopeWithEmptyProperties()
        {
            var envelope = CreateMinimalEnvelope();
            envelope.Properties = new Dictionary<string, object>();
            return envelope;
        }

        /// <summary>
        /// 중첩 객체를 포함하는 Properties를 가진 envelope.
        /// </summary>
        public static EventEnvelope CreateEnvelopeWithNestedProperties()
        {
            var envelope = CreateMinimalEnvelope();
            envelope.Properties = new Dictionary<string, object>
            {
                { "simple", "value" },
                { "number", 42 },
                { "nested", new Dictionary<string, object>
                    {
                        { "inner_key", "inner_value" },
                        { "inner_list", new List<object> { 1, 2, 3 } }
                    }
                }
            };
            return envelope;
        }

        public static Dictionary<string, object> CreateSampleProperties()
        {
            return new Dictionary<string, object>
            {
                { "button_name", "start_game" },
                { "level", 5 },
                { "is_premium", true }
            };
        }

        // --- Context 팩토리 ---

        public static EventContext CreateFullContext()
        {
            return new EventContext
            {
                Library = CreateLibraryContext(),
                Device = CreateDeviceContext(),
                Game = CreateGameContext()
            };
        }

        public static EventContext CreateMinimalContext()
        {
            return new EventContext
            {
                Library = CreateLibraryContext()
            };
            // Device = null, Game = null
        }

        public static LibraryContext CreateLibraryContext()
        {
            return new LibraryContext
            {
                Name = "com.dataferret.analytics",
                Version = "0.1.0"
            };
        }

        public static DeviceContext CreateDeviceContext()
        {
            return new DeviceContext
            {
                Type = "Desktop",
                Model = "MacBookPro18,1",
                Gpu = "Apple M1 Pro",
                Os = "macOS 14.5"
            };
        }

        public static GameContext CreateGameContext()
        {
            return new GameContext
            {
                Engine = "Unity",
                EngineVersion = "2021.3.0f1",
                AppVersion = "1.0.0",
                Platform = "OSXEditor",
                Scene = "MainMenu",
                Store = "steam"
            };
        }

        // --- Edge Case 데이터 ---

        /// <summary>
        /// 유니코드, 특수문자, 긴 문자열 등 경계 조건 Properties.
        /// </summary>
        public static Dictionary<string, object> CreateEdgeCaseProperties()
        {
            return new Dictionary<string, object>
            {
                { "unicode", "한국어 테스트 \U0001F600" },
                { "empty_string", "" },
                { "max_length_key_" + new string('x', 180), "value" },
                { "special_chars", "line1\nline2\ttab\"quote" },
                { "null_value", null },
                { "zero", 0 },
                { "negative", -1 },
                { "large_number", long.MaxValue },
                { "float_precision", 3.141592653589793 }
            };
        }
    }
}
```

### 3.2 SetUp / TearDown 패턴

각 테스트 클래스에서 `[SetUp]`으로 `JsonSerializerSettings`를 초기화합니다. 외부 상태(파일, PlayerPrefs 등)에 의존하지 않으므로 `[TearDown]`은 불필요합니다.

```csharp
[TestFixture]
[Category("Core")]
public class EventEnvelopeTests
{
    private JsonSerializerSettings _serializerSettings;

    [SetUp]
    public void SetUp()
    {
        _serializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };
    }

    // ... 테스트 메서드들
}
```

---

## 4. Mock / Stub 전략

### 이 태스크에서 Mock이 불필요한 이유

Task 2의 테스트 대상(EventEnvelope, EventContext, EventType)은 **순수 데이터 모델**입니다:

- Unity API 호출 없음 (SystemInfo, PlayerPrefs, SceneManager 등 미사용)
- 네트워크, 파일시스템 의존성 없음
- 다른 SDK 컴포넌트(EventBuilder, IdentityManager 등)에 의존하지 않음

따라서 **Mock/Stub 없이 순수 단위 테스트**로 작성합니다. 이것이 EditMode 테스트의 가장 큰 장점입니다.

### 향후 태스크(Task 3+)에서의 Mock 경계

Task 3(EventBuilder)부터는 `PlatformInfo` 등 Unity API 래퍼를 사용하므로, 해당 시점에서 interface 추출(`IPlatformInfo`) 및 Mock 전략이 필요합니다. Task 2의 데이터 모델 테스트는 그 기반이 됩니다.

---

## 5. 테스트 데이터 전략

### 5.1 데이터 범주

| 범주 | 설명 | 사용처 |
|------|------|--------|
| **Happy Path** | 모든 필드가 유효한 완전한 객체 | `CreateFullEnvelope()`, `CreateFullContext()` |
| **Minimal** | 필수 필드만 채운 최소 객체 | `CreateMinimalEnvelope()` -- null 생략 검증 |
| **Empty vs Null** | 빈 컬렉션 vs null 구분 | `CreateEnvelopeWithEmptyProperties()` |
| **Nested** | 중첩 구조 직렬화 | `CreateEnvelopeWithNestedProperties()` |
| **Edge Case** | 유니코드, 특수문자, 경계값 | `CreateEdgeCaseProperties()` |

### 5.2 직렬화 검증 전략

JSON 출력을 문자열 비교하지 않고, **역직렬화 후 필드 비교** 또는 **JObject 파싱 후 키 존재/부재 확인** 방식을 사용합니다. 이렇게 하면 필드 순서 변경에 영향받지 않습니다.

```csharp
// 권장: JObject 파싱 방식 (필드 존재/부재 검증에 적합)
var json = JsonConvert.SerializeObject(envelope, _serializerSettings);
var jObj = JObject.Parse(json);

Assert.That(jObj.ContainsKey("eventId"), Is.True);
Assert.That(jObj.ContainsKey("userId"), Is.False);  // null이므로 생략됨
```

```csharp
// 권장: 라운드트립 방식 (값 보존 검증에 적합)
var json = JsonConvert.SerializeObject(envelope, _serializerSettings);
var restored = JsonConvert.DeserializeObject<EventEnvelope>(json);

Assert.That(restored.EventId, Is.EqualTo(envelope.EventId));
Assert.That(restored.Context.Library.Name, Is.EqualTo("com.dataferret.analytics"));
```

---

## 6. Framework 설정 및 실행 가이드

### 6.1 asmdef 설정 (기존 - 변경 불필요)

`Tests/EditMode/DataFerret.Analytics.Tests.EditMode.asmdef`는 이미 올바르게 설정되어 있습니다:
- `DataFerret.Analytics` 참조 (Runtime 코드 접근)
- `nunit.framework.dll` precompiledReference
- `Editor` 플랫폼 한정
- `UNITY_INCLUDE_TESTS` defineConstraint

### 6.2 NUnit 속성 사용 규칙

```csharp
[TestFixture]                           // 모든 테스트 클래스에 필수
[Category("Core")]                      // 테스트 카테고리 (Core, Queue, Transport 등)
public class EventEnvelopeTests
{
    [SetUp]                             // 각 테스트 전 실행
    public void SetUp() { }

    [Test]                              // 일반 테스트
    public void Method_Scenario_Expected() { }

    [TestCase("track")]                 // 파라미터화 테스트
    [TestCase("identify")]
    [TestCase("screen")]
    public void Type_ValidValues_Accepted(string type) { }
}
```

### 6.3 Assertion 스타일

NUnit 3.x **Constraint Model** (`Assert.That`) 사용을 원칙으로 합니다. Classic Model(`Assert.AreEqual`)은 사용하지 않습니다.

```csharp
// O (Constraint Model)
Assert.That(envelope.EventId, Is.EqualTo("expected-id"));
Assert.That(envelope.UserId, Is.Null);
Assert.That(json, Does.Contain("\"eventId\""));
Assert.That(values, Has.Length.EqualTo(5));

// X (Classic Model - 사용 금지)
Assert.AreEqual("expected-id", envelope.EventId);
Assert.IsNull(envelope.UserId);
```

### 6.4 실행 방법

```
Unity Editor > Window > General > Test Runner > EditMode 탭 > Run All
```

또는 Category 필터로 `Core` 카테고리만 실행 가능합니다.

---

## 7. 전체 테스트 파일 스켈레톤

### 7.1 EventEnvelopeTests.cs

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Core")]
    public class EventEnvelopeTests
    {
        private JsonSerializerSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        // --- Serialization ---

        [Test]
        public void Serialize_AllFieldsPopulated_ProducesValidJson()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            Assert.That(() => JObject.Parse(json), Throws.Nothing);
        }

        [Test]
        public void Serialize_WithNullUserId_OmitsUserIdField()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);
            Assert.That(jObj.ContainsKey("userId"), Is.False);
        }

        [Test]
        public void Serialize_WithNullEvent_OmitsEventField()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);
            Assert.That(jObj.ContainsKey("event"), Is.False);
        }

        [Test]
        public void Serialize_WithNullProperties_OmitsPropertiesField()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);
            Assert.That(jObj.ContainsKey("properties"), Is.False);
        }

        [Test]
        public void Serialize_RequiredFields_AlwaysPresent()
        {
            var envelope = EventTestFactory.CreateMinimalEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("eventId"), Is.True, "eventId missing");
            Assert.That(jObj.ContainsKey("type"), Is.True, "type missing");
            Assert.That(jObj.ContainsKey("timestamp"), Is.True, "timestamp missing");
            Assert.That(jObj.ContainsKey("anonymousId"), Is.True, "anonymousId missing");
            Assert.That(jObj.ContainsKey("context"), Is.True, "context missing");
        }

        [Test]
        public void Serialize_JsonPropertyNames_AreCamelCase()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);

            // 모든 최상위 키가 camelCase인지 확인
            // (첫 글자 소문자, PascalCase 아님)
            foreach (var prop in jObj.Properties())
            {
                Assert.That(char.IsLower(prop.Name[0]), Is.True,
                    $"Property '{prop.Name}' is not camelCase");
            }
        }

        [Test]
        public void Deserialize_ValidJson_ReconstructsEnvelope()
        {
            var original = EventTestFactory.CreateFullEnvelope();
            var json = JsonConvert.SerializeObject(original, _settings);
            var restored = JsonConvert.DeserializeObject<EventEnvelope>(json);

            Assert.That(restored.EventId, Is.EqualTo(original.EventId));
            Assert.That(restored.Type, Is.EqualTo(original.Type));
            Assert.That(restored.Timestamp, Is.EqualTo(original.Timestamp));
            Assert.That(restored.AnonymousId, Is.EqualTo(original.AnonymousId));
            Assert.That(restored.UserId, Is.EqualTo(original.UserId));
            Assert.That(restored.Event, Is.EqualTo(original.Event));
        }

        // --- Timestamp ---

        [Test]
        public void Timestamp_WhenSet_IsIso8601UtcFormat()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            Assert.That(
                DateTimeOffset.TryParse(envelope.Timestamp, out var dto), Is.True,
                "Timestamp should be parseable as ISO 8601");
            Assert.That(dto.Offset, Is.EqualTo(TimeSpan.Zero),
                "Timestamp should be UTC");
        }

        // --- Reset (Object Pooling) ---

        [Test]
        public void Reset_AfterPopulation_ClearsAllFields()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            envelope.Reset();

            Assert.That(envelope.EventId, Is.Null);
            Assert.That(envelope.Type, Is.Null);
            Assert.That(envelope.Timestamp, Is.Null);
            Assert.That(envelope.AnonymousId, Is.Null);
            Assert.That(envelope.UserId, Is.Null);
            Assert.That(envelope.Event, Is.Null);
            Assert.That(envelope.Properties, Is.Null);
            Assert.That(envelope.Context, Is.Null);
        }

        [Test]
        public void Reset_CalledTwice_NoException()
        {
            var envelope = EventTestFactory.CreateFullEnvelope();
            Assert.That(() =>
            {
                envelope.Reset();
                envelope.Reset();
            }, Throws.Nothing);
        }

        // --- Edge Cases ---

        [Test]
        public void Serialize_WithEmptyProperties_IncludesEmptyObject()
        {
            var envelope = EventTestFactory.CreateEnvelopeWithEmptyProperties();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("properties"), Is.True,
                "Empty properties should be serialized (not omitted like null)");
            Assert.That(jObj["properties"].Type, Is.EqualTo(JTokenType.Object));
            Assert.That(jObj["properties"].HasValues, Is.False);
        }

        [Test]
        public void Serialize_PropertiesWithNestedObject_SerializesCorrectly()
        {
            var envelope = EventTestFactory.CreateEnvelopeWithNestedProperties();
            var json = JsonConvert.SerializeObject(envelope, _settings);
            var jObj = JObject.Parse(json);

            var props = jObj["properties"];
            Assert.That(props, Is.Not.Null);
            Assert.That(props["simple"]?.Value<string>(), Is.EqualTo("value"));
            Assert.That(props["number"]?.Value<int>(), Is.EqualTo(42));
            Assert.That(props["nested"]?["inner_key"]?.Value<string>(),
                Is.EqualTo("inner_value"));
        }
    }
}
```

### 7.2 EventContextTests.cs

```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Core")]
    public class EventContextTests
    {
        private JsonSerializerSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        // --- LibraryContext ---

        [Test]
        public void LibraryContext_Serialize_ContainsNameAndVersion()
        {
            var lib = EventTestFactory.CreateLibraryContext();
            var json = JsonConvert.SerializeObject(lib, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("name"), Is.True);
            Assert.That(jObj.ContainsKey("version"), Is.True);
        }

        [Test]
        public void LibraryContext_DefaultValues_NameIsPackageName()
        {
            var lib = EventTestFactory.CreateLibraryContext();
            Assert.That(lib.Name, Is.EqualTo("com.dataferret.analytics"));
        }

        // --- DeviceContext ---

        [Test]
        public void DeviceContext_Serialize_ContainsAllFields()
        {
            var device = EventTestFactory.CreateDeviceContext();
            var json = JsonConvert.SerializeObject(device, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("type"), Is.True);
            Assert.That(jObj.ContainsKey("model"), Is.True);
            Assert.That(jObj.ContainsKey("gpu"), Is.True);
            Assert.That(jObj.ContainsKey("os"), Is.True);
        }

        [Test]
        public void DeviceContext_WithNullFields_OmitsNullValues()
        {
            var device = new DeviceContext { Type = "Desktop" };
            // model, gpu, os are null
            var json = JsonConvert.SerializeObject(device, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("type"), Is.True);
            Assert.That(jObj.ContainsKey("model"), Is.False);
            Assert.That(jObj.ContainsKey("gpu"), Is.False);
            Assert.That(jObj.ContainsKey("os"), Is.False);
        }

        // --- GameContext ---

        [Test]
        public void GameContext_Serialize_ContainsAllFields()
        {
            var game = EventTestFactory.CreateGameContext();
            var json = JsonConvert.SerializeObject(game, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("engine"), Is.True);
            Assert.That(jObj.ContainsKey("engineVersion"), Is.True);
            Assert.That(jObj.ContainsKey("appVersion"), Is.True);
            Assert.That(jObj.ContainsKey("platform"), Is.True);
            Assert.That(jObj.ContainsKey("scene"), Is.True);
            Assert.That(jObj.ContainsKey("store"), Is.True);
        }

        [Test]
        public void GameContext_WithNullOptionalFields_OmitsNulls()
        {
            var game = new GameContext
            {
                Engine = "Unity",
                EngineVersion = "2021.3.0f1"
            };
            var json = JsonConvert.SerializeObject(game, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("engine"), Is.True);
            Assert.That(jObj.ContainsKey("scene"), Is.False);
            Assert.That(jObj.ContainsKey("store"), Is.False);
        }

        // --- EventContext 통합 ---

        [Test]
        public void EventContext_Serialize_ContainsLibraryDeviceGame()
        {
            var ctx = EventTestFactory.CreateFullContext();
            var json = JsonConvert.SerializeObject(ctx, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("library"), Is.True);
            Assert.That(jObj.ContainsKey("device"), Is.True);
            Assert.That(jObj.ContainsKey("game"), Is.True);
        }

        [Test]
        public void EventContext_Deserialize_ReconstructsNestedContexts()
        {
            var original = EventTestFactory.CreateFullContext();
            var json = JsonConvert.SerializeObject(original, _settings);
            var restored = JsonConvert.DeserializeObject<EventContext>(json);

            Assert.That(restored.Library, Is.Not.Null);
            Assert.That(restored.Library.Name,
                Is.EqualTo(original.Library.Name));
            Assert.That(restored.Device, Is.Not.Null);
            Assert.That(restored.Device.Model,
                Is.EqualTo(original.Device.Model));
            Assert.That(restored.Game, Is.Not.Null);
            Assert.That(restored.Game.Engine,
                Is.EqualTo(original.Game.Engine));
        }

        [Test]
        public void EventContext_WithNullDevice_OmitsDeviceField()
        {
            var ctx = EventTestFactory.CreateMinimalContext();
            var json = JsonConvert.SerializeObject(ctx, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("library"), Is.True);
            Assert.That(jObj.ContainsKey("device"), Is.False);
        }

        [Test]
        public void EventContext_WithNullGame_OmitsGameField()
        {
            var ctx = EventTestFactory.CreateMinimalContext();
            var json = JsonConvert.SerializeObject(ctx, _settings);
            var jObj = JObject.Parse(json);

            Assert.That(jObj.ContainsKey("game"), Is.False);
        }
    }
}
```

### 7.3 EventTypeTests.cs

```csharp
using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DataFerret.Analytics.Tests.EditMode
{
    [TestFixture]
    [Category("Core")]
    public class EventTypeTests
    {
        [Test]
        public void EventType_HasExpectedValues()
        {
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Identify), Is.True);
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Track), Is.True);
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Page), Is.True);
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Screen), Is.True);
            Assert.That(Enum.IsDefined(typeof(EventType), EventType.Group), Is.True);
        }

        [Test]
        public void EventType_Count_IsFive()
        {
            var values = Enum.GetValues(typeof(EventType));
            Assert.That(values, Has.Length.EqualTo(5));
        }

        [TestCase(EventType.Identify, "Identify")]
        [TestCase(EventType.Track, "Track")]
        [TestCase(EventType.Page, "Page")]
        [TestCase(EventType.Screen, "Screen")]
        [TestCase(EventType.Group, "Group")]
        public void EventType_ToString_ReturnsExpectedNames(
            EventType type, string expectedName)
        {
            Assert.That(type.ToString(), Is.EqualTo(expectedName));
        }

        [TestCase(EventType.Track, "track")]
        [TestCase(EventType.Identify, "identify")]
        [TestCase(EventType.Screen, "screen")]
        [TestCase(EventType.Group, "group")]
        [TestCase(EventType.Page, "page")]
        public void EventType_UsedInEnvelope_SerializesAsLowercaseString(
            EventType type, string expectedLowercase)
        {
            // EventEnvelope.Type은 string 필드이므로,
            // enum을 string으로 변환할 때 소문자를 사용해야 함
            var typeString = type.ToString().ToLowerInvariant();
            Assert.That(typeString, Is.EqualTo(expectedLowercase));
        }
    }
}
```

---

## 8. 테스트 수 요약

| 파일 | 테스트 수 | 카테고리 |
|------|-----------|----------|
| `EventEnvelopeTests.cs` | 12 | Core |
| `EventContextTests.cs` | 10 | Core |
| `EventTypeTests.cs` | 4 (+ TestCase 확장 = 14 개별 실행) | Core |
| **합계** | **26 테스트 메서드** | |

---

## 9. 프로덕션 코드 구현 시 테스트가 기대하는 계약 (Contract)

테스트 코드가 컴파일되려면 프로덕션 코드가 다음 최소 인터페이스를 충족해야 합니다:

### EventEnvelope 필수 구현

```csharp
namespace DataFerret.Analytics
{
    [System.Serializable]
    public class EventEnvelope
    {
        [JsonProperty("eventId")]
        public string EventId;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("timestamp")]
        public string Timestamp;

        [JsonProperty("anonymousId")]
        public string AnonymousId;

        [JsonProperty("userId", NullValueHandling = NullValueHandling.Ignore)]
        public string UserId;

        [JsonProperty("event", NullValueHandling = NullValueHandling.Ignore)]
        public string Event;

        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Properties;

        [JsonProperty("context")]
        public EventContext Context;

        public void Reset();
    }
}
```

### EventContext 필수 구현

```csharp
namespace DataFerret.Analytics
{
    [System.Serializable]
    public class EventContext
    {
        [JsonProperty("library")]
        public LibraryContext Library;

        [JsonProperty("device", NullValueHandling = NullValueHandling.Ignore)]
        public DeviceContext Device;

        [JsonProperty("game", NullValueHandling = NullValueHandling.Ignore)]
        public GameContext Game;
    }

    [System.Serializable]
    public class LibraryContext
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("version")] public string Version;
    }

    [System.Serializable]
    public class DeviceContext
    {
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)] public string Type;
        [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)] public string Model;
        [JsonProperty("gpu", NullValueHandling = NullValueHandling.Ignore)] public string Gpu;
        [JsonProperty("os", NullValueHandling = NullValueHandling.Ignore)] public string Os;
    }

    [System.Serializable]
    public class GameContext
    {
        [JsonProperty("engine", NullValueHandling = NullValueHandling.Ignore)] public string Engine;
        [JsonProperty("engineVersion", NullValueHandling = NullValueHandling.Ignore)] public string EngineVersion;
        [JsonProperty("appVersion", NullValueHandling = NullValueHandling.Ignore)] public string AppVersion;
        [JsonProperty("platform", NullValueHandling = NullValueHandling.Ignore)] public string Platform;
        [JsonProperty("scene", NullValueHandling = NullValueHandling.Ignore)] public string Scene;
        [JsonProperty("store", NullValueHandling = NullValueHandling.Ignore)] public string Store;
    }
}
```

### EventType 필수 구현

```csharp
namespace DataFerret.Analytics
{
    public enum EventType
    {
        Identify,
        Track,
        Page,
        Screen,
        Group
    }
}
```

---

## 10. TDD 워크플로우

1. **Red**: 위 테스트 파일 3개 + 팩토리 1개를 먼저 작성 (컴파일 불가 상태)
2. **Green**: 프로덕션 코드(EventEnvelope.cs, EventContext.cs, EventType.cs) 최소 구현으로 테스트 통과
3. **Refactor**: JsonProperty 속성 정리, Reset() 구현 최적화, XML 문서화 추가

이 순서를 따르면 테스트가 프로덕션 코드의 설계를 이끌고, 모든 공개 계약이 테스트로 보호됩니다.
