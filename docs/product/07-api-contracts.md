# API Contract Specification

## Contract Rules

- APIs return business state, not frontend instructions.
- APIs must not leak admin/source internals to learner endpoints.
- Error responses must use stable error codes.
- All state-changing endpoints must be idempotent or explicitly reject duplicates.
- API contracts must be covered by application/API tests.

## Learner APIs

### GET /api/learner/home

Purpose: return learner current state and primary CTA.

Must return:

- learner profile summary
- placement status
- today plan summary
- review count
- current progress

Must not return:

- raw source URLs
- admin validation issues
- parser data

### POST /api/learner/onboarding

Purpose: create or update learner profile.

Required request:

- target score
- current estimate
- test date optional
- study time per day

Pass criteria:

- profile persists
- response includes next required action

### POST /api/learner/placement/start

Purpose: create placement session.

Pass criteria:

- duplicate active session is rejected or resumed
- questions are assigned from published placement pool

### POST /api/learner/assignments/{assignmentId}/attempts

Purpose: submit activity answer/work.

Pass criteria:

- attempt persists
- scoring runs server-side
- review item is created when wrong
- next assignment can be recalculated

## Admin APIs

### POST /api/admin/sources/import-manifest

Purpose: import source inventory.

Pass criteria:

- source rows persist
- blocked sources persist as blocked
- import is idempotent

### POST /api/admin/sources/{sourceId}/discover

Purpose: discover source containers and assets.

Pass criteria:

- assets persist
- failures create issues
- repeated discovery does not duplicate assets

### POST /api/admin/drafts/{draftId}/publish

Purpose: publish approved content.

Pass criteria:

- validation gates run
- invalid draft cannot publish
- published content is visible to learner APIs only after success

