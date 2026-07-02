# Build Admin Source Inventory

## Purpose
P8.1 implements manifest listings page for content managers.

## Rules
1. Search, filter by provider or audit status, blocked toggle switch.

## Verification
```bash
npm --prefix frontend run test
rg -n "AdminSourceInventory" frontend/src
```
