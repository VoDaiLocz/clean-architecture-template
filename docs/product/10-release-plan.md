# Release Plan

## Release Gate Policy

No release stage can open because a date has arrived. A stage opens only when required product capability, quality evidence, operational readiness, and go/no-go review are complete.

Each gate must record:

- release owner
- scope included
- scope excluded
- required evidence links
- unresolved risks
- go/no-go decision
- rollback or exit plan

Severity rules:

- Critical learner journey failure blocks every external release.
- Critical content quality failure blocks every external release.
- Authentication or authorization failure blocks every external release.
- Data loss, migration failure, or missing backup rehearsal blocks market release.
- A known issue can pass only if it is documented, non-critical, and has an owner.

## Alpha Internal

Purpose: validate domain and internal content flow.

Audience:

- internal product, engineering, QA, and content operators only

Required:

- P0 complete
- P1 complete
- partial P2 with source asset discovery
- partial P3 with Part 5, Part 2, Part 7 sample content
- backend tests green
- no learner-facing raw PDF/Drive flow
- demo-only learner flow explicitly marked non-production

Exit criteria:

- internal user can import source, publish sample content, and create learner assignment
- content operator can see source inventory, extraction state, draft state, validation issue, and publish state
- engineering can identify context owner for every active task
- all P0 documentation gates are pushed to remote

Evidence:

- backend build/test logs
- source manifest import evidence
- sample publish evidence
- context ownership review
- known-risk list

## Private Beta

Purpose: validate learner journey with controlled users.

Audience:

- controlled learners and internal operators

Required:

- P4 complete
- P5 Part 2, Part 5, Part 7 complete
- P7 Today, Activity, Review screens complete
- E2E onboarding-to-review flow passing
- persisted learner profile, placement, assignment, attempt, review, and mastery state
- no production learner screen depends on demo-only fallback content
- admin can publish and unpublish learner-visible content through controlled workflow

Exit criteria:

- learner can complete placement, study, make mistakes, repair, and unlock
- learner understands why a unit is locked
- wrong answers create review work
- review completion can unblock progress when mastery rules allow it
- core learner flows work on desktop and mobile viewport

Evidence:

- E2E video or trace for onboarding-to-review
- API contract test output
- content validation report
- issue triage list with severity and owner

## Public Beta

Purpose: validate all 7 parts and practice test behavior.

Audience:

- broader learner cohort with production monitoring enabled

Required:

- P5 all part engines complete
- P6 mini, part, listening, reading tests complete
- P8 admin review usable
- all 7 parts have published validated content samples
- listening parts have audio playback and required media validation
- reading parts have passage/evidence validation
- test session state supports refresh/resume/submit handling
- support and incident workflow exists

Exit criteria:

- learners can study all 7 parts and take part/skill tests
- learners can take listening section, reading section, and targeted part tests
- score breakdown produces repair plan
- content coverage dashboard identifies gaps by part/media/status
- production monitoring catches API errors, slow endpoints, failed jobs, and content publish failures

Evidence:

- 7-part E2E coverage report
- practice test result report
- performance smoke report
- admin operations walkthrough
- support readiness checklist

## Market Release

Purpose: commercial release.

Audience:

- real public market users

Required:

- P6 full TOEIC test complete
- P9 complete
- production deployment rehearsed
- critical E2E tests passing
- content quality targets met
- authentication and authorization complete
- backup and restore rehearsal complete
- observability dashboards live
- production error handling stable
- deployment rollback documented
- release readiness checklist approved

Exit criteria:

- system is secure, observable, deployable, and usable by real learners
- learner can complete the full path from onboarding to study to review to practice test result
- admin can operate the source-to-publish pipeline without engineering intervention
- operations can detect, diagnose, and roll back production failures
- content quality meets published market threshold

Evidence:

- CI/CD green run
- security baseline report
- backup restore evidence
- full TOEIC test E2E trace
- launch go/no-go record
- rollback rehearsal record

## Go/No-Go Checklist

Use this checklist for every release gate:

1. Is the required phase scope complete and pushed?
2. Are all mandatory tests green?
3. Are known critical and high issues closed?
4. Are learner-facing flows free of raw source/PDF/Drive navigation?
5. Are learner decisions backend-owned?
6. Are content quality gates enforced?
7. Are admin/operator workflows ready for the gate audience?
8. Are metrics/logs available for the gate audience?
9. Is rollback or exit plan documented?
10. Has the release owner recorded a go/no-go decision?

If any answer is no, the release does not pass.
