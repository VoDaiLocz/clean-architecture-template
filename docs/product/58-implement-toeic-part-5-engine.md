# Implement TOEIC Part 5 Engine

## Purpose
P5.6 implements sentence completion behaviors for TOEIC Part 5.

## Rules
1. Prompt must include exactly 1 blank anchor character sequence.
2. Exactly 4 choices are supported.
3. Skill tag classification (grammar vs vocabulary) is required.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "Part5Engine" backend/src backend/tests docs/product
```
