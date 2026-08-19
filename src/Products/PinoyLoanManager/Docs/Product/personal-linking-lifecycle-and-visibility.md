# Pinoy Loan Manager — Personal Linking Lifecycle and Visibility

**Status:** Accepted product-owner rules (PLM-DOC-01). Not implemented.
**Implementation present:** No
**Last updated:** 2026-08-19

Canonical linking, consent, unlink, and visibility rules. Cardinality and duplicates: [borrower-identity-and-duplicate-policy.md](borrower-identity-and-duplicate-policy.md). ADR: [../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md](../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md).

**PLM-D-00-04** and **PLM-D-00-05** remain **open** for Platform contract, schema, transport, persistence, and integration mechanism. Do not design APIs or tables here.

---

## EX ID / QR safety

Hard rules:

- EX ID / QR resolution identifies a Personal identity **only**
- resolution **never** creates a relationship
- resolution **never** creates a Borrower
- resolution **never** creates a Loan
- resolution **never** publishes an offer
- explicit Personal consent is required
- organization confirmation is required
- no automatic matching from POS Customer records
- no direct Platform table reads
- no direct POS table reads

The identity preview must expose only the **minimum** information required to avoid linking the wrong person. Preview fields are **not** finalized.

---

## MVP link initiation

An **authorized PLM organization user** initiates the link request.

Examples of authorized presets according to **future grants** (identifiers still open — PLM-D-00-06): Owner, Manager.

The organization user:

1. opens an existing PLM Borrower
2. enters or scans the Personal EX ID / QR
3. PLM resolves a minimal Personal identity preview through a **future approved Platform contract**
4. the organization user confirms the intended Borrower
5. PLM sends a consent request to ExItS Personal
6. the Personal user accepts or declines

**Personal self-service claiming** of a lender Borrower record is **future work**. It is **not** authorized in MVP.

Do not design the Platform API or schema yet.

---

## Conceptual lifecycle

Not final code enum names:

```text
Unlinked
  → Link Requested
  → Pending Personal Consent
  → Linked
```

Alternative outcomes:

```text
Pending Personal Consent → Declined
Pending Personal Consent → Expired
Linked → Consent Revoked
Linked → Organization Suspended
```

After revocation or suspension, a **new** request may lead to Linked again:

```text
Consent Revoked / Organization Suspended
  → New Link Request
  → Linked
```

Every transition must eventually be **auditable**.

Numeric expiration period is **not** chosen in this package.

---

## Decline and expiry

If the Personal user **declines**:

- the Borrower remains
- no active Personal link is created
- no Loan data is deleted
- no negative credit meaning is inferred automatically
- the declined request remains auditable
- a future new request may be allowed according to organization policy

If the link request **expires**:

- the Borrower remains unlinked
- no automatic link occurs
- a new request is required

---

## Unlinking / consent revocation

A Personal user must be able to **revoke** the Personal relationship.

An authorized organization user may **suspend** or request unlinking for reasons such as:

- incorrect identity link
- fraud concern
- account investigation
- duplicate Borrower correction
- organization policy
- other documented reason

Unlinking or suspension must **NOT**:

- delete Borrower
- delete Loan
- delete application/request
- delete payment history
- change balances
- change schedules
- erase receipts
- erase audit history
- rewrite contractual financial records

---

## Effect of unlinking

After a Personal relationship is revoked or suspended:

**Immediately block:**

- new Quick Loan offers delivered through Personal
- new Personal-originated Quick Loan requests
- new Personal relationship-dependent actions

**Keep in PLM:**

- Borrower
- existing applications
- approved Loans
- active Loans
- settled Loans
- payments
- receipts
- financial history

**Personal visibility:**

- submitted requests and active contractual obligations must not disappear silently
- historical and active Loan visibility should remain available where the applicable contract, privacy basis, and legal review permit it
- exact legal retention and visibility obligations remain **open** under **PLM-D-00-11**

Do **not** claim a legal basis in this package.

---

## Relinking

Relinking to the **same** verified Personal identity requires:

- a new link request
- new Personal consent
- new audit history

Changing a Borrower from one Personal identity to a **different** Personal identity is a **high-risk identity correction**. It must eventually require:

- authorized Owner/Manager grant
- reason
- evidence or verification
- audit
- preservation of previous link history

Exact two-person approval remains **open** under **PLM-D-00-13**.

Do **not** silently replace the Personal identity reference.

---

## Personal data minimization

PLM should receive only the Personal identity information necessary for:

- identity resolution
- consent
- active relationship reference
- customer-facing Loan presentation
- approved notifications

PLM must **not** receive unrelated Personal activity, including:

- unrelated POS purchase history
- another lender’s Borrower data
- another organization’s Loans
- unrelated product activity
- unrestricted Personal profile data

One lending Organization must **never** see another lender’s relationship or operational data.

---

## Quick Loan visibility

A Quick Loan Template may be published through Personal only to:

- linked eligible Borrowers of that Organization
- an eligible Borrower Group
- selected eligible linked Borrowers

An **unlinked** Borrower may still exist and may still use organization-operated traditional workflows, but **cannot** receive a Personal-delivered offer until linked.

Revoking the link stops **future** Personal-delivered offers.

Publishing never creates a Loan. Eligibility never equals approval.

---

## Legal / compliance

No consent, retention, or visibility rule in this document is claimed legally compliant (**PLM-D-00-11**).
