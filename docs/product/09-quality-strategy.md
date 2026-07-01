# Quality Strategy

## Test Layers

### Domain Tests

Required for:

- placement scoring
- mastery policy
- unlock policy
- review creation
- part-specific validation

### Application Tests

Required for:

- onboarding use cases
- placement flow
- Today Plan assignment
- attempt submission
- content publishing

### Repository Tests

Required for:

- schema integrity
- persistence
- idempotent imports
- required foreign keys

### API Contract Tests

Required for:

- learner endpoints
- admin endpoints
- error codes
- auth/authorization

### End-To-End Tests

Required for:

- new learner onboarding
- placement completion
- Today Plan start
- activity attempt
- wrong answer review
- unit unlock
- practice test result

## Quality Gates

A task cannot pass if:

- tests are missing
- frontend contains production fake content
- learner endpoint exposes source/admin data
- API response is not stable
- build fails
- commit is not pushed

## Required Verification Commands

Backend task:

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
```

Frontend task:

```bash
cd frontend
npm run build
npm run test:e2e
```

Task-specific commands can add to this list but cannot remove required checks.

