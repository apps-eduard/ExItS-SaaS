# P4-WP03 — Subscriptions, Payments and Trials

## 1. Status

**Complete.** Platform Admin subscription lifecycle, trial start, and manual SaaS payment workflows delivered by reusing Phase 3 domain/application/API behavior. No new persistence migration. HealthCare remains frozen. Authentication, payment gateways, invoices, entitlement delivery, and product-local roles remain out of scope.

| Field | Value |
|---|---|
| Phase | Phase 4 — Platform Admin Expansion |
| Work package | P4-WP03 — Subscriptions, Payments and Trials |
| Branch | `main` |
| Date | 2026-07-30 |
| Phase marker | `P4-WP03-subscriptions-payments-trials` |

## 2. Delivered capability

### Subscription and trial administration

Admin workflows reuse existing Platform application use cases:

- Start organization trial (configured trial-definition duration; no fixed 90-day behavior)
- View subscription detail/history and commercial metadata
- Activate, enter grace, mark past due, suspend, reactivate, cancel, expire
- State-gated lifecycle actions with confirmation for access-reducing changes
- Duplicate active-like trial/subscription conflicts surface as ProblemDetails
- Terminal states (`Cancelled`, `Expired`) remain blocked for further lifecycle mutations
- Warnings: eligibility ≠ product provisioning; no product-local role assignment

### Manual SaaS payment administration

Methods: Cash, BankTransfer, GCash only.

- Create manual payment (`PendingConfirmation`)
- Confirm / Reject / Void
- Confirm payment and activate eligible subscription atomically
- Duplicate normalized external references blocked
- Confirmed payment reuse blocked
- Explicit copy: manual confirmation is **not** provider verification; no card/CVV/PIN/OTP/gateway secrets

### Commercial access impact

Subscription state continues to drive fail-closed effective commercial access (Trialing/Active only). P4-WP03 does not assign product-local roles or deliver entitlements.

## 3. Persistence / migration

**No new migration.** Reused Phase 3 subscription and SaaS payment tables/repositories. No authentication, invoice, gateway, POS, or HealthCare schema changes.

## 4. API

Existing Phase 3 subscription and payment mutation endpoints reused. Admin typed client calls:

| Workflow | Route pattern |
|---|---|
| Start trial | `POST .../organizations/{id}/subscriptions/trials` |
| Lifecycle | `POST .../subscriptions/{id}/activate\|grace-period\|past-due\|suspend\|reactivate\|cancel\|expire` |
| Create payment | `POST .../payments/manual` |
| Confirm/reject/void | `POST .../payments/{id}/confirm\|reject\|void` |
| Confirm + activate | `POST .../payments/{id}/activate-subscription` |

DTOs only; stable ProblemDetails; concurrency conflicts remain explicit. Payment activate response maps full subscription DTO for Admin deserialization. No gateway, webhook, invoice, QR, card, POS, or HealthCare APIs added.

## 5. Platform Admin UI

| Route | Purpose |
|---|---|
| `/admin/subscriptions`, `/admin/subscriptions/{id}` | List/filter, start trial, state-gated lifecycle, ConfirmDialog |
| `/admin/payments`, `/admin/payments/{id}` | Create manual payment, confirm/reject/void, confirm-and-activate |
| Organization panel links | Jump into trial/payment workflows |

Preserves compact responsive native CSS, loading/empty/error states, ProblemDetails-aware errors, toasts, reduced-motion, and development-stage security banners.

## 6. Explicit exclusions

- Authentication, JWT, MFA, SSO, Active Directory, production authorization
- Payment gateway, webhooks, QR, card processing, invoice engine, automatic verification
- Entitlement delivery / product provisioning
- Product-local roles/permissions
- POS payments / Utang repayments mixed into Platform SaaS payments
- Fixed 90-day trial; R-035 calendar-month EOM still open
- Automatic repeat-trial approval rules
- P4-WP04

## 7. Test evidence

| Suite | Passed |
|---|---:|
| Unit | 210 |
| Architecture | 39 |
| Admin unit | 16 |
| Integration | 71 |
| **Total** | **336** |

Baseline was 331; not reduced. Added Admin client route/conflict coverage and architecture guards for lifecycle/payment controls without gateway/card fields.

Focused commercial API integration evidence: `ApiSubscriptionLifecycleTests` + `ApiSaaSPaymentTests` (12 passed).

## 8. Runtime validation

- API `/` → `phase=P4-WP03-subscriptions-payments-trials`; `/health` → Healthy
- Admin `/admin/subscriptions` → 200 with Start trial
- Admin `/admin/payments` → 200 with Create manual payment and GCash (not ManualGCash)
- Integration lifecycle: trial → duplicate blocked → activate → grace → past due → suspend → reactivate → cancel/expire terminal blocked
- Integration payments: create → confirm/reject/void → confirm-and-activate → reuse conflict → duplicate reference 409

## 9. Authentication and production limitations

APIs and Admin remain **development-stage and unauthenticated**. Manual confirmation is operator judgment, not provider verification. Subscription Admin changes are not completed product provisioning.

## 10. HealthCare freeze

`/HealthCare/` remains ignored, untracked, outside `ExItS.slnx`, unchanged.

## 11. Exact next work package

**P4-WP04 — Audit, Authorization and Closeout**

Do not begin until explicitly authorized.

## 12. Commits

| Kind | Message / hash |
|---|---|
| Feature | `feat(admin): manage subscriptions payments and trials` — `91e88c3216ab400149339fa43f519fbe59551314` |
| Docs | `docs(admin): record P4-WP03 commit hashes` — `653129e` |
