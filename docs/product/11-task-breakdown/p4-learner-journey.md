# P4 - Learner Journey Core

## Phase Goal

Replace demo learner flow with persisted production journey.

| Task | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- |
| P4.1 Learner onboarding | Capture learner goal | Profile persisted; next action returned | `feat(p4.1): implement learner onboarding` |
| P4.2 Learner profile persistence | Remove memory-only learner state | Profile survives restart | `feat(p4.2): persist learner profile state` |
| P4.3 Placement test start | Create placement session | Duplicate active session handled | `feat(p4.3): start TOEIC placement session` |
| P4.4 Placement scoring | Diagnose part weaknesses | Result has accuracy by part and tags | `feat(p4.4): score TOEIC placement` |
| P4.5 Learning path generation | Create path from diagnosis | Starting units generated from placement | `feat(p4.5): generate learner path from placement` |
| P4.6 Today Plan assignment | Assign next best activity | Review blockers outrank new lessons | `feat(p4.6): assign learner today plan` |
| P4.7 Activity session lifecycle | Track activity state | Start, resume, complete states persist | `feat(p4.7): manage learner activity sessions` |
| P4.8 Attempt submission | Score learner work | Attempt persists and returns result | `feat(p4.8): process learner attempts` |
| P4.9 Mistake review queue | Force repair | Wrong answer creates review item | `feat(p4.9): create learner review queue` |
| P4.10 Mastery unlock engine | Enforce progression | Unit unlocks only after all gates pass | `feat(p4.10): enforce mastery unlocks` |

## P4.1 - Implement Learner Onboarding

**Context:** Learner Journey  
**Purpose:** Create or update the learner's TOEIC learning profile and return the first backend-owned next action.  
**User/Business Value:** Lets a real learner enter the product with durable goals, current level, daily study capacity, and a clear next step toward diagnosis instead of demo/static UI state.  
**Dependencies:** P2.7, P1.6.  
**Detailed Scope:** Add onboarding command/result models; add `OnboardLearnerHandler`; persist learner profile through `IKnowledgeRepository`; register typed API contract; map `POST /api/learner/onboarding`; add application/API-contract test; add docs.  
**Out Of Scope:** authentication account creation, placement session creation, learning path generation, frontend onboarding screen, marketing profile fields, payments.  
**Data Contract:** Onboarding writes one `learner_profiles` row keyed by `learner_id`; repeat onboarding updates the same row and preserves durable identity.  
**API Contract:** `POST /api/learner/onboarding` accepts learner id, display name, email, target score, current estimated score, daily study minutes, and timezone; response is `OnboardLearnerResponse` with persisted profile summary and `NextAction`.  
**UI Contract:** Future UI must display the returned next action and must not decide placement routing itself.  
**Business Rules:** Learner profile must be active after onboarding; TOEIC score/minute validation remains repository/domain-owned; next action after onboarding is `StartPlacement` until placement flow is implemented.  
**Edge Cases:** Repeat onboarding updates the profile idempotently; invalid score/minutes fail before persistence; timezone/name/email are required by repository validation.  
**Required Tests:** Application test persists profile, checks next action, checks typed API contract, and checks repeated onboarding updates without duplicate profile rows.  
**Acceptance Criteria:** Handler exists; endpoint is mapped; API contract is registered; profile persists; repeat onboarding updates; next action returned; tests and build pass.  
**Verification Commands:** `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`; `dotnet build backend/ToeicSystem.sln`; `rg -n "OnboardLearner|/api/learner/onboarding|StartPlacement" backend/src backend/tests docs/product`.  
**Definition Of Done:** Learner onboarding workflow is committed and pushed.  
**Commit:** `feat(p4.1): implement learner onboarding`  
**Push:** `git push origin main`

## Required P4 Acceptance Standard

`DemoLearnerSession` is not used by production endpoints after this phase.
