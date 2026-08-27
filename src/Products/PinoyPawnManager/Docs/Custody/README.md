# Pinoy Pawn Manager — Custody Domain Docs

> Parent index: [../README.md](../README.md)  
> Product companion: [../Product/README.md](../Product/README.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Folder | `Docs/Custody/` |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

This folder defines **physical control** of pledged items: custody states (machine B), storage locations, movements, customer release, and loss/damage/discrepancy handling.

Documentation only. Names may be refined at implementation; the requirements **payment ≠ release** and **custody history required** must not be refined away.

---

## Permanent principles (Custody)

| Principle | Value |
|---|---|
| PPM owns custody while pledged | YES |
| Current location alone sufficient | **NO** — history required |
| Payment alone releases item | **NO** |
| Pledged item is POS inventory while pledged | **NO** |
| Cross-branch move | Explicit only ([PPM-D-00-16](../risks-and-decisions.md)) |
| Mandatory biometrics for release | **NO** (foundation) |
| Silent edit of intake evidence | **Forbidden** |
| Web/PWA custody mutations (initial) | **ONLINE-ONLY** |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

---

## Document index

| Doc | Purpose |
|---|---|
| [custody-state-model.md](custody-state-model.md) | Machine B states and alignment with ticket machine A |
| [storage-location-model.md](storage-location-model.md) | Org→Branch→Vault→… hierarchy; MVP pragmatism |
| [custody-movement.md](custody-movement.md) | Movement audit fields and rules |
| [item-release.md](item-release.md) | Wrong-item safeguards; release checklist |
| [loss-damage-discrepancy.md](loss-damage-discrepancy.md) | Incidents; evidence integrity |

---

## How custody relates to Product

| Concern | Doc |
|---|---|
| Ticket lifecycle | [../Product/pawn-transaction-model.md](../Product/pawn-transaction-model.md) |
| Redemption payment then release | [../Product/redemption-model.md](../Product/redemption-model.md) |
| Disposition handoff | [../Product/unredeemed-and-disposition-model.md](../Product/unredeemed-and-disposition-model.md) |
| Pledged item identity | [../Product/pledged-item-model.md](../Product/pledged-item-model.md) |

```text
Machine A (obligation)  ≠  Machine B (physical control)
Payment (C)             ≠  Release (B)
```

---

## MVP posture

Avoid overbuilding vault taxonomy before first operations ([PPM-R-00-10](../risks-and-decisions.md)). Prefer a usable Branch → StorageArea → Bin/Bag path that can deepen later. See [storage-location-model.md](storage-location-model.md).

---

## Open decisions touching custody

| ID | Topic |
|---|---|
| [PPM-D-00-13](../risks-and-decisions.md) | Representative redemption/release |
| [PPM-D-00-14](../risks-and-decisions.md) | Disposition model |
| [PPM-D-00-15](../risks-and-decisions.md) | POS handoff |
| [PPM-D-00-16](../risks-and-decisions.md) | Cross-branch transfer |
| [PPM-D-00-19](../risks-and-decisions.md) | Evidence retention |

---

## Exclusions (PPM-00)

- No custody tables, scanners, or label printers implemented  
- No claim of vault insurance or regulatory custody standards met  
