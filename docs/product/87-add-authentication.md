# Add Authentication

## Purpose
P9.1 secures application access through verified identity credentials.

## Domain Model
- `AuthUser`
  - `UserId` (string, UUID)
  - `Email` (string)
  - `PasswordHash` (string)
  - `Role` (Enum: `Learner`, `Admin`)
  - `CreatedAtUtc` (DateTimeOffset)

## Repository Contract
- Interface: `IUserRepository`
  - `Task SaveUserAsync(AuthUser user, CancellationToken cancellationToken);`
  - `Task<AuthUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken);`
- Table: `auth_users`
  ```sql
  CREATE TABLE auth_users (
      user_id TEXT PRIMARY KEY,
      email TEXT NOT NULL UNIQUE,
      password_hash TEXT NOT NULL,
      role TEXT NOT NULL,
      created_at_utc TEXT NOT NULL
  );
  ```

## Application Contract
- Handler: `AuthenticateUserHandler`
- Command: `AuthenticateUserCommand`
- Response: `AuthenticateUserResponse`

## API Contract
- Endpoints:
  - `POST /api/auth/login`
  - `GET /api/auth/me`

## Rules
1. Verify credentials using secure PBKDF2 or BCrypt hashing algorithms.
2. JWT token must contain user identifier and role claim with 1h expiry.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "AuthenticateUser|auth_users" backend/src backend/tests docs/product
```
