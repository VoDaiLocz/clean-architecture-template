# Standardize Error Handling

## Purpose
P9.4 implements unified global exception handlers.

## Rules
1. Exceptions are mapped by global middleware into standard JSON error objects.
2. Error response payload must hide raw stack trace details in production.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "GlobalException" backend/src backend/tests docs/product
```
