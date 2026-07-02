# Add Production Deployment Config

## Purpose
P9.9 defines connection strings and runtime credentials bindings.

## Rules
1. Runtime configs map keys from secure environment variables/secrets manager.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "DeploymentConfig" backend/src backend/tests docs/product
```
