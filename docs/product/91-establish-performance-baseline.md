# Establish Performance Baseline

## Purpose
P9.5 defines and measures API response time limits.

## Rules
1. Core learning paths target p95 response time under 200ms.
2. Performance profiles are validated against BenchmarkDotNet metrics.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "PerformanceBaseline" backend/src backend/tests docs/product
```
