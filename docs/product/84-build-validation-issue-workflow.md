# Build Validation Issue Workflow

## Purpose
P8.5 implements validation warning log view.

## Rules
1. Gaps list, manual validation override control with operator log.

## Verification
```bash
npm --prefix frontend run test
rg -n "ValidationIssueWorkflow" frontend/src
```
