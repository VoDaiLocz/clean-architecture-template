# Build Learner App Shell

## Task

P7.2 - Build Learner App Shell

## Purpose

Build the production learner navigation shell for Today, Learn, Practice, Review, Tests, and Progress routes.

## Detailed Scope

- Build Angular TypeScript UI against real typed API services, route guards, interceptors, and feature-level lazy loading.
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

App shell requirements:

- persistent learner navigation: Today, Learn, Practice, Review, Tests, Progress
- secondary entry to the 7-part overview
- authenticated user menu with profile/settings/logout
- route-level loading skeletons
- global API error banner with correlation id when available
- mobile bottom navigation or compact drawer
- desktop layout that keeps the primary study area visually dominant
- no landing-page hero inside authenticated app
- Angular route guards for authenticated learner routes
- lazy-loaded learner feature routes

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
- Learner can navigate all primary routes on desktop and mobile without placeholder pages.

## Verification

```bash
npm --prefix frontend run build
npm --prefix frontend run test
npm --prefix frontend run test:e2e:browser
rg -n "AppShell|LearnerNavigation|ProtectedRoute" frontend/src frontend/tests docs/product
```

## Commit

`feat(p7.2): build learner app shell`

## Push

`git push origin main`
