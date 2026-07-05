# Release Readiness Checklist

## 1. Go/No-Go Rules (Blockers)
- [ ] Authentication is fully functional (sign up, sign in, token refresh).
- [ ] CI/CD pipeline is passing (build, test, deployment).
- [ ] No critical security vulnerabilities identified in the latest scan.

## 2. Product Scope Checklist
- [ ] Core MVP features are implemented and functioning.
- [ ] UI/UX matches approved design specs.
- [ ] Cross-device compatibility tested (desktop, mobile).

## 3. TOEIC Content Readiness Checklist
- [ ] Minimum viable test content uploaded and verified.
- [ ] Audio files for listening sections are accessible and play correctly.
- [ ] Reading section texts and questions are formatted correctly.

## 4. Learner Journey Checklist
- [ ] Onboarding flow completed successfully for new users.
- [ ] Daily plan generation and tracking works as expected.
- [ ] Practice flow (taking tests, submitting answers, viewing results) is functional.

## 5. Admin Operations Checklist
- [ ] Admin panel is accessible to authorized users only.
- [ ] Content upload (questions, audio, images) is functioning.
- [ ] Content publishing workflow is verified.

## 6. Auth/Security Checklist
- [ ] CORS policies are correctly configured.
- [ ] Rate limiting is enforced on critical endpoints.
- [ ] HSTS is enabled in production environments.

## 7. Performance and Observability Checklist
- [ ] k6 smoke tests pass against staging/production-like environment.
- [ ] Serilog structured logging is active and logs are accessible.
- [ ] Critical metrics and dashboards are operational.

## 8. Backup/Restore Checklist
- [ ] SQLite database automated backups are configured.
- [ ] Backup restore runbook has been rehearsed recently.
- [ ] Backup retention policies are defined and applied.

## 9. CI/CD/Deployment Checklist
- [ ] GitHub Actions CI/CD pipeline is green for the main branch.
- [ ] Infrastructure as code (if applicable) is up to date and deployed.
- [ ] Post-deployment verification tests passed.

## 10. Known Risks and Sign-off Table
| Role | Name | Status | Date | Notes |
| :--- | :--- | :--- | :--- | :--- |
| Product Manager | | [ ] Approved | | |
| Lead Engineer | | [ ] Approved | | |
| QA Lead | | [ ] Approved | | |
| Operations Lead | | [ ] Approved | | |
