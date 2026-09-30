# Personal Utang Tracking Domain and Business Boundary

## Status

**Authoritative product design specification.**

This document defines the free personal Utang Tracking feature, its relationship model, invitations, reminders, notifications, privacy boundaries, and its separation from organization customers, staff, POS, and other business features.

## Product Decision

Personal Utang is creator-owned bookkeeping with optional read-only sharing.
The creator is the sole financial writer; the connected counterparty receives a
mirrored read-only view.

Ledger ownership is per debt record, not per connected-person pair.
Connected users may independently maintain separate records involving each
other. Sharing provides a read-only view only. ExItS does not automatically
merge, reconcile, or synchronize separately owned ledgers.

Utang Tracking is the main entry point for mobile users.

A person can register and use Utang Tracking for free without creating or joining an organization.

The feature supports personal peer-to-peer debt tracking between ordinary users.

Examples:

- a coworker borrowed money
- a friend lent money
- a family member owes money
- a person wants to track several unpaid personal balances

The user does not need to own a store or subscribe to POS to use this feature.

## Core User Model

Every registered person is a global Platform User.

A Platform User can use Utang Tracking personally and may later:

- create an organization
- join an organization as staff
- become linked to a business customer record
- subscribe to or unlock a product such as POS

These are separate contexts and must not be merged automatically.

```text
Platform User
├── Personal Utang Tracking
├── Optional Organization Staff Membership
├── Optional Business Customer Link
└── Optional Product-local Role
```

## Personal Utang Tracking Context

Personal Utang Tracking is not an organization feature.

It belongs to the individual user.

A user can:

- create a personal contact
- record money lent
- record money borrowed
- create payment entries
- create adjustments
- set due dates
- create reminders
- send invitations
- link with another registered user
- receive notifications
- view shared balances and history after linking

## Relationship Roles

Do not create permanent global roles named Borrower or Lender.

The role exists only inside a specific debt relationship.

A user may be:

- lender in one relationship
- borrower in another relationship
- both at the same time across different records

Example:

```text
Eduard
├── Lender to Coworker A
├── Lender to Coworker B
└── Borrower from Friend C
```

Use relationship roles:

```text
Creditor / Lender
Debtor / Borrower
```

## Unlinked Contact Flow

A user must be able to track a debt even when the other person has not installed the app.

Example:

```text
User creates contact
→ User creates debt record
→ Contact remains unlinked
→ User records payments and reminders
```

The unlinked person is a personal contact only. They are not yet a Platform User in that relationship.

## Linked User Flow

When the other person installs the application:

```text
Lender creates invitation
→ Borrower downloads or opens app
→ Borrower verifies identity
→ Borrower accepts invitation
→ App links the registered user to the existing debt relationship
→ Both users can view the shared balance and transaction history
```

Linking must require explicit acceptance.

No user may be silently linked based only on a matching name, phone number, or email address.

## Invitation Lifecycle

Recommended invitation states:

```text
Pending
Accepted
Expired
Revoked
Declined
```

Rules:

- only authorized participants can send an invitation
- the recipient must explicitly accept
- expired, revoked, or declined invitations cannot create a link
- accepting an invitation must not create organization membership
- accepting an invitation must not grant product access
- all invitation changes should be auditable

## Shared Record Behavior

Connection consent and Utang sharing are **separate**. A connected contact does **not** automatically create a shared ledger.

**Core rule: creator = ledger owner.** Whoever creates "I Lent" or "I Borrowed" owns that debt record for its lifetime (`LedgerOwnerUserIdentityId`, immutable). There is **one ledger, one writer, two views** — never mirrored duplicate transactions.

Ownership is **per debt record**, not per connected-person pair. Paul ↔ Mica may have **0..N** independent `PersonalDebtRelationship` rows (same or opposite creditor/debtor direction). Creating a new debt never reuses another owner's relationship merely because the same users/contact/direction already exist. Client `RelationshipId` idempotency still converges retries of the same request.

Dashboard money totals (`TotalLentBalance` / `TotalBorrowedBalance`) count **My Records** only (viewer is ledger owner). Shared-with-me balances are separate informational fields and must not double-count into primary totals. Payments on one owner's ledger never alter another owner's ledger.

Creating a new I Lent / I Borrowed debt may optionally **Share with {person}** (default **OFF**). Sharing controls **visibility**, not financial authority.

### Private mode (Share OFF, or unlinked contact)

- Keep the counterparty as an **owner-private contact participant** — do **not** canonicalize a linked contact to a Platform user participant.
- Entries are **Confirmed** immediately and update the owner's `CurrentBalance`.
- Does **not** appear in the counterparty's I Lent / I Borrowed, dashboard, due totals, reminders, or notifications.
- Remains queueable offline where existing private Utang supports it.

Connecting / linking an ExItS ID must **not** silently promote historical private relationships into a shared ledger. Explicit Utang invitation accept may still authorize that specific relationship.

### Share ON = shared Personal↔Personal (owner-authoritative)

Share ON is **online required**. The server canonicalizes the linked contact to a user participant so both sides share **one** authoritative `PersonalDebtRelationship` (no dual ledgers / no duplicate financial entry). Perspective mirrors automatically (I Lent ↔ I Borrowed).

The **ledger owner** may add Loan / Payment / Adjustment, settle, close, and correct using append-only rules. The **counterparty** may view the mirrored perspective, balance, history, and due date; pause receiving future shares / notifications; and block/unlink. The counterparty must **not** mutate financial balance (server-enforced via `LedgerOwnerUserIdentityId`).

Recipient-owned preferences (keyed by recipient + connected counterparty; **not** global account settings):

| Preference | Default | Meaning |
|---|---|---|
| `ReceiveSharedUtang` | true | Show shared Utang from this person |
| `SharedUtangNotifications` | true | Notify about shared activity from them |

`AutoAcceptSharedUtang` is **obsolete** under the owner model (accepted in DTOs for compatibility but forced off / ignored). New owner-model entries are Confirmed immediately; there is no Confirm / Dispute / auto-accept workflow for new writes.

If `ReceiveSharedUtang` is **OFF**, Share ON falls back to **private** for the sender with a soft outcome (`PrivateNotReceiving`) — no notification and no counterparty visibility. Turning receive back ON affects **future** shared creates only.

Block / unlink overrides sharing.

### Shared entry authority (owner-model)

When both sides are linked Personal users (`IsSharedLinked`) on an owner-model relationship:

- Owner records Loan / Payment / Adjustment → **Confirmed immediately** → `CurrentBalance` updates immediately → counterparty sees the same single entry read-only
- Sharing does **not** create Pending for new entries
- Only **Confirmed** entries change relationship `CurrentBalance` (Loan +, Payment −, Adjustment ±)
- Legacy Pending / Disputed / Cancelled history remains identifiable; Pending / Disputed / Cancelled have zero effect on confirmed `CurrentBalance` (display totals may still include legacy Pending signed amounts while unresolved)
- Legacy Confirm / Dispute paths remain only for resolving historical Pending rows; they must not reopen two-writer behavior for new entries

Entry statuses: `Pending` | `Confirmed` | `Disputed` | `Cancelled`.

Recommended shared visibility:

- current balance (confirmed; plus legacy pending display where applicable)
- history (owner-confirmed entries; legacy pending / disputed if present)
- due dates
- participant identities and ledger-owner attribution ("Shared by {owner}")
- reminder status where appropriate

Recommended controls:

- only the ledger owner may mutate financial balance / lifecycle
- disputed legacy entries remain in history without balance effect
- confirmed historical amounts are not silently rewritten in place
- corrections use append-only adjustments by the ledger owner
- every meaningful change should be timestamped and attributable

### Legacy ledger-owner backfill

Existing relationships receive `LedgerOwnerUserIdentityId` from:

1. earliest financial entry `CreatedByUserIdentityId` (deterministic creator);
2. sole user participant when the other side is contact-only;
3. contact `OwnerUserIdentityId` when no user participant remains.

Do **not** guess owner among two user participants. Migration fails rather than assigning the wrong person when creator cannot be determined.

Anti-spam (shared Loan creation): preserve block checks, duplicate submission protection, rolling daily limit, and advisory locking. Unresolved-pending limit does **not** apply to new owner-model Confirmed writes (`skipUnresolvedPendingLimit`).
## Reminders and Notifications

The lender may create reminder rules such as:

- one-time reminder
- reminder on due date
- reminder before due date
- recurring overdue reminder
- custom reminder message

Example:

```text
Due date: 2026-08-15
Reminder: 3 days before
Message: Your payment is due soon.
```

Notification channels may include:

- in-app notification
- mobile push notification
- optional email
- optional SMS in a future phase

Rules:

- reminders must not expose sensitive debt details on a locked-screen notification by default
- users should be able to control notification preferences
- notification delivery failure must not change the debt balance
- reminders are communication events, not financial transactions
- repeated reminders should have rate limits and anti-harassment safeguards
- reminders and delivery attempts should be auditable

## Recommended Mobile Navigation

```text
Home

Utang
├── People
├── I Lent
├── I Borrowed
├── Invitations
├── Reminders
└── History

My Businesses
├── Organizations
├── POS
├── Customers
└── Staff
```

The personal Utang area must work without an organization.

## Recommended Domain Model

### User

Global login identity.

```text
User
- Id
- DisplayName
- Email
- Phone
- Status
```

### PersonalContact

A contact created by one user.

```text
PersonalContact
- Id
- OwnerUserId
- Name
- Phone optional
- Email optional
- LinkedUserId optional
- Status
```

### DebtRelationship

Represents the personal debt relationship.

```text
DebtRelationship
- Id
- CreditorUserId or CreditorContactId
- DebtorUserId or DebtorContactId
- Currency
- OriginalAmount
- CurrentBalance
- Status
- CreatedAt
- Version
```

At least one side must be the authenticated owner creating the record.

### DebtTransaction

Append-only financial activity.

```text
DebtTransaction
- Id
- DebtRelationshipId
- Type: Loan, Payment, Adjustment
- Amount
- TransactionDate
- Notes optional
- CreatedByUserId
- CreatedAt
```

### DebtInvitation

Links an existing relationship to another Platform User.

```text
DebtInvitation
- Id
- DebtRelationshipId
- InvitedByUserId
- InvitedEmailOrPhone
- RecipientUserId optional
- Status
- ExpiresAt
- AcceptedAt optional
```

### DebtReminder

Stores reminder scheduling.

```text
DebtReminder
- Id
- DebtRelationshipId
- CreatedByUserId
- DueDate
- ScheduleType
- Message
- Status
- NextDeliveryAt
```

### NotificationDelivery

Tracks delivery attempts.

```text
NotificationDelivery
- Id
- DebtReminderId
- RecipientUserId
- Channel
- Status
- AttemptedAt
- DeliveredAt optional
- FailureReason optional
```

## Personal and Business Separation

Personal Utang records and business customer-credit records are different domains.

```text
Personal Debt
≠
Organization Customer Credit
```

They may use the same Platform User identity, but ownership, authorization, reporting, and privacy remain separate.

### Personal debt

- owned by individual participants
- available without an organization
- peer-to-peer
- private to the linked participants
- not part of POS accounting
- not visible to organization staff unless explicitly introduced through a separate approved business workflow

### Organization customer credit

- owned by the organization
- connected to an organization customer record
- managed by authorized organization or product staff
- may integrate with POS
- may affect business reports
- remains isolated from unrelated personal debts

A personal debt must never be automatically converted into organization customer credit.

A business customer record must never expose the customer’s unrelated personal debts.

## Organization Terminology

For organizations, use:

```text
Organization Staff
Customers
Credit Customers
Linked App Users
Staff Invitations
Customer Link Requests
Staff Roles & Permissions
```

Definitions:

- **Organization Staff** — employees or workers who may receive organization or product-local roles
- **Customers** — people who have a business relationship with the organization
- **Credit Customers** — customers with organization-owned credit balances
- **Linked App Users** — Platform Users connected to a specific organization customer record
- **Staff Invitations** — invitations to join the organization as staff
- **Customer Link Requests** — requests to link a Platform User to a customer record

Do not call personal Utang users Organization Members.

Do not call organization customers Organization Staff.

## Authorization Boundaries

### Personal Utang permissions

Only debt participants and explicitly authorized services may access a personal debt relationship.

Recommended checks:

- authenticated user is creditor
- authenticated user is debtor
- authenticated user is an accepted linked participant
- operation is allowed for the user’s relationship role

### Organization permissions

Organization staff authorization is separate.

Organization roles must not grant access to a user’s unrelated personal Utang records.

### Product-local permissions

POS roles such as Owner, Manager, Cashier, and Viewer remain product-owned.

Personal Utang participation must never grant a POS role.

## Privacy and Safety Requirements

- no cross-user debt visibility
- no cross-organization debt visibility
- invitations require explicit acceptance
- ExItS public ID / QR may be used to find a person, but scanning never silently creates a contact, debt link, or acceptance — confirmation + existing invitation rules still apply ([public-user-id-and-qr](../specs/identity/public-user-id-and-qr.md))
- contact matching must not silently reveal account existence
- reminders require rate limiting
- block/report controls should be supported
- sensitive values should be minimized in notification previews
- audit significant changes
- preserve transaction history
- use optimistic concurrency for updates
- stale updates should return `409 Conflict`
- personal data export and deletion must follow applicable privacy rules
- account deletion must not corrupt shared financial history; anonymization or retained legal records may be required

## Product Funnel

Recommended product journey:

```text
Free registration
→ Personal Utang Tracking
→ Invite friends or coworkers
→ Linked shared balances
→ Reminders and notifications
→ User creates or joins a business
→ Business unlocks POS or other paid products
```

Utang Tracking is the acquisition feature.

POS and other products are optional later upgrades.

The application must not force organization creation during personal onboarding.

## Acceptance Criteria

This design is correctly implemented only when:

- a normal user can use Utang Tracking without an organization
- a user can track an unlinked person
- a linked user must explicitly accept an invitation
- both linked participants can see the shared debt and history
- a user can be lender and borrower in different relationships
- reminders and notifications work independently of financial calculations
- personal debts are isolated from organizations and POS
- organization roles cannot access unrelated personal debts
- personal Utang participation grants no organization or product-local role
- business customer-credit records remain organization-owned
- transaction and invitation history is retained
- authorization and privacy tests cover cross-user and cross-organization denial
