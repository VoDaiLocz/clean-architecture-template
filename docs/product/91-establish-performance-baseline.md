# Establish Performance Baseline

## Task

P9.5 - Establish Performance Baseline

## Purpose

Define and measure baseline latency for core TOEIC learner and admin workflows before market release.

## Detailed Scope

- Define API performance budgets.
- Add benchmark or smoke measurement command.
- Seed representative data for measurement.
- Measure learner home, Today Plan, activity load, attempt submit, review queue, admin source list, and dashboard endpoints.
- Save baseline report with commit sha, environment, data volume, and p50/p95/p99.

## Out Of Scope

- Large paid load test.
- Autoscaling implementation.
- Deep query optimization unless a release blocker is found.

## Performance Budgets

Initial local/staging targets:

- Learner home p95 under 300ms.
- Today Plan p95 under 400ms.
- Activity payload p95 under 500ms for media-free content.
- Attempt submit p95 under 500ms.
- Admin source inventory p95 under 700ms for audited source volume.

Targets must be revisited after realistic staging data is available.

## Data Contract

Baseline report records:

- scenario name
- endpoint/use case
- data volume
- environment
- commit sha
- p50/p95/p99
- error count
- timestamp

## API Contract

Benchmark scenarios use stable API payloads and seeded fixtures for learner home, Today plan, item payload, attempt submit, source inventory, and admin coverage. Performance endpoints or reports must not expose secrets.

## UI Contract

Playwright performance smoke covers major learner/admin screens when routes exist. UI budgets include first meaningful route render, not only API response time.

## Business Rules

Performance baselines must use realistic seeded data, not only an empty database. Regressions above the declared budget create follow-up tasks before market release.

## Edge Cases

- Empty database.
- Audited 73-source corpus.
- Many review blockers.
- Large Part 7 passage.
- Media-heavy listening payload.
- Cold start.

## Required Tests

- Performance smoke command runs.
- Report file/check output is generated.
- Budgets are declared in docs/config.
- Build and unit tests still pass.

## Acceptance Criteria

- Baseline scenarios are documented and measurable.
- Report exists or command output is reproducible.
- Any budget miss is documented as blocker/follow-up.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "PerformanceBaseline|p95|performance budget" backend/src backend/tests docs/product
```

## Commit

`perf(p9.5): establish performance baseline`

## Push

```bash
git push origin main
```
