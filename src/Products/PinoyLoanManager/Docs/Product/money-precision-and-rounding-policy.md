# Pinoy Loan Manager — Money Precision and Rounding Policy

**Status:** Accepted product policy (PLM-DOC-02); **PLM-D-00-12 Closed**; not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Authoritative money arithmetic, PHP posting scale, intermediate precision, midpoint rounding, and schedule reconciliation. Not a database schema and not a legally validated disclosure rounding rule.

**Canonical companions:** [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md), [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md). ADR: [../Decisions/ADR-004-rounding-fees-and-payment-allocation.md](../Decisions/ADR-004-rounding-fees-and-payment-allocation.md).

---

## Authoritative money arithmetic

- decimal-based
- never binary floating point

Initial currency: **PHP**.

Posted / display currency scale: **2 decimal places**.

Intermediate calculation precision: **at least 8 decimal places** where the formula requires it.

Do **not** round every intermediate multiplication/division prematurely.

Round only at documented calculation/posting boundaries.

Future database implementation should use an appropriate fixed-precision numeric/decimal type. **Do not create the database in this package.**

Do not silently use different rounding methods across Web, API, MAUI, database calculations, reports, or receipts.

---

## Midpoint rounding (PLM-D-00-12 Closed)

MVP midpoint rounding: **To Even**.

Equivalent .NET intent: `MidpointRounding.ToEven`.

Rationale:

- deterministic
- minimizes systematic aggregate rounding bias
- reproducible across schedules and reports

This closes **PLM-D-00-12**.

Legal/accounting review of consumer presentation remains **PLM-D-00-11**.

---

## Schedule reconciliation

Every generated schedule must satisfy:

```text
Sum of scheduled principal = Contract Principal
```

For flat/add-on **added-interest** Loans:

```text
Sum of scheduled finance charge = Total Finance Charge
```

For all Loans:

```text
Sum of all scheduled components = Total Scheduled Repayment
```

Rounding residual must not disappear.

Deterministic **final-installment** reconciliation:

- ordinary installments use the standard rounded component values
- final applicable installment receives the remaining centavo residual
- final principal component must reduce scheduled principal to exactly zero

The final installment may differ by a small rounding residual. That difference must be explainable.

For deducted-interest Loans, the deducted finance charge is **not** scheduled as unpaid interest; schedule totals still reconcile to Total Scheduled Repayment as defined in [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md).

---

## Versioning

Rounding policy and version are snapshotted with each submitted request/application and resulting Loan. Future engine changes must not silently alter historical Loans.

---

## Legal / compliance boundary

No rounding or presentation rule in this document is claimed legally sufficient for consumer disclosure. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Database column types / migrations
- Multi-currency implementation
- Legal EIR/APR rounding algorithm
- Implementation of a calculation engine
