# ADR-013 — Borrower onboarding and application minimums

**Status:** Accepted product policy (PLM-DOC-07); not implemented
**Date:** 2026-08-19

---

## Context

PLM needed Product Owner minimums for Borrower profile, Traditional Application fields, and Quick Loan Request fields. Prior docs listed KYC fields and application minimums as open.

---

## Decision

1. MVP supports **natural-person Borrowers** only; business/entity Borrowers deferred.
2. Minimum Borrower profile: legal name, DOB, primary contact, primary address, identity-verification status/method, organization, branch where applicable, status, privacy acknowledgment, audit metadata.
3. Optional/configurable fields documented with data minimization by role.
4. Conceptual Borrower statuses: Prospect, Active, Suspended, Blocked, Inactive, Deceased, Archived.
5. Traditional Application minimum fields documented; collateral not mandatory for MVP.
6. Quick Loan Request minimum: linked eligible relationship, published template, amount in range, calculation, acknowledgment, snapshots. Eligibility ≠ approval.

Canonical: [../Product/borrower-onboarding-and-verification-policy.md](../Product/borrower-onboarding-and-verification-policy.md), [../Product/traditional-application-and-assessment-policy.md](../Product/traditional-application-and-assessment-policy.md), [../Product/quick-loan-eligibility-and-approval-policy.md](../Product/quick-loan-eligibility-and-approval-policy.md).

---

## Consequences

MVP Borrower and application minimums are approved for planning.

**Still open:** legal/KYC sufficiency (PLM-D-00-11), schema, implementation.
