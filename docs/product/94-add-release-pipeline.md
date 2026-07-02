# Add Release Pipeline

## Task

P9.8 - Add Release Pipeline

## Purpose

Add CI/CD quality gates so broken or unsafe changes cannot be shipped accidentally.

## Detailed Scope

- Add/verify backend restore/build/test gate.
- Add/verify frontend install/build/test gate.
- Add Playwright gate for core production flows when frontend exists.
- Add secret scan gate.
- Add artifact/build output step.
- Add explicit deployment smoke gate stub if deployment target is not ready.

## Out Of Scope

- Cloud provider provisioning.
- Blue/green deployment automation.
- Paid CI optimization.

## Data Contract

Release evidence records include commit sha, workflow run id, environment, artifact version, test status, migration status, deployment status, and approver where applicable.

## API Contract

No product API is required. Deployment health checks must call the documented health endpoints and fail the pipeline on unhealthy readiness.

## UI Contract

No learner UI changes. Build artifacts must be generated through the same frontend build used for production deployment.

## Business Rules

1. Main branch release requires green pipeline.
2. Failing tests/build/security scan block release.
3. Workflow commands must match local verification commands where possible.
4. Secrets must be supplied by CI secret store, not files.

## Edge Cases

- Missing CI secret.
- Flaky E2E.
- Dependency cache corruption.
- Test DB unavailable.
- Frontend/backend contract drift.

## Required Tests

- Workflow file exists.
- Backend test command runs.
- Backend build command runs.
- Frontend build command runs when frontend exists.
- Secret scan command/check exists.

## Acceptance Criteria

- Release pipeline has explicit gates.
- Docs explain failure blocks.
- Local verification mirrors CI commands.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
npm --prefix frontend run build
rg -n "ReleasePipeline|dotnet build|playwright|secret" .github docs/product
```

## Commit

`ci(p9.8): add release pipeline`

## Push

```bash
git push origin main
```
