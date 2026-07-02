# Tag TOEIC Learner Weaknesses

## Purpose
P5.9 converts attempt outcomes into stable weakness tags.

## Rules
1. Incorrect responses propagate original question's skill tags to learner profile.
2. Aggregate analytics count tag error weight over time.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "TagLearnerWeakness" backend/src backend/tests docs/product
```
