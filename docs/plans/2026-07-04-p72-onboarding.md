# Phase P72 Implementation Plan: Onboarding & Placement UX

## Context
We are implementing the frontend components for the Onboarding and Placement flow.
The user chose Option 1A (Multi-step Wizard) for Onboarding and Option 2A (One-Question-Per-Screen) for Placement.

## Tasks

### Task 1: API Services
- Create `frontend/src/app/core/api/learner-api.service.ts`.
- Add `provideHttpClient()` to `frontend/src/app/app.config.ts`.
- Implement `onboardLearner`, `startPlacement`, `scorePlacement`, `generatePath` methods based on `Program.cs` routes.

### Task 2: Onboarding Component
- Create `frontend/src/app/features/learner/onboarding/onboarding.component.ts` and `.html`.
- Implement a multi-step form (Step 1: Goal/Target, Step 2: Time/Focus).
- Call `/api/learner/onboarding`.
- Handle `NextAction` to navigate to placement.

### Task 3: Placement Component
- Create `frontend/src/app/features/learner/placement/placement.component.ts` and `.html`.
- Start session via `/api/learner/placement/start`.
- Display one question at a time (progress bar, option selection).
- Submit via `/api/learner/placement/score` upon completion.

### Task 4: Placement Result Component
- Create `frontend/src/app/features/learner/placement-result/placement-result.component.ts` and `.html`.
- Display diagnostic score estimate.
- Button to call `/api/learner/path/generate` and navigate to the Today screen.

### Task 5: Routing Integration
- Update `frontend/src/app/app.routes.ts` to include the onboarding and placement routes.
- Verify with `npm --prefix frontend run build`.
