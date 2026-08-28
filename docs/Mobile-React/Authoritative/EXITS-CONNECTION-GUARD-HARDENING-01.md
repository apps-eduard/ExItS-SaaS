# EXITS-CONNECTION-GUARD-HARDENING-01 — Connection invitation guards

## Rule

| Layer | Role |
|-------|------|
| **Frontend** | Prevent predictable invalid actions; explain why; fail closed when eligibility unknown |
| **Backend** | Authoritative security and data integrity |

## Matrix

| Flow | Self | Already active | Pending duplicate | Block |
|------|------|----------------|-------------------|-------|
| Personal → Personal | Reject | Reject | Reject | Reject |
| Organization → Organization (supplier) | Reject | Reject | Reject | (N/A / product rules) |
| Organization → Personal **customer** | Owner self reject | Same org + Personal active link reject | Same org + Personal pending reject (any customer) | Neutral unavailable |
| Organization → Staff invite | Owner self reject | Existing staff reject | Pending invite reject | (separate package) |

## Organization → Personal customer (this package)

Eligibility statuses (single service `EvaluateCustomerLinkEligibility`):

- `Eligible`
- `OwnerOfOrganization`
- `OrganizationStaff` (via `LinkedPersonalUserId`, not email)
- `AlreadyLinked`
- `PendingInvitation`
- `BlockedOrUnavailable`
- `InvalidTarget`

Preflight: `POST /api/v1/organizations/{orgId}/customers/link-eligibility`  
Create path re-runs the same evaluator.

Concurrency: unique filtered index `ux_customer_link_requests_pending_org_target`.

Customer link acceptance still creates **no** Organization membership / staff / POS role.

## Non-goals / deferred

- Local walk-in name duplicate warning: **DEFERRED**
- No public “is this EX-ID an employee?” directory
- No email enumeration
