# Data Ownership and Authority

[Architecture summary](approved-architecture-summary.md) | [Capability boundary](platform-product-capability-boundary.md) | [Contracts](platform-product-contracts.md)

**Version:** 2.0  
**Status:** Authoritative  
**Current phase:** Phase 16 — P16-WP11 validation  
**Last reconciled:** 2026-08-03

---

## 1. Permanent rule

> Platform owns commercial and global identity truth. Products own operational truth.

No cross-database foreign keys and no shared DbContext across bounded contexts.

---

## 2. Platform database ownership

The Platform database owns:

- User Identity
- credentials and verification
- Account Profiles: Platform, Personal, Organization
- sessions, refresh tokens, revocation, security stamps
- Organizations
- Organization memberships and Organization roles
- Product catalog
- Plans
- Plan prices and limits
- trial policy
- Organization Subscriptions
- agreed-price snapshots
- SaaS Payments
- Plan-change requests
- Product Entitlements
- entitlement revisions and overrides
- Product provisioning intent/status metadata
- Platform roles and permissions
- Platform audit and security events
- Local Validation configuration metadata, never Production secrets

---

## 3. Personal data ownership

Personal scope owns:

- Personal settings
- Personal contacts
- Personal Utang records
- Personal reminders
- Personal relationship invitations
- Personal notification preferences
- migration source metadata

Personal data is not automatically copied, merged, or synchronized with Organization data.

---

## 4. Organization data ownership

Platform/Organization boundary owns:

- Organization profile and status
- membership
- Organization Owner/Staff role
- Subscription selection
- billing-cycle choice
- Plan-change request
- Organization-level settings that are not Product operational settings
- Organization audit references

Product operational Organization data remains in the Product database.

---

## 5. Pinoy Business POS database ownership

Authoritative location:

```text
Database: ExItS_PinoyBusinessPOS
Schema: pos
```

The POS database owns:

- POS business workspace
- branches
- stores
- registers
- terminals/devices
- Product-local staff profiles where required
- POS Product-role assignments and Product permissions
- customers
- catalog, categories, SKU, barcode
- inventory and stock movements
- sales and sale lines
- returns and voids
- retail payments
- cash sessions
- Customer Credit / Utang
- credit repayments and reversals
- expenses
- purchasing and suppliers
- reports
- offline local/sync metadata
- Product-local audit

---

## 6. Payment separation

| Payment type | Owner | Example |
|---|---|---|
| SaaS Payment | Platform | Organization pays ExITS for Business Plan |
| POS Retail Payment | POS | customer pays store for a sale |
| POS Credit Repayment | POS | customer pays existing Utang balance |

These must not share entities or tables.

`LocalValidationPaymentProvider` applies only to Platform SaaS Payments.

---

## 7. Cross-boundary references

Products may store stable value references:

```text
PlatformUserId
PlatformOrganizationId
OrganizationMembershipId
SubscriptionId
ProductKey
PlanKey
EntitlementRevision
CorrelationId
```

These are not database foreign keys to Platform tables.

---

## 8. Projections

A POS commercial projection may store:

- Organization identifier and display name
- Subscription ID/status
- Plan key/version
- Entitlement revision
- feature grants and limits
- trial/grace/expiry facts
- effective and refresh timestamps
- source event/version

Projection rules:

- versioned
- idempotent
- additive where possible
- no authority to change Platform commercial state
- no unnecessary Personal data
- no payment secrets
- no Platform credentials

---

## 9. Start a Business transaction ownership

Platform orchestration creates:

- Organization Account Profile
- Organization
- Owner membership
- Subscription
- Entitlement
- provisioning request
- Product-owner bootstrap request

Product provisioning creates:

- Organization Product Instance
- Product-owned workspace
- explicit Product-local POS Owner assignment

The Personal Account and Personal data remain unchanged unless a separate migration operation is explicitly approved.

---

## 10. Migration ownership

Personal-to-business migration is:

- source read from Personal ownership
- destination write to the selected Organization/Product
- explicit and selective
- previewed
- idempotent
- auditable
- protected against duplicates
- protected against cross-Organization import
- consent-aware for linked participants

After migration, the Organization Product owns the created destination operational records.

No automatic two-way synchronization is allowed.

---

## 11. Retention and destructive behavior

Subscription expiry, cancellation, suspension, downgrade, or payment failure must not automatically delete:

- Organization
- memberships
- Product Instance
- Product operational data
- Product-role history
- billing history
- audit records

Downgrade retains over-limit data and blocks new over-limit creation according to policy.

Local Validation reset is an explicit environment-only operation and must fail closed outside Local Validation.
