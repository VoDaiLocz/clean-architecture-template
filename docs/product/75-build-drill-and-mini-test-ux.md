# Build Drill and Mini Test UX

## Task

P7.6 - Build Drill and Mini Test UX

## Purpose

Build drill and mini-test answer flows with server submissions, result states, and no pre-submit answer leaks.

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

Drill and mini-test requirements:

- answer choice controls with keyboard support
- explicit skip support where backend allows it
- question progress and unanswered indicator
- audio player for listening parts
- passage reader for reading parts
- sticky submit/next controls on mobile without covering answers
- submit confirmation for mini tests
- result state from backend only
- no correct answer, explanation, or score before submit
- mini-test locked/unlocked state comes from unit mastery API

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
- Learner can complete a drill or unit mini test without answer leakage or local scoring.

## Verification

```bash
npm --prefix frontend run build
npm --prefix frontend run test
npm --prefix frontend run test:e2e:browser
rg -n "Drill|MiniTest|SubmitAttempt" frontend/src frontend/tests docs/product
```

## Commit

`feat(p7.6): build drill and mini test UX`

## Push

`git push origin main`
