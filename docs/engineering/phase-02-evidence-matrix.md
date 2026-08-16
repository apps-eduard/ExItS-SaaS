# Phase 2 Evidence Matrix

[Phase 2 closeout](../reports/phase-02-extraction-closeout.md) | [Phase 2](../phases/phase-02-platform-extraction.md) | [Gate matrix](implementation-gate-matrix.md)

Statuses reflect Phase 2 closeout (P2-WP06). **Foundation only** means domain/application models or interfaces exist without runtime auth, persistence, transport, or product cutover.

| Capability or Gate | Planned Work Package | Evidence | Validation | Status | Blocking? | Follow-up |
|---|---|---|---|---|---|---|
| Root solution | P2-WP01 | `ExItS.slnx`, `global.json`, Directory.* | Restore/build | Implemented / Validated | No | Continuous |
| Dependency direction | P2-WP01 | ArchitectureTests | 21 arch tests | Validated | No | Continuous |
| portfolio independence verification | P2-WP01–06 | `/legacy product/` ignore; empty `ls-files` | Repo safety tests | Validated | Yes (ongoing) | Until import WP |
| Identity domain | P2-WP02 | PlatformUser + IDs | Unit tests | Foundation only | No | Auth WP |
| Organization domain | P2-WP02 | PlatformOrganization | Unit tests | Foundation only | No | Persistence |
| Membership domain | P2-WP02 | OrganizationMembership + roles | Unit tests | Foundation only | No | Persistence (R-032) |
| Product access | P2-WP02 | ProductAccess concept | Tests + docs | Foundation only | No | Enforcement later |
| Catalog | P2-WP03 | Product/Feature/Plan | Unit tests | Foundation only | No | P3-WP01 |
| Plans and versions | P2-WP03 | Immutable published PlanVersion | Unit + arch tests | Foundation only | No | Phase 3 |
| Trials | P2-WP03 | Configurable TrialDefinition | Unit tests; no 90-day hardcode | Foundation only | No | R-035 Phase 3+ |
| Subscription lifecycle | P2-WP03 | Subscription aggregate | Unit tests | Foundation only | No | P3-WP02 |
| Entitlements | P2-WP03 | Snapshot + composer | Unit tests | Foundation only | No | P3-WP04 / R-022 |
| Feature overrides | P2-WP03 | FeatureOverride | Unit tests | Foundation only | No | Phase 3 |
| Contract envelope | P2-WP04 | ContractEnvelope | Unit tests | Implemented / Validated | No | Transport later |
| Contract versioning | P2-WP04 | ContractVersion | Unit tests | Implemented / Validated | No | R-036 |
| Projection idempotency | P2-WP04 | Applicability evaluator | Unit tests | Implemented / Validated | No | R-037 |
| Reconciliation interfaces | P2-WP04 | Interface-only | Arch tests | Foundation only | No | Transport WP |
| Migration preflight | P2-WP05 | MigrationPreflightValidator | Unit tests | Implemented / Validated | No | Real mapping WP |
| Migration simulation | P2-WP05 | MigrationSimulationService | Unit tests | Implemented / Validated | No | Not production migration |
| Rollback readiness | P2-WP05 | RollbackReadinessValidator | Unit tests | Foundation only | No | R-027 rehearsal |
| Remote publication | P2-WP05 | origin/main + tag | ls-remote | Validated | No | Continuous |
| Authentication | — | Absent by design | API routes / packages | Not started / Prohibited in Phase 2 | — | Post–Phase 2 auth WP |
| Persistence | — | No EF/Npgsql | Grep + arch tests | Not started / Prohibited in Phase 2 | — | Persistence WP |
| legacy product integration | P2-WP04 boundary only | Interfaces; no transport | Docs + freeze | Deferred | No for Phase 2 close | Cutover gates |
| legacy product migration | P2-WP05 dry-run only | Simulation | Docs | Deferred | No for Phase 2 close | After G6–G7 |
| legacy product code retirement | — | None removed | Freeze evidence | Prohibited in Phase 2 | — | After proven cutover |
| Platform Admin | Phase 4 | Absent | No UI projects | Not started | — | Phase 4 |
| PinoyBusinessPOS | Phase 5+ | Absent | No POS projects | Not started | — | Phase 5 |
| G1 Solution foundation | P2-WP01 | Build + arch tests | Release | Met | No | — |
| G2 Identity (login) | Future | Domain only | — | Partial | Yes before legacy product auth cutover | Auth WP |
| G6 Mapping dry run | P2-WP05 | Platform validators | Unit tests | Partial | Yes before legacy product adapter enable | legacy product data + backups |
| G7 legacy product regression | Cutover | 1102 not rerun | Explicit | Not started | Yes before cutover | Supported env |
