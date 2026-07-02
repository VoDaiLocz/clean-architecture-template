# Enforce Learner Admin Authorization

## Task

P9.2 - Enforce Learner Admin Authorization

## Purpose

Enforce role and ownership rules so learners access only their own learning state, operators can run content operations, and admins control publication/system operations.

## Detailed Scope

- Add authenticated actor/current user abstraction for application use cases.
- Add role policy model for `Learner`, `Operator`, `Admin`, `SuperAdmin`.
- Add route-level authorization metadata.
- Add application-level ownership checks for learner resources.
- Add admin/operator policy checks for content operation use cases.
- Add authorization error contract.
- Add audit record for denied privileged operations.
- Add tests for positive and negative access paths.

## Out Of Scope

- MFA.
- SSO/IAM integration.
- Fine-grained permission editor UI.
- Row-level database security.
- Paid subscription entitlements.

## Data Contract

Authorization reads authenticated user, role, learner ownership, and audit records. Denied privileged actions write an audit event with actor id, resource, action, outcome, reason, and timestamp.

## Role Matrix

| Capability | Learner | Operator | Admin | SuperAdmin |
| --- | --- | --- | --- | --- |
| Own learner profile/home/path | Yes | No | Support read only | Support read only |
| Own activities/attempts/review/tests | Yes | No | Support read only | Support read only |
| Source inventory and assets | No | Yes | Yes | Yes |
| Extraction jobs | No | Yes | Yes | Yes |
| Draft review approve/reject | No | Yes | Yes | Yes |
| Publish/unpublish learner content | No | No | Yes | Yes |
| Manage users/roles | No | No | No | Yes |
| View audit logs | No | No | Yes | Yes |
| Health live endpoint | Public | Public | Public | Public |
| Readiness/metrics endpoint | No | Yes | Yes | Yes |

## API Contract

Protected endpoints return:

- `401 Unauthorized` when token is missing, invalid, or expired.
- `403 Forbidden` when token is valid but role/ownership is insufficient.

Error body follows P9.4:

```json
{
  "error": {
    "code": "FORBIDDEN_RESOURCE",
    "message": "You do not have access to this resource.",
    "correlationId": "request-correlation-id",
    "timestamp": "2026-07-02T00:00:00Z"
  }
}
```

## UI Contract

Learner UI hides admin routes, but backend authorization remains source of truth. Admin UI can hide disabled actions but must still handle `401` and `403` responses explicitly.

## Business Rules

1. Default deny for every production endpoint except documented public health/login/register routes.
2. Learner APIs must derive learner id from authenticated actor or verify request learner id matches actor.
3. Application use cases enforce resource ownership; API layer only extracts actor and applies coarse route policy.
4. Learner APIs never expose source inventory, drafts, validation issues, or admin evidence.
5. Operator can approve/reject drafts but cannot publish/unpublish.
6. Admin can publish/unpublish but cannot bypass validation rules.
7. Denied admin/operator operations create audit records.

## Edge Cases

- Learner requests another learner id.
- Learner calls admin route.
- Operator calls publish route.
- Admin calls learner-owned route without support context.
- Missing role claim.
- Deactivated user token.
- Public health endpoint without auth.

## Required Tests

- Learner can access own home/profile.
- Learner cannot access another learner's home/profile.
- Learner cannot access admin source inventory.
- Operator can access draft review but cannot publish.
- Admin can publish when content rules pass.
- Missing/expired token returns `401`.
- Valid token with wrong role returns `403`.
- Denied privileged operation creates audit record.
- Route catalog has explicit auth policy for non-public endpoints.

## Acceptance Criteria

- Every production endpoint has explicit public/auth/role policy.
- Ownership is enforced in application layer.
- Admin/source/draft data cannot leak to learner APIs.
- Stable `401`/`403` error contract exists.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "Authorize|AuthenticatedActor|LearnerAuthorization|AdminAuthorization" backend/src backend/tests docs/product
```

## Commit

`feat(p9.2): enforce learner admin authorization`

## Push

```bash
git push origin main
```
