# Build Content Publish Queue

## Purpose
P8.6 implements batch draft publisher control interface.

## Rules
1. Run pre-publish audits, trigger publisher, track history.

## Verification
```bash
npm --prefix frontend run test
rg -n "ContentPublishQueue" frontend/src
```
