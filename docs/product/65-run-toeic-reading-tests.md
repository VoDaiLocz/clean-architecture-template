# Run TOEIC Reading Tests

## Purpose
P6.4 implements combined reading section test sessions (Parts 5-7).

## Rules
1. 100 questions total, passages visible alongside forms.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
rg -n "ReadingTest" backend/src backend/tests docs/product
```
