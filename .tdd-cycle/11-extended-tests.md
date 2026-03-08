# Phase 5: 확장 테스트

통합 테스트 파일(EventIntegrationTests.cs)에 다음 확장 테스트가 포함됩니다:

## 스트레스/경계 테스트
- `Properties_WithVariousTypes_SerializesCorrectly` — int, float, bool, string, null, long 등 다양한 타입
- `Properties_WithUnicode_SerializesCorrectly` — 한국어, 이모지, 일본어
- `Properties_WithSpecialCharacters_SerializesCorrectly` — 따옴표, 백슬래시, 개행, 탭

## 에러 복구 테스트
- `Reset_ThenReuse_ProducesCleanEnvelope` — Reset 후 재사용 시 이전 데이터 잔존 없음

## 통합 검증
- `AllEventTypes_CanBeUsedInEnvelope` — 모든 EventType enum 값이 EventEnvelope.Type에서 정상 동작

## 전체 테스트 수 업데이트
- 기존: 26개 메서드 (36개 개별 실행)
- 추가: 11개 메서드 (통합 + 확장)  → **수정: 10개 (BatchPayload 3 + Edge 3 + Pooling 1 + Integration 1 + Round-trip 2)**
- **총계: 36개 메서드**
