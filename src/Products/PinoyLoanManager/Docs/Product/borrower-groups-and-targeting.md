# Pinoy Loan Manager — Borrower Groups and Targeting

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Organization-owned Borrower Groups for Quick Loan publishing and operational targeting. Not a mandatory taxonomy or rule engine.

Related: [quick-loan-publishing-and-eligibility.md](quick-loan-publishing-and-eligibility.md), [borrower-model.md](borrower-model.md), [quick-loan-model.md](quick-loan-model.md).

---

## Concept

Support **organization-owned** Borrower Groups.

Do **not** create built-in mandatory groups.

Examples only (not product types):

- Good Payers
- Employees
- VIP
- Area / Route
- Promotional Group

---

## Maintenance

Groups may be:

- **manually maintained** (planning baseline)
- **future rule-derived** (not designed; do not implement dynamic rules yet)

Membership does not replace grants or assignment scope. A Collector still sees assigned work, not every member of a group, unless granted and assigned.

---

## Use with publishing

A Quick Loan Template may be published to a Borrower Group. Publishing still does not create a Loan. Eligibility still applies to each borrower. See [quick-loan-publishing-and-eligibility.md](quick-loan-publishing-and-eligibility.md).

---

## Explicit non-goals

- Platform-wide groups across organizations
- Mandatory built-in group names
- Dynamic rule engine
- Using groups to bypass authorization scope
