# Phase 5: 통합 테스트

## 생성된 파일
- `Tests/EditMode/Core/EventIntegrationTests.cs` — 11개 테스트

## 테스트 목록

| ID | 메서드명 | 카테고리 |
|----|---------|----------|
| 1 | `FullEnvelope_RoundTrip_PreservesAllFields` | Round-trip |
| 2 | `MinimalEnvelope_RoundTrip_PreservesRequiredFields` | Round-trip |
| 3 | `BatchPayload_Serialize_ProducesCorrectStructure` | Batch |
| 4 | `BatchPayload_SentAt_IsIso8601Utc` | Batch |
| 5 | `BatchPayload_EmptyBatch_SerializesCorrectly` | Batch |
| 6 | `Properties_WithUnicode_SerializesCorrectly` | Edge Case |
| 7 | `Properties_WithSpecialCharacters_SerializesCorrectly` | Edge Case |
| 8 | `Properties_WithVariousTypes_SerializesCorrectly` | Edge Case |
| 9 | `Reset_ThenReuse_ProducesCleanEnvelope` | Pooling |
| 10 | `AllEventTypes_CanBeUsedInEnvelope` | Integration |
