# Pinoy Pawn Manager — POS / Commerce Boundary

> Architecture index: [README.md](README.md)  
> Product definition: [../product-definition.md](../product-definition.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |
| Principles | `PPM_IS_POS_MODULE` = **NO**; `PAWN_ITEM_IS_NORMAL_POS_INVENTORY_WHILE_PLEDGED` = **NO** |

## Verdict

**Pledged items are not ordinary retail stock while pledged.**

PinoyBusinessPOS / Commerce owns normal retail product catalog, on-hand inventory, and checkout. PPM owns pledged-item records, custody, and disposition workflow **inside PPM** until an authorized handoff occurs.

## Ownership while pledged

| Concern | Owner while item is pledged |
|---|---|
| Pledged-item identity, photos, appraisal | **PPM** |
| Physical custody / vault location | **PPM** |
| Pawn obligation / ticket | **PPM** |
| Retail SKU / POS on-hand quantity | **Not applicable** — must not appear as sellable stock |
| Ordinary POS sale of the pledged item | **Forbidden** |

## Disposition → Commerce handoff (planning)

When (and only when) disposition is **operationally and legally eligible** (**PPM-D-00-14/20** Open), PPM may initiate a **handoff** so Commerce/POS can create retail inventory for sale.

| Rule | Intent |
|---|---|
| Handoff is explicit | No auto-transfer at maturity |
| Contract-based | Approved message/API contract; Guids + payload snapshots |
| No direct POS DB writes | `DIRECT_POS_DB_ACCESS` = **NO** |
| No silent inventory create | PPM must not invent POS stock rows |
| Audit both sides | PPM records handoff; Commerce records intake (when built) |
| Decision | **PPM-D-00-15** OPEN |

### Handoff contract sketch (not implemented)

Planning fields only—final schema deferred:

- PPM disposition / pledged-item identifiers  
- OrganizationId / BranchId  
- Identifying snapshot (category, description, serial/IMEI refs, photo evidence refs)  
- Appraisal / disposition authorization references  
- Idempotency key for handoff attempt  
- Commerce acknowledgment / retail inventory reference (returned later)

Exact payload, transport, and failure/retry rules close under **PPM-D-00-15** and later ADRs—not in PPM-00 code.

## Anti-patterns (forbidden)

| Anti-pattern | Why forbidden |
|---|---|
| Treating vault bins as POS stock locations | Wrong ownership; sellable vs pledged conflation |
| Maturity auto-creates POS inventory | Legal/ops risk; violates payment≠release / disposition rules |
| PPM EF navigating POS inventory entities | Cross-product DB violation |
| POS staff selling from PPM custody without handoff | Wrong-item / theft / compliance risk |

## Cash controls note

Cash drawer / till integration with POS is **Open** (**PPM-D-00-17**). Until decided, PPM records its own payment/release facts and does not copy POS drawer models.

## Risk reference

- **PPM-R-00-02** — Treating pledged items as POS inventory while pledged (mitigated in docs)

## Related

- [persistence-boundary.md](persistence-boundary.md)
- [api-contract-boundary.md](api-contract-boundary.md)
- [idempotency-and-reconciliation.md](idempotency-and-reconciliation.md)
- [../Custody/](../Custody/) (when present)
- [../risks-and-decisions.md](../risks-and-decisions.md)
