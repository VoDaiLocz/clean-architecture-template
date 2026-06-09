# TOEIC Normalization System

Production-style C# / TypeScript system for converting TOEIC source materials into validated database content.

## Structure

- `backend/src/Domain`: TOEIC learning item models and validation rules.
- `backend/src/Application`: use cases and repository contracts.
- `backend/src/Infrastructure`: SQLite persistence and publish gate.
- `backend/src/Api`: Minimal API endpoints.
- `backend/tests/Application.UnitTest`: executable test harness.
- `frontend`: TypeScript/Vite operational dashboard.

## Verify

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/src/Api/Api.csproj
cd frontend && npm run build
```

## Run Locally

```bash
dotnet run --project backend/src/Api/Api.csproj --urls http://localhost:5080
cd frontend && npm run dev -- --port 5173
```

Then open `http://localhost:5173`.
