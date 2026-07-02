# Remove Frontend Demo Learner Content

## Purpose
P7.1 deletes frontend mock sessions and static fallback files.

## Rules
1. Build fails if demo learner memory state references exist in learner views.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "DemoLearner" backend/src backend/tests docs/product
```
