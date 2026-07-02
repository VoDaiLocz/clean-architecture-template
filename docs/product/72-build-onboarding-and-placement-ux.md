# Build Onboarding and Placement UX

## Task

P7.3 - Build Onboarding and Placement UX

## Purpose

Build onboarding and placement screens that follow backend next actions instead of client-owned routing.

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

Onboarding and placement requirements:

- reactive onboarding form with target score, current estimate, daily minutes, timezone, and study goal
- inline validation with backend error-code handling
- backend `NextAction` controls whether the learner starts placement, resumes placement, or goes to Today
- placement question layout supports progress, explicit skip, question navigation, and submit confirmation
- diagnostic result screen clearly labels score as estimate/band, not official TOEIC score
- result screen shows part weaknesses and next learning-path action
- refresh during placement resumes active session instead of losing answers
- no correct answers or explanations are visible before placement submit

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
- Learner can complete onboarding, start/resume placement, submit, and understand next action.

## Verification

```bash
npm --prefix frontend run build
npm --prefix frontend run test
npm --prefix frontend run test:e2e:browser
rg -n "Onboarding|Placement|StartPlacement" frontend/src frontend/tests docs/product
```

## Commit

`feat(p7.3): build onboarding and placement UX`

## Push

`git push origin main`
