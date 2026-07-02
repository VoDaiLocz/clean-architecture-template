# P7 - Learner UX Production

## Phase Goal

Build learner UI on real APIs with no fake content and no frontend-owned learning logic.

## Source Of Truth

Detailed implementation contracts live in the standalone task specs below. This phase file is an execution index only.

| Task | Detailed Spec | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- | --- |
| P7.1 | [70-remove-frontend-demo-learner-content.md](../70-remove-frontend-demo-learner-content.md) | Stop demo leakage | FE has no hardcoded question/vocab bank | `refactor(p7.1): remove frontend demo learner content` |
| P7.2 | [71-build-learner-app-shell.md](../71-build-learner-app-shell.md) | Production learner layout | Today, Learn, Practice, Review, Tests, Progress | `feat(p7.2): build learner app shell` |
| P7.3 | [72-build-onboarding-and-placement-ux.md](../72-build-onboarding-and-placement-ux.md) | Start learner correctly | Placement CTA and flow use APIs | `feat(p7.3): build onboarding and placement UX` |
| P7.4 | [73-build-learner-today-screen.md](../73-build-learner-today-screen.md) | Main daily workflow | Next activity, blockers, progress shown | `feat(p7.4): build learner today screen` |
| P7.5 | [74-build-lesson-and-example-ux.md](../74-build-lesson-and-example-ux.md) | Teach before practice | Lesson and guided example activities render | `feat(p7.5): build lesson and example UX` |
| P7.6 | [75-build-drill-and-mini-test-ux.md](../75-build-drill-and-mini-test-ux.md) | Practice correctly | Attempts submit to API; results shown | `feat(p7.6): build drill and mini test UX` |
| P7.7 | [76-build-mistake-repair-ux.md](../76-build-mistake-repair-ux.md) | Repair wrong answers | Evidence/explanation/repair action shown | `feat(p7.7): build mistake repair UX` |
| P7.8 | [77-build-toeic-part-overview.md](../77-build-toeic-part-overview.md) | Real part navigation | Locked/unlocked/progress shown from API | `feat(p7.8): build TOEIC part overview` |
| P7.9 | [78-build-toeic-practice-test-ux.md](../78-build-toeic-practice-test-ux.md) | Exam-like flow | Timer, navigation, submit behavior | `feat(p7.9): build TOEIC practice test UX` |
| P7.10 | [79-build-learner-progress-ux.md](../79-build-learner-progress-ux.md) | Show improvement | Progress and test breakdown visualized | `feat(p7.10): build learner progress UX` |

## Required P7 Acceptance Standard

- Learner UI uses real APIs only.
- No frontend-owned placement, scoring, unlock, answer-key, review, or assignment logic.
- Every learner route has loading, error, empty, unauthorized, desktop, and mobile coverage.
