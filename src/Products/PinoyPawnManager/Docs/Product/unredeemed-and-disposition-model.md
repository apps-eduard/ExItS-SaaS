# Unredeemed and Disposition Model

> Index: [README.md](README.md)  
> Maturity: [maturity-model.md](maturity-model.md)  
> POS boundary: [../Architecture/pos-commerce-boundary.md](../Architecture/pos-commerce-boundary.md) (when present)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

After maturity, a ticket may become **operationally unredeemed** and later enter a **disposition** workflow inside PPM. These are **product/ops states**, not automatic proof of legal ownership transfer or auction authority.

**LEGAL_AUTHORIZATION_CLAIMED=NO.** Do not invent PH auction schedules, forfeiture rules, or surplus/deficit formulas.

---

## Operational vs legal

| Layer | Meaning |
|---|---|
| **Operational** | PPM states `MATURED` → `UNREDEEMED` → `DISPOSITION_PENDING` → `CLOSED` for work management |
| **Legal** | Whether the shop may sell, melt, or keep surplus — **external compliance** ([PPM-D-00-14](../risks-and-decisions.md), [PPM-D-00-20](../risks-and-decisions.md)) |

```text
Technical eligibility to open disposition UI
        ≠
Legal authorization to dispose / auction / transfer ownership
```

Software may track workflow steps; it must not claim the org is licensed or that a button click satisfies statute.

---

## Path overview

```text
MATURED
→ (policy window Open — PPM-D-00-10) operational UNREDEEMED
→ staff starts disposition → DISPOSITION_PENDING
→ custody: DISPOSITION_PENDING / TRANSFERRED_FOR_DISPOSITION
→ optional Commerce/POS handoff contract (PPM-D-00-15)
→ CLOSED
```

Late redeem/renew while still allowed by policy remains possible until disposition locks the item — exact lock point is Open.

---

## `UNREDEEMED` (operational)

| Aspect | Planning rule |
|---|---|
| Entry | Ops classification after maturity without successful redeem/renew; **threshold Open** |
| Custody | Item still PPM-held; **not** retail stock |
| Money | May still compute a redeem quote if policy allows; no invented forfeiture posting |
| Forbidden | Auto-create POS inventory row |

---

## Disposition workflow (machine D — planning)

Conceptual stages (names refinable):

1. **Eligibility review** — checklist that ops conditions met (not a legal opinion engine)  
2. **Authorization** — staff/capability gated; possibly dual control Open  
3. **Disposition method selection** — auction / private sale / other — **methods Open**; no PH-prescribed menu claimed  
4. **Custody transition** — prepare item for disposition path ([../Custody/custody-state-model.md](../Custody/custody-state-model.md))  
5. **Handoff or completion** — either remain tracked in PPM until sold externally, or **explicit** transfer event toward Commerce  
6. **Close ticket** — `CLOSED` with disposition outcome references  

Proceeds, deficiencies, and customer surplus handling are **accounting/compliance Open** — record hooks only when implementing, without fake legal math.

---

## POS / Commerce handoff boundary

| Rule | Intent |
|---|---|
| No direct POS DB writes from PPM | Required |
| No cross-product FKs | Required |
| Handoff is an explicit contract later | [PPM-D-00-15](../risks-and-decisions.md) |
| While pledged / unredeemed pre-handoff | Not normal POS on-hand inventory |
| After authorized handoff | Commerce owns retail listing/sale; PPM retains historical pawn/custody evidence |

```text
PPM pledged item (custody)
        │  explicit handoff event (future)
        ▼
POS / Commerce inventory (retail)
```

Until the handoff ADR exists, agents document the boundary only — **no handoff implementation** in PPM-00.

---

## Custody states during disposition

Typical alignment (refine as needed):

| Machine A | Machine B |
|---|---|
| `UNREDEEMED` | `IN_CUSTODY` |
| `DISPOSITION_PENDING` | `DISPOSITION_PENDING` |
| Approaching close via sale channel | `TRANSFERRED_FOR_DISPOSITION` |
| `CLOSED` | Terminal transferred / disposed record; history kept |

See [../Custody/custody-state-model.md](../Custody/custody-state-model.md).

---

## Prohibited behaviors

| Prohibited | Why |
|---|---|
| Maturity job auto-writes POS stock | Ownership/custody failure |
| Silent delete of intake photos when disposing | Evidence retention ([PPM-D-00-19](../risks-and-decisions.md)) |
| UI label “Auction authorized by law” without compliance close | False compliance ([PPM-R-00-09](../risks-and-decisions.md)) |
| Cross-org disposition | Isolation |

---

## Online-only

Starting disposition, authorizing steps, and handoff events are **ONLINE-ONLY** mutations on initial Web/PWA.

---

## Related decisions

| ID | Topic |
|---|---|
| [PPM-D-00-10](../risks-and-decisions.md) | Grace / default process |
| [PPM-D-00-14](../risks-and-decisions.md) | Disposition / auction model |
| [PPM-D-00-15](../risks-and-decisions.md) | POS handoff |
| [PPM-D-00-20](../risks-and-decisions.md) | Regulatory prerequisites |

---

## Exclusions

- No auction engine  
- No statutory surplus calculator  
- No claim ExItS authorizes pawnshop disposal under PH law  
