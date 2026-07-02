# Build Admin Asset Discovery

## Purpose
P8.2 implements container asset inspector panel.

## Rules
1. File explorer view, categorization dropdowns, checksum status indicators.

## Verification
```bash
npm --prefix frontend run test
rg -n "AdminAssetDiscovery" frontend/src
```
