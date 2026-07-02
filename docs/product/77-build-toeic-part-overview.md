# Build TOEIC Part Overview

## Purpose
P7.8 implements roadmap overview of all 7 parts.

## Rules
1. Visual lock/unlock representation with locked reason tooltips.

## Verification
```bash
npm --prefix frontend run test
rg -n "PartOverviewUX" frontend/src
```
