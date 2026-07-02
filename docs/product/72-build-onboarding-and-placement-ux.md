# Build Onboarding and Placement UX

## Purpose
P7.3 implements onboarding profile forms and placement quiz view.

## Rules
1. Diagnostic screen disables review tabs and forces sequence submit.

## Verification
```bash
npm --prefix frontend run test
rg -n "OnboardingUX" frontend/src
```
