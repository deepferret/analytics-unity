# Phase 3 (GREEN): 최소 구현

## 생성된 프로덕션 코드 파일

### 1. Runtime/Core/EventType.cs
- `EventType` enum: `Identify`, `Track`, `Page`, `Screen`, `Group`
- 최소 구현 — enum 정의만

### 2. Runtime/Core/EventContext.cs
- `EventContext` 클래스: `Library`, `Device`, `Game` 필드
- `LibraryContext` 클래스: `Name`, `Version` 필드
- `DeviceContext` 클래스: `Type`, `Model`, `Gpu`, `Os` 필드
- `GameContext` 클래스: `Engine`, `EngineVersion`, `AppVersion`, `Platform`, `Scene`, `Store` 필드
- 모든 필드에 `[JsonProperty]` camelCase 속성 적용
- 선택 필드에 `NullValueHandling.Ignore` 적용

### 3. Runtime/Core/EventEnvelope.cs
- 필수 필드: `EventId`, `Type`, `Timestamp`, `AnonymousId`, `Context`
- 선택 필드 (NullValueHandling.Ignore): `UserId`, `Event`, `Properties`
- `Reset()` 메서드: 모든 필드를 `null`로 초기화

## 설계 결정 사항
- `EventEnvelope.Type`은 `string` 타입 (CLAUDE.md 설계와 일치) — `EventType` enum은 상수/확장 메서드로 변환
- `Reset()`은 `Properties`를 `null`로 설정 (`Clear()` 아님) — GC 회수 허용
- `[Serializable]` 어트리뷰트 적용 — Unity 직렬화 호환
- 모든 클래스를 `Runtime/Core/` 디렉토리에 배치

## 기술적 부채 (Refactor phase에서 해결)
- XML 문서 주석 보강 가능
- UUID v7 생성 유틸리티는 별도 태스크(EventBuilder)에서 구현
