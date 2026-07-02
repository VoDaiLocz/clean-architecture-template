# Enforce Learner Admin Authorization

## Purpose
P9.2 secures routes and endpoints based on active role credentials.

## Rules
1. Admin paths (/api/admin/*) are protected by middleware role validation.
2. Accessing protected resources without role claims returns HTTP 403 Forbidden.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "AdminAuthorization" backend/src backend/tests docs/product
```
