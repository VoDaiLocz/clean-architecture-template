# Add Release Pipeline

## Purpose
P9.8 implements CI build validation and deploy scripts.

## Rules
1. Pipeline checks static lint rules, runs unit tests, and builds docker containers.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "ReleasePipeline" backend/src backend/tests docs/product
```
