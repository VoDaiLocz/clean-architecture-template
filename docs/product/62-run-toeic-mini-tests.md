# Run TOEIC Mini Tests

## Purpose
P6.1 implements runtime execution of mini tests.

## Domain Model
- `MiniTestSession`
  - `SessionId` (string, UUID)
  - `TestId` (string, UUID)
  - `LearnerId` (string, UUID)
  - `Status` (Enum: `Active`, `Completed`)

## API Contract
- Endpoint: `POST /api/learner/mini-test/start`

## Rules
1. Mini test completion status directly feeds unit mastery triggers.
2. Session expiration completes the session automatically.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "MiniTestSession" backend/src backend/tests docs/product
```
