# Add Production Observability

## Purpose
P9.3 sets up structured logs and metrics export capability.

## Rules
1. JSON structured console logging is active on production endpoints.
2. Correlation-ID headers must propagate trace contexts across layers.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "ProductionObservability" backend/src backend/tests docs/product
```
