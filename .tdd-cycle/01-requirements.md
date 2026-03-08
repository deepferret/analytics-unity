# 이벤트 데이터 모델 테스트 분석: EventEnvelope, EventContext, EventType

## 1. 요구사항 요약

| 구분 | 요구사항 | 출처 |
|------|----------|------|
| R1 | `EventEnvelope` 클래스: `eventId`, `type`, `timestamp`, `anonymousId` (필수), `userId`, `event`, `properties` (선택), `context` (필수) | Task #2, CLAUDE.md |
| R2 | `EventContext` 클래스: `LibraryContext`, `DeviceContext`, `GameContext` 서브 컨텍스트 포함 | Task #2 |
| R3 | `EventType` enum: `Identify`, `Track`, `Page`, `Screen`, `Group` | Task #2 |
| R4 | JSON 직렬화: `[JsonProperty]` camelCase, `NullValueHandling.Ignore` (선택 필드) | CLAUDE.md, Task #2 |
| R5 | `Reset()` 메서드: object pooling을 위한 모든 필드 초기화 | CLAUDE.md |
| R6 | `eventId`: UUID v7 형식 (timestamp 기반) | CLAUDE.md |
| R7 | `timestamp`: ISO 8601 UTC 형식 | CLAUDE.md, Task #2 |
| R8 | Collector API 호환: 단일 이벤트 32KB 이하, properties key 최대 200개 | CLAUDE.md |
| R9 | GC 최적화: Track() 1회 호출 < 1KB GC Alloc, object pooling (`Rent()`/`Return()`) | CLAUDE.md |

## 2. 수락 기준 (Acceptance Criteria)

### AC-1: EventEnvelope 필수 필드 존재
- **PASS**: `EventEnvelope` 인스턴스를 생성하고 필수 필드에 값을 할당한 후 JSON 직렬화 시, 모든 필수 필드가 JSON 출력에 존재한다.
- **FAIL**: 필수 필드 중 하나라도 JSON 출력에서 누락된다.

### AC-2: 선택 필드 null 시 JSON 생략
- **PASS**: `userId`, `event`, `properties`가 `null`일 때, 직렬화된 JSON에 해당 키가 포함되지 않는다.
- **FAIL**: `null`인 선택 필드가 `"userId": null` 형태로 JSON에 포함된다.

### AC-3: Timestamp ISO 8601 UTC 형식
- **PASS**: `timestamp` 값이 `yyyy-MM-ddTHH:mm:ss.fffZ` 패턴과 일치하고, UTC('Z' 접미사)로 끝난다.
- **FAIL**: 형식이 일치하지 않거나 로컬 타임존 오프셋이 포함된다.

### AC-4: EventId UUID v7 형식
- **PASS**: `eventId`가 표준 UUID 형식을 따르고, version 비트가 7이다.
- **FAIL**: UUID 형식이 아니거나 version 비트가 7이 아니다.

### AC-5: JSON 역직렬화 왕복 (Round-trip)
- **PASS**: `EventEnvelope` -> JSON -> `EventEnvelope` 역직렬화 시, 모든 필드 값이 원본과 동일하다.
- **FAIL**: 필드 값이 유실되거나 변형된다.

### AC-6: Reset() 메서드 동작
- **PASS**: `Reset()` 호출 후 모든 필드가 기본값(`null`, `default`)으로 초기화된다.
- **FAIL**: `Reset()` 후 이전 값이 남아있는 필드가 존재한다.

### AC-7: EventType enum 정의
- **PASS**: `EventType` enum에 `Identify`, `Track`, `Page`, `Screen`, `Group` 5개 값이 정의되어 있다.
- **FAIL**: 값이 누락되거나 추가 값이 존재한다.

### AC-8: EventContext 서브 컨텍스트 구조
- **PASS**: `EventContext`가 `LibraryContext`, `DeviceContext`, `GameContext`를 포함하고, 직렬화 시 camelCase 중첩 JSON으로 출력된다.
- **FAIL**: 서브 컨텍스트가 누락되거나 필드 이름이 camelCase가 아니다.

### AC-9: camelCase JSON 키 네이밍
- **PASS**: 모든 직렬화된 JSON 키가 camelCase이다.
- **FAIL**: PascalCase 또는 snake_case 키가 존재한다.

## 3. 엣지 케이스 식별

### 3.1 Null / Empty 경계

| ID | 엣지 케이스 | 예상 동작 |
|----|-------------|-----------|
| E1 | `properties`가 빈 Dictionary `{}` | JSON에 `"properties": {}` 포함 |
| E2 | `properties` key가 빈 문자열 `""` | 직렬화 허용 |
| E3 | `properties` value가 `null` | `{"key": null}` 포함 |
| E4 | `context`가 `null` | 필수 필드이므로 직렬화 시 문제 가능 |
| E5 | `anonymousId`가 빈 문자열 `""` | 직렬화는 되지만 상위 validation에서 거부 |
| E6 | `userId`가 빈 문자열 `""` vs `null` | 빈 문자열은 JSON에 포함 |

### 3.2 경계값

| ID | 엣지 케이스 | 예상 동작 |
|----|-------------|-----------|
| E7 | `properties` key 수 = 200 (최대) | 직렬화 성공 |
| E8 | `properties` key 수 = 201 (초과) | 데이터 모델에서는 허용, validation은 상위 레이어 |
| E9 | 직렬화된 단일 이벤트 크기 32KB 초과 | 데이터 모델에서는 허용, Transport에서 거부 |
| E10 | `properties` value에 중첩 Dictionary | Newtonsoft.Json 정상 처리 |
| E11 | `properties` value에 다양한 타입 (int, float, bool, string, array, nested) | 모두 정상 직렬화 |

### 3.3 직렬화 특수 상황

| ID | 엣지 케이스 | 예상 동작 |
|----|-------------|-----------|
| E12 | Unicode 문자열 (한글, 이모지) | UTF-8 정상 직렬화 |
| E13 | 특수 문자 (따옴표, 백슬래시) | JSON escape 처리 |
| E14 | 매우 긴 문자열 value (10,000자+) | 직렬화 성공 |
| E15 | `EventType` enum 소문자 문자열 변환 | `"track"`, `"identify"` 등 |

### 3.4 Object Pooling (Reset)

| ID | 엣지 케이스 | 예상 동작 |
|----|-------------|-----------|
| E16 | `Reset()` 후 `Properties` Dictionary 참조 | `null`로 초기화 |
| E17 | `Reset()` 후 `Context` 내부 서브 컨텍스트 | `Context` 자체가 `null` |
| E18 | `Reset()` 후 재사용 -> 새 값 할당 -> 직렬화 | 이전 값 잔존 없이 정상 동작 |

## 4. 테스트 시나리오 매트릭스

### 4.1 EventEnvelope 테스트 (U01~U17)

| Test ID | 테스트 시나리오 | 매핑 | 카테고리 |
|---------|----------------|------|----------|
| U01 | 필수 필드 직렬화 -> 키 존재 확인 | AC-1 | Serialization |
| U02 | `userId = null` -> JSON에 키 없음 | AC-2 | Serialization |
| U03 | `event = null` -> JSON에 키 없음 | AC-2 | Serialization |
| U04 | `properties = null` -> JSON에 키 없음 | AC-2 | Serialization |
| U05 | 모든 선택 필드 값 할당 -> 모든 키 존재 | AC-1,2 | Serialization |
| U06 | JSON -> EventEnvelope 역직렬화 | AC-5 | Deserialization |
| U07 | EventEnvelope -> JSON -> EventEnvelope 왕복 | AC-5 | Round-trip |
| U08 | timestamp ISO 8601 UTC 형식 검증 | AC-3 | Format |
| U09 | eventId UUID v7 형식 검증 | AC-4 | Format |
| U10 | 모든 JSON 키 camelCase 확인 | AC-9 | Serialization |
| U11 | Reset() 후 모든 필드 null/default | AC-6 | Pooling |
| U12 | Reset() 후 재사용 직렬화 | AC-6 | Pooling |
| U13 | 빈 Dictionary 직렬화 | E1 | Edge Case |
| U14 | 다양한 property value 타입 | E11 | Serialization |
| U15 | Unicode 문자열 properties | E12 | Serialization |
| U16 | 특수 문자 properties | E13 | Serialization |
| U17 | 빈 문자열 key 허용 | E2 | Edge Case |

### 4.2 EventContext 테스트 (U18~U23)

| Test ID | 테스트 시나리오 | 매핑 | 카테고리 |
|---------|----------------|------|----------|
| U18 | LibraryContext camelCase 직렬화 | AC-8,9 | Serialization |
| U19 | DeviceContext 필드 존재 확인 | AC-8 | Serialization |
| U20 | GameContext 필드 존재 확인 | AC-8 | Serialization |
| U21 | EventContext 전체 중첩 구조 확인 | AC-8 | Serialization |
| U22 | 선택 필드 null 시 JSON 생략 | AC-2 | Serialization |
| U23 | EventContext 역직렬화 왕복 | AC-5 | Round-trip |

### 4.3 EventType 테스트 (U24~U27)

| Test ID | 테스트 시나리오 | 매핑 | 카테고리 |
|---------|----------------|------|----------|
| U24 | enum 5개 값 존재 확인 | AC-7 | Definition |
| U25 | EventType -> 소문자 문자열 변환 | E15 | Serialization |
| U26 | 소문자 문자열 -> EventType 파싱 | E15 | Deserialization |
| U27 | EventEnvelope.Type에 EventType 문자열 일관성 | R1,R3 | Integration |

### 4.4 Batch Payload 테스트 (U28~U30)

| Test ID | 테스트 시나리오 | 매핑 | 카테고리 |
|---------|----------------|------|----------|
| U28 | Batch JSON 구조 확인 | R8 | Serialization |
| U29 | sentAt ISO 8601 UTC 형식 | R7 | Format |
| U30 | 빈 batch 배열 직렬화 | Edge | Serialization |

### 테스트 요약: 총 30개 EditMode 단위 테스트

## 5. 외부 의존성 및 Mocking 전략

| 의존성 | Mocking 필요 여부 |
|--------|-------------------|
| `Newtonsoft.Json` | **불필요** -- 실제 라이브러리 사용 |
| `System.Guid` / UUID v7 | **불필요** -- 형식 검증만 |
| `System.DateTime` | **불필요** -- 고정 문자열 사용 |
| Unity `Application` API | **해당 없음** -- 이 태스크 범위 외 |

모든 테스트는 순수 C# POCO 직렬화 검증이므로 Mock 프레임워크 불필요.

## 6. 구현 시 주의사항

- UUID v7: .NET Standard 2.1에 기본 지원 없음. 별도 유틸리티 필요.
- EventType: CLAUDE.md에서 `Type`이 `string`으로 정의. enum은 내부 상수/확장 메서드로 변환.
- Reset(): `Properties` Dictionary는 `null`로 설정 (Clear 아님). GC 회수 허용.
