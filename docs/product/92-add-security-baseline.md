# Add Security Baseline

## Purpose
P9.6 establishes input sanitization and secure CORS middleware configs.

## Rules
1. Request payloads are scanned and sanitized to prevent XSS/SQLi.
2. Secure HTTP headers must be present (HSTS, Content-Security-Policy).

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "SecurityBaseline" backend/src backend/tests docs/product
```
