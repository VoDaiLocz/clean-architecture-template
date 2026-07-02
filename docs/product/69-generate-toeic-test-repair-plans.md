# Generate TOEIC Test Repair Plans

## Purpose
P6.8 calculates targeted repair tasks after exam completes.

## Rules
1. Weak parts trigger repair plans that block new learning units until completed.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "GenerateRepairPlan" backend/src backend/tests docs/product
```
