# Architecture Decisions

[Home](../index.md)

| ID | Decision | Status |
|---|---|---|
| ADR-001 | Controlled monorepo initially | Proposed |
| ADR-002 | Reuse domain-neutral foundation patterns after assessment | Accepted |
| ADR-003 | Separate Platform and product databases | Accepted |
| ADR-004 | Keep framework-specific UI implementations separate; Platform Admin Ant adoption is ADR-015 | Accepted — detail in [ADR-010](ADR-010-separate-ui-implementations-platform-and-pos.md) / [ADR-015](ADR-015-antdesign-blazor-platform-admin.md) |
| ADR-005 | Native CSS for PinoyBusinessPOS (Admin now Ant Design per ADR-015) | Accepted — detail in [ADR-010](ADR-010-separate-ui-implementations-platform-and-pos.md) |
| ADR-006 | English and Filipino/Tagalog from POS MVP | Accepted |
| ADR-007 | Light, dark and system themes | Accepted |
| ADR-008 | Native date wrapper first; custom calendar only by requirement | Accepted |
| ADR-009 | Local entitlement snapshots protect product availability | **Accepted** (confirmed by ADR-011) |
| ADR-010 | Separate UI stacks; Admin Ant Design (ADR-015); POS native | **Accepted (amended P15)** — [Open](ADR-010-separate-ui-implementations-platform-and-pos.md) |
| ADR-011 | Platform authority and product-local projections | **Accepted** — [Open](ADR-011-platform-authority-and-product-local-projections.md) |
| ADR-012 | Versioned Platform contracts and local product projections | **Accepted** — [Open](ADR-012-versioned-platform-contracts-and-local-projections.md) |
| ADR-013 | Build the Platform foundation before any external product integration | **Accepted (historical)** — ADR file removed; portfolio is Platform + PinoyBusinessPOS only |
| ADR-014 | Approve ExItS portfolio architecture for controlled implementation | **Accepted** — [Open](ADR-014-approve-exits-portfolio-architecture-for-controlled-implementation.md) |
| ADR-015 | Ant Design Blazor for Platform Admin (Pro as design reference) | **Accepted** — [Open](ADR-015-antdesign-blazor-platform-admin.md) |
| ADR-016 | Account profile isolation (Platform / Personal / Organization) | **Accepted** — [Open](ADR-016-account-profile-isolation.md) |
| ADR-017 | Scope-bound sessions and API family guards | **Accepted** — [Open](ADR-017-scope-bound-sessions.md) |
| ADR-018 | Platform Support Session isolation | **Accepted** — [Open](ADR-018-platform-support-session-isolation.md) |
| ADR-019 | Personal Utang versus Business Credit ownership | **Accepted** — [Open](ADR-019-personal-utang-versus-business-credit-ownership.md) |
| ADR-020 | Personal Utang migration and provenance | **Accepted** — [Open](ADR-020-personal-utang-migration-and-provenance.md) |
| ADR-021 | Linked customer statements and Personal monetization | **Accepted** (architecture contract; not implemented) — [Open](ADR-021-linked-customer-statements-and-personal-monetization.md) |
| ADR-022 | Separated AntDesign browser hosts and unified authentication | **Accepted** — [Open](ADR-022-separated-antdesign-web-hosts-and-unified-auth.md) |
| ADR-023 | Organization Supplier Payables (AP) vs Customer Utang (AR) | **Accepted** — [Open](ADR-023-organization-supplier-payables.md) |

Cursor creates detailed ADR files only when the related phase validates the decision against the repository.
