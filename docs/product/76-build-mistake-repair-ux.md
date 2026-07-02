# Build Mistake Repair UX

## Task

P7.7 - Build Mistake Repair UX

## Purpose

Build review/repair workspace for wrong answers with explanation, evidence, media replay, and repair submission.

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

Mistake repair requirements:

- review queue grouped by blocker, unit, TOEIC part, and skill tag
- repair detail shows original question context, learner answer, correct answer, explanation, and evidence
- listening repair can replay relevant audio
- reading repair highlights passage evidence when supplied by API
- blocker reason is visible before repair action
- repair submission uses backend result to resolve or keep blocker
- resolved animation/status update is allowed but must not hide failed repair
- empty review state directs learner back to Today

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
- Learner can understand a mistake, attempt repair, and see whether the blocker is resolved.

## Verification

```bash
npm --prefix frontend run build
npm --prefix frontend run test
npm --prefix frontend run test:e2e:browser
rg -n "MistakeRepair|ReviewQueue|repair" frontend/src frontend/tests docs/product
```

## Commit

`feat(p7.7): build mistake repair UX`

## Push

`git push origin main`
