# Build Content Coverage Dashboard

## Task

P8.7 - Build Content Coverage Dashboard

## Purpose

Build coverage analytics by TOEIC part, media requirement, source status, extraction status, and publish readiness.

## Detailed Scope

- Build admin/operator UI against real admin APIs.
- Include filters, search, pagination or virtualized list where data can grow.
- Show loading/error/empty states.
- Add operator-safe actions with confirmation where mutations exist.
- Add Playwright/admin route smoke tests.

## Out Of Scope

- Learner-facing content presentation.
- Raw Google auth automation inside browser UI.
- Publishing content without validation.
- Manual DB editing from UI.

## Data Contract

Admin UI reads normalized source/content operations tables. It never treats the Google Sheet as learner content; the manifest remains source inventory. Audit actions include actor, timestamp, resource id, outcome, and reason.

## API Contract

Consumes admin APIs under `/api/admin/...` with explicit authorization from P9.2. Mutation endpoints must return stable success/error contracts and audit ids.

## UI Contract

Admin UI is dense, operational, and task-focused: sortable tables, filters, status badges, clear failed-state reasons, and no learner-marketing copy.

## Business Rules

1. Operators can see what is blocked, extractable, reviewable, publishable, and missing.
2. Destructive or publish actions require confirmation and backend validation.
3. Learner APIs remain unaffected until publish succeeds.
4. Every mutation has an audit trail.
5. Source manifest rows and assets are traceable to published/draft content.

## Edge Cases

- No rows.
- 73-row manifest loaded.
- Blocked source.
- Failed API.
- Unauthorized operator.
- Large result set.
- Mutation conflict.
- Partial extraction evidence.

## Required Tests

- Component tests for table/filter/action states.
- Playwright admin smoke for route and key workflow.
- API integration mock/fixture tests.
- Authorization/error state tests.
- Build passes.

## Acceptance Criteria

- Admin workflow uses real operations APIs.
- Operators can identify current content state and next action.
- Build/tests/Playwright pass.

## Verification

```bash
npm --prefix frontend run build
npm --prefix frontend run test -- --run
npx playwright test --config frontend/playwright.config.ts
rg -n "ContentCoverageDashboard|coverage|media requirement" frontend/src frontend/tests docs/product
```

## Commit

`feat(p8.7): build content coverage dashboard`

## Push

`git push origin main`
