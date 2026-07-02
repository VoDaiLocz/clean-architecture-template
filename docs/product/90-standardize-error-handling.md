# Standardize Error Handling

## Task

P9.4 - Standardize Error Handling

## Purpose

Provide one stable API error contract for learner, admin, auth, content, and platform endpoints.

## Detailed Scope

- Add global exception middleware.
- Add error code taxonomy.
- Add validation error mapping.
- Add auth/authorization error mapping.
- Add dependency failure mapping.
- Add correlation id to every error response.
- Hide raw exception details in production.

## Out Of Scope

- UI toast design.
- Full localization.
- Incident management workflow.

## API Contract

All errors use:

```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "The request is invalid.",
    "correlationId": "request-correlation-id",
    "timestamp": "2026-07-02T00:00:00Z",
    "details": []
  }
}
```

`details` is allowed only for safe validation fields.

## Business Rules

1. Production errors never include stack traces.
2. Domain rule failures map to stable codes.
3. Validation failures return `400` or `422` based on route convention.
4. Auth failures return `401`.
5. Authorization failures return `403`.
6. Conflicts/idempotency conflicts return `409`.
7. Unexpected exceptions return `500` with generic message and correlation id.

## Edge Cases

- Unhandled exception.
- Validation error with multiple fields.
- Domain invariant failure.
- Unauthorized request.
- Forbidden request.
- Not found.
- Conflict.
- Dependency unavailable.

## Required Tests

- Middleware maps unhandled exception to safe `500`.
- Production mode hides stack trace.
- Validation errors include safe field details.
- Auth and forbidden errors use stable codes.
- Correlation id appears in every error.

## Acceptance Criteria

- Shared error contract exists.
- All major error categories are mapped.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "GlobalException|ErrorCode|correlationId" backend/src backend/tests docs/product
```

## Commit

`feat(p9.4): standardize error handling`

## Push

```bash
git push origin main
```
