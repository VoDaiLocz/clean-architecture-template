# Persisted Learner Home

## Purpose

P4.2 makes learner home state repository-backed instead of memory-only.

The home endpoint can still coexist with legacy demo activity endpoints, but the production home decision must come from persisted learner state.

## Application Contract

Handler:

- `GetLearnerHomeHandler`

Query:

- `GetLearnerHomeQuery`

Response:

- `LearnerHomeResponse`

## API Contract

Endpoint:

- `GET /api/learner/home?learnerId={learnerId}`

Rules:

1. Missing learner profile returns onboarding as the next activity.
2. Existing learner profile returns placement as the next activity until placement sessions are implemented.
3. The learner id in the response comes from persisted profile state when the profile exists.
4. Review count is read from persisted review items.
5. The home endpoint must not depend on `DemoLearnerSession`.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "GetLearnerHome|toeic-placement-start|persisted learner home" backend/src backend/tests docs/product
```
