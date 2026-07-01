# P9 - Production Hardening And Release

## Phase Goal

Make the product secure, observable, deployable, and release-ready.

| Task | Purpose | Key Pass Criteria | Commit |
| --- | --- | --- | --- |
| P9.1 Authentication | Identify users | Learner/admin login works | `feat(p9.1): add authentication` |
| P9.2 Authorization | Protect roles | Admin APIs inaccessible to learners | `feat(p9.2): enforce learner admin authorization` |
| P9.3 Observability | Operate production | Logs, metrics, traces available | `feat(p9.3): add production observability` |
| P9.4 Error handling | Stable failures | Error codes and user-safe messages | `feat(p9.4): standardize error handling` |
| P9.5 Performance baseline | Prevent slow UX | API p95 targets measured | `perf(p9.5): establish performance baseline` |
| P9.6 Security baseline | Reduce risk | OWASP checks and secret handling | `sec(p9.6): add security baseline` |
| P9.7 Backup/migration strategy | Protect data | Restore and migration checks documented/tested | `chore(p9.7): add backup and migration strategy` |
| P9.8 CI/CD release pipeline | Ship safely | Build/test/deploy gates run | `ci(p9.8): add release pipeline` |
| P9.9 Deployment config | Run production | PostgreSQL, object storage, secrets configured | `chore(p9.9): add production deployment config` |
| P9.10 Release readiness checklist | Decide go/no-go | Checklist completed before market release | `docs(p9.10): add release readiness checklist` |

