# Background Job Foundation

## Purpose

The content factory needs reliable asynchronous processing for Drive discovery, PDF extraction, audio metadata extraction, transcript parsing, validation, and publishing workflows.

P1.5 creates the application job queue contract and local in-memory implementation. It does not create a production worker host yet.

## Application Contract

Application-owned interface:

```text
Application.Common.Interfaces.Jobs.IBackgroundJobQueue
```

Operations:

- `Enqueue`
- `TryLeaseNext`
- `RecordSuccess`
- `RecordFailure`
- `Get`

Model:

- `BackgroundJob`
- `BackgroundJobLease`
- `BackgroundJobStatus`
- `EnqueueBackgroundJobRequest`

Statuses:

- `Queued`
- `Running`
- `Succeeded`
- `Failed`

## Infrastructure Implementation

Local/test implementation:

```text
Infrastructure.Jobs.InMemoryBackgroundJobQueue
```

Retry policy:

```text
Infrastructure.Jobs.BackgroundJobRetryPolicy
```

Default local registration:

```text
maxAttempts: 3
```

## Business Rules

1. A queued job can be leased for execution.
2. Leasing increments attempt count and marks the job running.
3. A failed job below retry limit returns to queued state.
4. A failed job at retry limit becomes failed and records failure reason.
5. Succeeded and failed jobs are not leased again.
6. Job type and payload reference are required.

## Out Of Scope

- hosted worker service
- persistent job table
- distributed locks
- delayed scheduling
- dead-letter queue
- dashboard UI
- external queue provider

These are added in later P1/P3/P8 tasks.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "IBackgroundJobQueue|InMemoryBackgroundJobQueue|Background Job Foundation" backend/src backend/tests docs/product
```
