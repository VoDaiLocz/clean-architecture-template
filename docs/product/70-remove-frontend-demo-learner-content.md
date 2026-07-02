# Remove Frontend Demo Learner Content

## Task

P7.1 - Remove Frontend Demo Learner Content

## Purpose

Remove hardcoded learner demo content and establish the Angular production frontend baseline so the UI cannot hide missing backend flow behind fake TOEIC questions.

## Detailed Scope

- Build Angular TypeScript UI against real typed API services, route guards, interceptors, and feature-level lazy loading.
- Replace the legacy Vite TypeScript learner surface with an Angular application baseline if Angular has not already been scaffolded.
- Add Angular app shell wiring, environment configuration, API base URL configuration, and route-level lazy loading.
- Implement loading, empty, error, unauthorized, and success states.
- Keep business decisions in backend responses.
- Add accessible controls and responsive layouts.
- Add Playwright coverage for the core user path.

## Out Of Scope

- Backend business-rule implementation.
- Hardcoded TOEIC content.
- Raw PDF/Drive document navigation as the primary learner experience.
- Marketing landing-page redesign.

## Data Contract

Frontend state stores only view state and cached API responses. It does not store source-of-truth mastery, scoring, unlock, placement, or answer-key logic.

## API Contract

Consumes typed backend APIs from the corresponding P4-P6 tasks. All failure states use standardized error contract once P9.4 exists; before that, UI still handles stable code/message shapes.

## UI Contract

UI must be production-user focused: clear primary action, no internal build/admin wording in learner screens, no fake content, responsive desktop/mobile, and no answer leaks before submit.

Angular baseline requirements:

- `angular.json`, Angular build scripts, and TypeScript configuration exist.
- `src/app/core`, `src/app/shared`, `src/app/features/learner`, `src/app/features/admin`, and `src/app/features/auth` boundaries exist.
- API interceptor handles auth/correlation/error basics.
- Router has learner/admin/auth route groups.
- Existing demo-only frontend data is removed or quarantined outside production routes.

## Business Rules

1. UI follows backend `NextAction`, lock state, and blocker reasons.
2. No learner-facing route renders hardcoded question/vocab banks.
3. Error and empty states are useful and not silent.
4. Authentication guard is respected where available.
5. Keyboard and screen-reader basics are handled for forms and exam controls.

## Edge Cases

- API loading.
- API error.
- Empty content.
- Unauthorized/expired session.
- Mobile viewport.
- Long text/audio controls.
- Refresh during in-progress work.

## Required Tests

- Component/unit tests for view states.
- Playwright happy path for the screen.
- Negative test for fake content/answer leak where relevant.
- Responsive smoke screenshot or viewport check.
- Build passes.

## Acceptance Criteria

- UI uses real APIs and no frontend-owned learning logic.
- Core user path works in Playwright.
- Build/tests pass.
- Angular baseline exists and production learner routes do not depend on legacy Vite demo fallback.

## Verification

```bash
npm --prefix frontend run build
npm --prefix frontend run test
npm --prefix frontend run test:e2e:browser
rg -n "DemoLearner|mockLearner|hardcoded question|static fallback" frontend/src frontend/tests docs/product
```

## Commit

`refactor(p7.1): remove frontend demo learner content`

## Push

`git push origin main`
