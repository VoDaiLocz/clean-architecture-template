# Learner Onboarding

## Purpose

P4.1 creates or updates the durable learner profile and returns the first backend-owned action in the TOEIC journey.

The learner app must not infer the next step from local UI state. It must use the response from this use case.

## Application Contract

Handler:

- `OnboardLearnerHandler`

Command:

- `OnboardLearnerCommand`

Response:

- `OnboardLearnerResponse`
- `LearnerNextAction`

## API Contract

Endpoint:

- `POST /api/learner/onboarding`

Request fields:

- `learnerId`
- `displayName`
- `email`
- `targetScore`
- `currentEstimatedScore`
- `dailyStudyMinutes`
- `timeZoneId`

Response fields:

- persisted learner id
- persisted TOEIC target score
- persisted current estimated score
- persisted daily study minutes
- persisted timezone
- next action

## Rules

1. Onboarding creates the learner profile when none exists.
2. Repeating onboarding updates the same learner profile.
3. The profile is active after onboarding.
4. Target score, current estimate, daily study minutes, identity, email, and timezone validation remain backend-owned.
5. The next action after onboarding is `StartPlacement`.
6. The next action points to `/api/learner/placement/start`.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "OnboardLearner|/api/learner/onboarding|StartPlacement" backend/src backend/tests docs/product
```
