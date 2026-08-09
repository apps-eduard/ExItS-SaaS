# Phase 21 — Privacy, Compliance, and Regulatory Readiness

[Portfolio](../portfolio-progress.md) | [Phases](README.md) | [Phase 20](phase-20-global-product-catalog-and-business-template-onboarding.md) | [Authorization](../engineering/authorization-matrix.md) | [Security](../engineering/security.md)

| Field | Value |
|---|---|
| Status | **Open** |
| Overall | **Foundation Code Complete — readiness tooling only** |
| Feature tip | `7f6795b` |
| Tests tip | `26ec821` |
| Device Verified | **No** |
| Production Ready | **No** |
| Legal / NPC Compliant | **No — not claimed** |
| DPO / legal review | **Required** |

## Objective

Build a **Platform-only** Privacy & Compliance workspace that tracks Philippine Data Privacy Act / NPC **readiness** documentation (requirements, processing systems, evidence references, PDF exports). This is a **compliance-management system**, not a claim that ExItS is legally compliant or NPC-registered.

Never display “Compliant” merely because records exist. Prefer language: readiness, documented, implemented, pending review, not verified, legal/DPO review required.

## Work packages

| WP | Name | Target status |
|---|---|---|
| P21-WP01 | Requirements & privacy inventory | **Code Complete** (foundation) |
| P21-WP02 | Platform compliance workspace | **Code Complete** (foundation) |
| P21-WP03 | Document registry / status | **Code Complete** (foundation) |
| P21-WP04 | Processing systems / data inventory | **Code Complete** (foundation) |
| P21-WP05 | PIA framework | **Code Complete** (foundation views) |
| P21-WP06 | Evidence / phase traceability | **Code Complete** (foundation) |
| P21-WP07 | DPO / NPC readiness | **Code Complete** (tracked items; not registered) |
| P21-WP08 | PDF / export | **Code Complete** (QuestPDF; DRAFT unless Approved) |
| P21-WP09 | Gap assessment | **Code Complete** (overview gaps) |
| P21-WP10 | Security / tests / readiness review | **Code Complete** (foundation tests) |

## Authorization

| Permission | Code |
|---|---|
| View | `platform.permission.view_privacy_compliance` |
| Manage | `platform.permission.manage_privacy_compliance` |

Platform Administrator: View + Manage. Platform Auditor: View only. Personal / Organization / POS users: **blocked**.

## Routes (Platform Admin)

```text
/admin/privacy-compliance
/admin/privacy-compliance/documents
/admin/privacy-compliance/systems
/admin/privacy-compliance/pias
/admin/privacy-compliance/data-inventory
/admin/privacy-compliance/retention
/admin/privacy-compliance/incidents
/admin/privacy-compliance/vendors
/admin/privacy-compliance/dpo-npc
/admin/privacy-compliance/evidence
```

API: `/api/v1/platform/privacy-compliance/*`

## Privacy Impact standing rule

Every future phase/work package that introduces or changes personal-data processing must include a **Privacy Impact** section (see [security.md](../engineering/security.md)).

## Explicit exclusions

- No fabricated NPC submission dates, registrations, or approvals
- No “percentage compliant” score pretending legal certification
- No Personal/Org/POS exposure of this workspace
- No POS tenant DB coupling; Platform DB only
- No secrets/tokens/raw customer records in PDF exports

## Reports

| Artifact | Link |
|---|---|
| Foundation | [P21-foundation-privacy-compliance-workspace](../reports/P21-foundation-privacy-compliance-workspace.md) |
| WP01 inventory | [P21-WP01-requirements-and-privacy-inventory](../reports/P21-WP01-requirements-and-privacy-inventory.md) |
