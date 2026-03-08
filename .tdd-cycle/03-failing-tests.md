# Phase 2 (RED): 실패하는 테스트 목록

## 생성된 테스트 파일

### 1. Tests/EditMode/TestUtilities/EventTestFactory.cs
- 공용 테스트 데이터 팩토리
- `CreateFullEnvelope()`, `CreateMinimalEnvelope()`, `CreateEnvelopeWithEmptyProperties()`, `CreateEnvelopeWithNestedProperties()`
- `CreateFullContext()`, `CreateMinimalContext()`, `CreateLibraryContext()`, `CreateDeviceContext()`, `CreateGameContext()`

### 2. Tests/EditMode/Core/EventEnvelopeTests.cs (12 tests)
| ID | 메서드명 | 실패 이유 |
|----|---------|-----------|
| 1 | `Serialize_AllFieldsPopulated_ProducesValidJson` | EventEnvelope 클래스 미존재 |
| 2 | `Serialize_WithNullUserId_OmitsUserIdField` | EventEnvelope 클래스 미존재 |
| 3 | `Serialize_WithNullEvent_OmitsEventField` | EventEnvelope 클래스 미존재 |
| 4 | `Serialize_WithNullProperties_OmitsPropertiesField` | EventEnvelope 클래스 미존재 |
| 5 | `Serialize_RequiredFields_AlwaysPresent` | EventEnvelope 클래스 미존재 |
| 6 | `Serialize_JsonPropertyNames_AreCamelCase` | EventEnvelope 클래스 미존재 |
| 7 | `Deserialize_ValidJson_ReconstructsEnvelope` | EventEnvelope 클래스 미존재 |
| 8 | `Timestamp_WhenSet_IsIso8601UtcFormat` | EventEnvelope 클래스 미존재 |
| 9 | `Reset_AfterPopulation_ClearsAllFields` | EventEnvelope 클래스, Reset() 메서드 미존재 |
| 10 | `Reset_CalledTwice_NoException` | EventEnvelope 클래스, Reset() 메서드 미존재 |
| 11 | `Serialize_WithEmptyProperties_IncludesEmptyObject` | EventEnvelope 클래스 미존재 |
| 12 | `Serialize_PropertiesWithNestedObject_SerializesCorrectly` | EventEnvelope 클래스 미존재 |

### 3. Tests/EditMode/Core/EventContextTests.cs (10 tests)
| ID | 메서드명 | 실패 이유 |
|----|---------|-----------|
| 1 | `LibraryContext_Serialize_ContainsNameAndVersion` | LibraryContext 클래스 미존재 |
| 2 | `LibraryContext_DefaultValues_NameIsPackageName` | LibraryContext 클래스 미존재 |
| 3 | `DeviceContext_Serialize_ContainsAllFields` | DeviceContext 클래스 미존재 |
| 4 | `DeviceContext_WithNullFields_OmitsNullValues` | DeviceContext 클래스 미존재 |
| 5 | `GameContext_Serialize_ContainsAllFields` | GameContext 클래스 미존재 |
| 6 | `GameContext_WithNullOptionalFields_OmitsNulls` | GameContext 클래스 미존재 |
| 7 | `EventContext_Serialize_ContainsLibraryDeviceGame` | EventContext 클래스 미존재 |
| 8 | `EventContext_Deserialize_ReconstructsNestedContexts` | EventContext 클래스 미존재 |
| 9 | `EventContext_WithNullDevice_OmitsDeviceField` | EventContext 클래스 미존재 |
| 10 | `EventContext_WithNullGame_OmitsGameField` | EventContext 클래스 미존재 |

### 4. Tests/EditMode/Core/EventTypeTests.cs (4 methods, 14 individual executions)
| ID | 메서드명 | 실패 이유 |
|----|---------|-----------|
| 1 | `EventType_HasExpectedValues` | EventType enum 미존재 |
| 2 | `EventType_Count_IsFive` | EventType enum 미존재 |
| 3 | `EventType_ToString_ReturnsExpectedNames` (x5) | EventType enum 미존재 |
| 4 | `EventType_ToLowerInvariant_MatchesApiFormat` (x5) | EventType enum 미존재 |

## 요약
- 총 테스트 메서드: **26개** (개별 실행: **36개**)
- 실패 이유: 프로덕션 코드(EventEnvelope, EventContext, LibraryContext, DeviceContext, GameContext, EventType) 미구현
- 모든 테스트가 올바른 이유(missing implementation)로 실패함
