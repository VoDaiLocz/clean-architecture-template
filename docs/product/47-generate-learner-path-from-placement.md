# Generate Learner Path From Placement

## Task

P4.5 - Generate Learner Path From Placement

## Purpose

Generate the learner's first backend-owned learning path from placement weaknesses so every user starts with a controlled route instead of arbitrary part clicking.

## Detailed Scope

- Add `GenerateLearningPathHandler`.
- Create active `LearningPath` and ordered `LearningPathUnit` rows.
- Build units from a versioned TOEIC unit catalog.
- Prioritize high/medium placement weaknesses.
- Unlock only the first eligible unit.
- Archive the previous active path when regenerating after a new diagnostic.

## Out Of Scope

- AI-generated custom lesson content.
- Manual path editing UI.
- Paid coaching plans.
- Practice-test repair plans from P6.8.

## Data Contract

Tables: `learning_paths`, `learning_path_units`, `learner_path_generation_runs`.
Each unit stores `unit_id`, `path_id`, `unit_key`, `toeic_part`, `skill_tags`, `display_order`, `status`, `unlock_reason`, `source_result_id`.

## API Contract

`POST /api/learner/path/generate` accepts `{ learnerId, placementResultId }`.
Success returns path id, first unlocked unit, total units, generated reason summary, and next action.
Errors: `PLACEMENT_RESULT_REQUIRED`, `PLACEMENT_RESULT_NOT_OWNED`, `PATH_ALREADY_ACTIVE`, `UNIT_CATALOG_EMPTY`.

## UI Contract

UI shows the returned first unit/today action. UI must not reorder, unlock, or synthesize learning units.

## Business Rules

1. A completed placement result is required.
2. High weakness units appear before medium/low units unless prerequisites force earlier foundation units.
3. Exactly one active path exists per learner.
4. Regeneration archives the old active path with a reason.
5. Unit catalog version is persisted for auditability.

## Edge Cases

- No placement result.
- Placement result belongs to another learner.
- Empty catalog.
- Multiple weaknesses mapping to same unit.
- New placement after old path exists.
- Missing prerequisite unit.

## Required Tests

- Requires placement result.
- Generates deterministic order from fixture weaknesses.
- Deduplicates units.
- Archives old active path on regeneration.
- Unlocks first eligible unit only.
- Persists catalog version.

## Acceptance Criteria

- Learner path is generated from backend diagnostic data.
- Unit order and unlock state are reproducible.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "GenerateLearningPath|learning_paths|learning_path_units|UNIT_CATALOG" backend/src backend/tests docs/product
```

## Commit

`feat(p4.5): generate learner path from placement`

## Push

`git push origin main`
