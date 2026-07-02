# Build Extraction Operations Dashboard

## Purpose
P8.3 implements extraction job runner console.

## Rules
1. Real-time run console log panel, retry triggers, status metrics.

## Verification
```bash
npm --prefix frontend run test
rg -n "ExtractionOperations" frontend/src
```
