# P0 - Product And Documentation Reset

## Phase Goal

Remove conflicting product direction, establish official enterprise-grade documentation, and prevent future development from building on demo behavior.

## P0.1 - Reset Official Product Documentation

**Context:** Product governance  
**Purpose:** Replace fragmented TOEIC specs with a structured documentation system that the whole team can use.  
**User/Business Value:** Prevents scope drift and gives dev/product/design/QA one shared product definition.  
**Dependencies:** none.  
**Detailed Scope:** Delete old TOEIC specs in `docs/superpowers/specs`; create `docs/product`; add master, product, domain, journey, architecture, data, API, UX, quality, release, and task docs.  
**Out Of Scope:** Code changes, UI changes, DB changes.  
**Data Contract:** none.  
**API Contract:** none.  
**UI Contract:** none.  
**Business Rules:** This documentation set becomes the only official spec source.  
**Edge Cases:** Existing dirty code files must not be included in the commit.  
**Required Tests:** Verify file structure and placeholder scan.  
**Acceptance Criteria:** `docs/product/00-master-spec.md` exists; task breakdown exists for P0-P9; old tracked TOEIC specs are deleted; no old spec file remains under `docs/superpowers/specs`.  
**Verification Commands:** `find docs/product -type f | sort`; `find docs/superpowers/specs -maxdepth 1 -type f | sort`; `rg -n "TBD|TODO|FIXME" docs/product`.  
**Definition Of Done:** Docs are committed and pushed.  
**Commit:** `docs(p0.1): reset TOEIC product specification`  
**Push:** `git push origin main`

## P0.2 - Deprecate Demo Learner Direction

**Context:** Product architecture  
**Purpose:** Ensure production work does not build on `DemoLearnerSession` or frontend fake content.  
**User/Business Value:** Prevents a demo flow from becoming the commercial learner experience.  
**Dependencies:** P0.1.  
**Detailed Scope:** Add explicit deprecation documentation and tests marking demo session as non-production; identify replacement endpoints.  
**Out Of Scope:** Full replacement of learner engine.  
**Data Contract:** none.  
**API Contract:** Future learner APIs must not depend on demo session.  
**UI Contract:** Production UI must not consume demo-only content.  
**Business Rules:** Demo code can exist only as temporary legacy code until replacement phase removes it.  
**Edge Cases:** Existing tests may still reference demo flow; mark them as legacy or replace when production journey lands.  
**Required Tests:** Test or static check that new production handlers do not depend on demo session.  
**Acceptance Criteria:** Team can identify demo code and knows it is not production.  
**Verification Commands:** `rg -n "DemoLearnerSession|fallback|hardcoded" backend/src frontend/src`.  
**Definition Of Done:** Boundary is documented and pushed.  
**Commit:** `chore(p0.2): mark demo learner flow as non-production`  
**Push:** `git push origin main`

## P0.3 - Define Bounded Context Ownership

**Context:** Architecture governance  
**Purpose:** Lock responsibility boundaries before implementation.  
**User/Business Value:** Reduces future coupling and makes team work parallelizable.  
**Dependencies:** P0.1.  
**Detailed Scope:** Add ADR or docs defining Content Factory, Learning Content, Learner Journey, Attempt/Review, Analytics boundaries.  
**Out Of Scope:** Schema implementation.  
**Data Contract:** Context ownership table.  
**API Contract:** Admin APIs separate from learner APIs.  
**UI Contract:** Admin screens separate from learner screens.  
**Business Rules:** Learner context never exposes source factory internals.  
**Edge Cases:** Shared read models are allowed only for analytics.  
**Required Tests:** Architecture dependency checks when tooling exists; otherwise documented review checklist.  
**Acceptance Criteria:** Every future task maps to one context owner; phase owner map exists; cross-context contract rules exist; review checklist exists.  
**Verification Commands:** `rg -n "Ownership Matrix|Phase Owner Map|Cross-Context Contract Types|Review Checklist" docs/product/04-bounded-context-ownership.md`; `rg -n "Content Factory|Learner Journey|Attempt And Review" docs/product`.  
**Definition Of Done:** Context rules documented and pushed.  
**Commit:** `docs(p0.3): define TOEIC bounded context ownership`  
**Push:** `git push origin main`
