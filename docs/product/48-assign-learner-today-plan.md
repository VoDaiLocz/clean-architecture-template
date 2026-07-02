# Assign Learner Today Plan

## Task

P4.6 - Assign Learner Today Plan

## Purpose

Return the learner's next best action for the current day using backend priority rules: repair blockers first, then resume work, then next unit activity.

## Detailed Scope

- Add `GetLearnerTodayPlanHandler`.
- Compute current assignment from review blockers, active sessions, and path progress.
- Return progress summary and blocker reasons.
- Create assignment records when the next activity is selected.
- Keep assignment selection deterministic and repository-backed.

## Out Of Scope

- Calendar scheduling.
- Push notifications.
- Frontend dashboard implementation.
- Manual teacher assignment overrides.

## Data Contract

Tables: `learner_assignments`, `assignment_blockers`, `daily_plan_snapshots`.
Assignment fields: `assignment_id`, `learner_id`, `unit_id`, `activity_type`, `priority`, `status`, `due_date`, `created_at_utc`.

## API Contract

`GET /api/learner/today?learnerId=...` returns primary assignment, blockers, path progress, review count, and next allowed actions.
Errors: `LEARNER_PROFILE_REQUIRED`, `ACTIVE_PATH_REQUIRED`.

## UI Contract

UI renders the returned primary assignment as the main action and shows blockers. UI must not choose or override assignment priority.

## Business Rules

1. Blocking review items outrank new lessons.
2. In-progress assignments outrank newly created assignments.
3. Activity order inside a unit is Lesson -> GuidedExample -> Drill -> MiniTest.
4. Locked units cannot produce new work.
5. Empty content returns `ContentUnavailable`, not fake data.

## Edge Cases

- No profile.
- No active path.
- Open blocking review.
- In-progress assignment.
- Completed unit.
- Unit content unavailable.
- Multiple eligible next units.

## Required Tests

- Review blocker priority.
- Resume existing assignment.
- Create next assignment deterministically.
- Empty path/content unavailable result.
- Progress numbers from persisted data.

## Acceptance Criteria

- Today plan endpoint never returns demo content.
- Priority rules are tested and documented.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "GetLearnerTodayPlan|learner_assignments|assignment_blockers|ContentUnavailable" backend/src backend/tests docs/product
```

## Commit

`feat(p4.6): assign learner today plan`

## Push

`git push origin main`
