# Add Release Readiness Checklist

## Task

P9.10 - Add Release Readiness Checklist

## Purpose

Define the go/no-go checklist for private beta, public beta, and market release.

## Detailed Scope

- Add product scope checklist.
- Add TOEIC content readiness checklist.
- Add learner journey checklist.
- Add admin operations checklist.
- Add auth/security checklist.
- Add performance and observability checklist.
- Add backup/restore checklist.
- Add CI/CD/deployment checklist.
- Add known risks and sign-off table.

## Out Of Scope

- Implementing missing release blockers.
- Marketing launch plan.
- Pricing/legal documents.

## Go/No-Go Rules

Public release is blocked by:

- Authentication or authorization missing.
- Learner journey cannot complete onboarding to review/test result.
- TOEIC part engines missing required media/content validation.
- Admin cannot publish reviewed content safely.
- Backup/restore rehearsal missing.
- Critical security finding open.
- CI/CD release gate failing.
- Production health/readiness failing.

## Required Evidence

Each checklist item must include:

- owner
- status
- evidence command/link
- blocker severity
- decision date

## Required Checks

- No empty checklist rows.
- Every critical release rule has evidence.
- Known risks are explicit.
- Rollback plan is documented.

## Acceptance Criteria

- Checklist supports clear private beta/public beta/market go-no-go decision.
- Missing evidence is visible as blocker.
- Docs scan has no unresolved marker text.

## Verification

```bash
rg -n "Release Readiness|Go/No-Go|rollback|restore rehearsal|security baseline" docs/product
rg -n "TB[D]|TO[D]O" docs/product/96-add-release-readiness-checklist.md
```

## Commit

`docs(p9.10): add release readiness checklist`

## Push

```bash
git push origin main
```
