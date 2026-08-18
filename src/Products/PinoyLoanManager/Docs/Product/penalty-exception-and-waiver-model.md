# Pinoy Loan Manager — Penalty, Exception, and Waiver Model

**Status:** Agreed product direction (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Penalty policy is configurable per Loan Template / snapshotted Loan terms. Do **not** make one hard-coded global penalty. Do **not** choose an actual peso rate or percentage. This is not a legally validated collections policy.

Related: [lending-operating-model.md](lending-operating-model.md), [quick-loan-model.md](quick-loan-model.md), [payment-and-allocation-model.md](payment-and-allocation-model.md), [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md).

---

## Penalty configuration concepts

Support the concept of:

- grace allowance
- chargeable missed days
- excused missed days
- unexcused missed days
- penalty tiers
- fixed amount penalty
- percentage penalty
- explicit penalty basis
- penalty cap
- post-maturity policy
- waiver
- reversal
- collection exception

No Platform default amount, percentage, or “N days” is established.

---

## Missed collection classification

A scheduled collection / payment event may conceptually result in:

- Paid
- Partially Paid
- Excused Missed Day
- Unexcused Missed Day
- Pending Exception Review

### Potential excused reasons (examples)

- severe weather
- flooding
- declared emergency
- organization collection suspension
- collector / service failure
- organization closure
- manager-approved exceptional circumstance

**Customer simply being unavailable is not automatically excused.**

### Possible unexcused reasons (examples)

- customer unavailable
- customer refused
- insufficient funds
- customer avoided collection

Collector may **record** the reason. Collector must **not** unilaterally approve a penalty waiver where approval is required.

---

## Allowable missed days / penalty tiers

Support configurable logic such as:

- Allowed unexcused / grace days: **N** (organization-configured; not a Platform default)
- Penalty may begin after the configured allowance

Support **tier** concepts such as (example of a configuration engine only):

- Chargeable missed days 1–5 → configured rule
- Chargeable missed days 6–10 → another configured rule
- 11+ → another configured rule

Do **not** establish 5 days or any amount as a Platform default.

Penalty basis must be **explicit**. Possible future options (not selected):

- missed installment
- total past-due amount
- outstanding principal
- fixed amount

Do **not** select the legal/business default yet.

---

## Collection exception vs waiver vs reversal

Keep these separate.

| Concept | Meaning |
|---|---|
| **Collection exception** | A qualifying event **prevents penalty assessment**. Example: organization declares collection suspension due to flooding. Penalty may never be assessed for the affected event according to policy. |
| **Penalty waiver** | Penalty was **validly assessed**, but authorized staff forgives all or part of it. History remains visible. |
| **Penalty reversal** | Penalty was **incorrectly assessed** and is corrected through an auditable reversal. |

Never silently delete historical penalty records.

---

## Organization-wide exceptions

Owner / Manager may declare (future concept):

- collection suspension date
- branch / area affected
- collectors affected
- reason
- treatment of penalties
- treatment of schedule / maturity

Example: heavy rain / flooding.

Schedule-extension rules are **not** finalized.

The important distinction:

**organization / service-caused missed collection** must be distinguishable from **borrower-caused missed payment**.

---

## Maturity / post-maturity

A loan does not disappear when the original term ends.

If an amount remains at maturity:

```text
Active
  → Maturity reached
  → Matured / Past Due
```

The remaining obligation remains tracked.

Support a configurable post-maturity policy such as:

- post-maturity grace
- penalty behavior
- penalty basis
- penalty cap
- collection treatment

Do **not** invent an actual penalty rate.

No unlimited silent penalty growth.

**Penalty-on-penalty should be OFF** as the engineering-safe default unless explicitly authorized and legally validated later.

Penalty-cap support should exist.

Exact maturity statuses and treatment remain **Open / Product Owner Decision Required**.

---

## Legal / compliance boundary

No interest, fee, penalty, collection policy, disclosure, or workflow in this document is claimed to be legally compliant. External legal/compliance validation is required before Production use. This package does not invent Philippine lending regulations.

---

## Explicit non-goals

- Hard-coded global penalty
- Platform-default days, peso amounts, or percentages
- Silent deletion of penalty history
- Collector self-approval of waivers
- Final schedule-extension algorithm
- Penalty-on-penalty as a default
