# Add Authentication

## Task

P9.1 - Add Authentication

## Purpose

Establish the first production identity slice for learners and internal users. This task must be small enough for one real commit and safe enough to protect learner-owned state before public release.

## Detailed Scope

- Add `AuthUser` and `AuthRefreshToken` domain/data models.
- Add migration/schema for `auth_users` and `auth_refresh_tokens`.
- Add password hash/verify service using the platform-approved .NET password hasher.
- Add access token issuer and validator.
- Add refresh token rotation.
- Add register, login, refresh, logout, and current-user use cases.
- Add typed API contracts and endpoints:
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `POST /api/auth/refresh`
  - `POST /api/auth/logout`
  - `GET /api/auth/me`
- Add auth configuration validation.
- Add application tests and API contract tests.

## Out Of Scope

- Password reset email.
- Email verification workflow.
- MFA.
- OAuth/social login.
- SSO.
- User management UI.
- Admin role invitation workflow.
- Subscription/payment identity.

Those are future tasks, not acceptance criteria for P9.1.

## Data Contract

### `auth_users`

Required fields:

- `user_id`
- `email_normalized`
- `password_hash`
- `display_name`
- `role`
- `status`
- `failed_login_attempts`
- `locked_until_utc`
- `created_at_utc`
- `updated_at_utc`

Rules:

- Email is unique case-insensitively.
- Password hash is never reversible.
- `role` must be one of `Learner`, `Operator`, `Admin`, `SuperAdmin`.
- `status` must be one of `Active`, `Locked`, `Disabled`.

### `auth_refresh_tokens`

Required fields:

- `refresh_token_id`
- `user_id`
- `token_hash`
- `expires_at_utc`
- `revoked_at_utc`
- `replaced_by_token_id`
- `created_at_utc`

Rules:

- Raw refresh token is returned once and never stored.
- Reusing a revoked refresh token revokes all active refresh tokens for the user.

## API Contract

### `POST /api/auth/register`

Request:

- email
- password
- displayName

Response:

- user id
- email
- display name
- role

Errors:

- `AUTH_EMAIL_ALREADY_EXISTS`
- `AUTH_INVALID_EMAIL`
- `AUTH_WEAK_PASSWORD`

### `POST /api/auth/login`

Request:

- email
- password

Response:

- access token
- refresh token
- expires at
- user summary

Errors:

- `AUTH_INVALID_CREDENTIALS`
- `AUTH_USER_LOCKED`
- `AUTH_USER_DISABLED`

### `POST /api/auth/refresh`

Request:

- refresh token

Response:

- new access token
- new refresh token
- expires at

Errors:

- `AUTH_REFRESH_TOKEN_INVALID`
- `AUTH_REFRESH_TOKEN_EXPIRED`
- `AUTH_REFRESH_TOKEN_REUSED`

### `POST /api/auth/logout`

Request:

- refresh token

Response:

- `204 No Content`

### `GET /api/auth/me`

Requires valid access token.

Response:

- user id
- email
- display name
- role

## UI Contract

Login/register UI consumes auth APIs and stores only client-safe session state. UI must not store raw refresh tokens outside the approved secure client strategy and must not reveal whether an email exists on login failure.

## Business Rules

1. Passwords are hashed using a framework-approved password hasher; do not store a separate salt unless the hasher contract requires it.
2. Access token expiry is configurable and short-lived.
3. Refresh tokens rotate on every refresh.
4. Logout revokes the submitted refresh token.
5. Disabled users cannot login or refresh.
6. Registering an admin/operator account is not allowed through public learner registration.
7. Auth errors must not reveal whether an email exists during login.

## Edge Cases

- Duplicate email with different case.
- Wrong password.
- Disabled user.
- Locked user.
- Expired refresh token.
- Reused refresh token.
- Concurrent refresh requests.
- Missing auth signing configuration.

## Required Tests

- Password hash and verify.
- Register learner user.
- Duplicate email is rejected.
- Login returns access and refresh token.
- Invalid credentials use safe error.
- Refresh rotates token and revokes old token.
- Reused refresh token is rejected.
- Logout revokes token.
- `/api/auth/me` returns current user for valid token.
- Missing production auth config fails startup/config validation.

## Acceptance Criteria

- Auth data persists in normalized tables.
- Register/login/refresh/logout/me use cases exist.
- Refresh token rotation is enforced.
- Token validation rejects invalid/expired tokens.
- Tests and build pass.
- No password/token/secret is logged or committed.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "AuthenticateUser|auth_users|RegisterUser|LoginUser|RefreshToken" backend/src backend/tests docs/product
```

## Commit

`feat(p9.1): add authentication`

## Push

```bash
git push origin main
```
