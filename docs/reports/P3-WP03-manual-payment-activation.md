# P3-WP03 — Manual Payment Activation

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 3 — Portfolio Billing, Plans and Entitlements |
| Work package | P3-WP03 — Manual Payment Activation |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Implemented persistent Platform SaaS manual payment records, confirmation lifecycle, duplicate-reference detection, subscription activation linkage, and void/reversal. Added EF migration `AddManualSaaSPayments`, repositories, use cases, queries, development-stage REST API, unit/architecture/integration tests, and isolated PostgreSQL migration validation.

**Manual confirmation is not automatic payment verification.** Platform does not call GCash, does not generate QR codes, does not receive webhooks, and does not store payment credentials.

**Security note:** Payment mutation endpoints are **development-stage and unauthenticated** (R-045 expanded). Actor references (`confirmedBy`, `rejectedBy`, `voidedBy`) accept plain strings — a production blocker.

## 3. Payment domain

| Item | Value |
|---|---|
| Aggregate | `SaaSPayment` — strongly typed `SaaSPaymentId` (Guid-backed, rejects Empty) |
| Methods | `SaaSPaymentMethod`: Cash, BankTransfer, GCash (manual recording only) |
| Statuses | PendingConfirmation → Confirmed → Voided; PendingConfirmation → Rejected |
| Currency | `CurrencyCode` value object (3-letter ISO, e.g. PHP, USD) |
| Amount | `decimal(18,4)`, positive-only (DB check constraint) |
| Reference | External reference + normalized form; duplicate detection scoped to method + org |
| Actor fields | `ConfirmedBy`, `RejectedBy`, `VoidedBy` — plain strings (no auth yet) |
| Immutability | Confirmed payments cannot be silently edited; void requires reason + actor |

## 4. Payment status lifecycle

| Transition | Rule |
|---|---|
| Create → PendingConfirmation | Default for all new manual payments |
| PendingConfirmation → Confirmed | Explicit confirm command with actor |
| PendingConfirmation → Rejected | Explicit reject with actor + reason |
| Confirmed → Voided | Explicit void with actor + reason |
| Rejected / Voided | Terminal — no further transitions |
| Confirmed → Confirmed | Blocked (409 already confirmed) |
| PendingConfirmation → Voided | Blocked (must confirm first) |

## 5. Subscription activation link

`ConfirmPaymentAndActivateSubscription` atomically:

1. Loads and confirms the payment (if PendingConfirmation)
2. Validates org/product match between payment and subscription
3. Activates the subscription (Trialing → Active) using existing lifecycle methods
4. Links `SubscriptionId` to the payment record
5. Prevents reuse — a linked payment cannot activate again

Does **not** duplicate subscription lifecycle logic. Uses existing `Subscription.ActivateFromTrial()`.

## 6. Duplicate reference detection

| Item | Value |
|---|---|
| Scope | `(method, normalized_reference, organization_id)` — provisional |
| Exclusion | Rejected and Voided payments excluded from uniqueness |
| Index | `ux_saas_payments_reference` partial unique index |
| Normalization | Trim, uppercase, collapse whitespace |
| Conflict | Returns 409 with `payment_reference_conflict` |
| Status | Provisional scope — documented as open decision |

## 7. Persistence

| Item | Value |
|---|---|
| Table | `platform.saas_payments` |
| Migration | `AddManualSaaSPayments` (`20260729182031_AddManualSaaSPayments`) |
| Prior migrations | `InitialPlatformCatalog`, `AddPlatformOrganizationsAndSubscriptions` retained |
| PK | `id` (UUID) |
| FKs | `organization_id` → organizations (restrict), `subscription_id` → subscriptions (restrict, nullable) |
| Check constraint | `ck_saas_payments_positive_amount` (amount > 0) |
| Unique index | `ux_saas_payments_reference` partial (method, normalized_reference, organization_id) WHERE status NOT IN ('Rejected','Voided') |
| Concurrency | PostgreSQL `xmin` row version; conflicts → `application.concurrency_conflict` |
| Excluded tables | POS payments, Utang payments, invoices, cards, webhooks, gateway tokens, HealthCare |

## 8. Application capability

- Repository: `ISaaSPaymentRepository` (EF implementation; no generic repository)
- Commands: CreateManualSaaSPayment, ConfirmSaaSPayment, RejectSaaSPayment, VoidSaaSPayment, ConfirmPaymentAndActivateSubscription
- Queries: by ID, by organization, by product, by status, by reference, by subscription (paginated)
- Conflicts: duplicate reference, already confirmed, already used, invalid transition, org/product mismatch → stable error codes; API maps to ProblemDetails (409 where conflict/transition)

## 9. API capability

Development-stage, unauthenticated:

| Route | Action |
|---|---|
| `POST /api/v1/platform/payments/manual` | Create manual payment |
| `GET /api/v1/platform/payments/{paymentId}` | Get payment |
| `GET /api/v1/platform/payments?status=&productCode=&reference=` | Filtered list |
| `GET /api/v1/platform/organizations/{orgId}/payments?status=` | List by org |
| `POST /api/v1/platform/payments/{paymentId}/confirm` | Confirm |
| `POST /api/v1/platform/payments/{paymentId}/reject` | Reject |
| `POST /api/v1/platform/payments/{paymentId}/void` | Void |
| `POST /api/v1/platform/payments/{paymentId}/activate-subscription` | Confirm + activate |

Confirmed absent: gateway, webhook, QR, card, POS, Utang, HealthCare, invoice routes.

Phase marker: `P3-WP03-manual-payment-activation`. Retained: `GET /`, `GET /health`.

## 10. Migration result

Isolated Docker `postgres:18` on `127.0.0.1:5434`:

```text
dotnet ef database update
  → Applied InitialPlatformCatalog + AddPlatformOrganizationsAndSubscriptions + AddManualSaaSPayments
dotnet ef database update AddPlatformOrganizationsAndSubscriptions
  → Dropped saas_payments (9 prior tables remain)
dotnet ef database update
  → Re-applied AddManualSaaSPayments (10 platform tables)
```

History table: `public.__EFMigrationsHistory`. Partial unique index verified. Positive amount constraint verified. No POS/Utang/invoice/card/webhook tables.

## 11. Build and tests

| Command | Result |
|---|---|
| `dotnet build ExItS.slnx -c Release` | Exit 0; 0 warnings; 0 errors |
| `dotnet test ExItS.slnx -c Release --no-build` | Exit 0 |

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| ExItS.Platform.UnitTests | 172 | 0 | 0 |
| ExItS.ArchitectureTests | 37 | 0 | 0 |
| ExItS.Platform.IntegrationTests | 42 | 0 | 0 |
| **Total** | **251** | **0** | **0** |

## 12. Runtime validation

| Step | Result |
|---|---|
| API | `http://127.0.0.1:5288` |
| `GET /` | `phase=P3-WP03-manual-payment-activation` |
| `GET /health` | Healthy |
| Create org → catalog → trial subscription | OK |
| Create manual SaaS payment | PendingConfirmation |
| Retrieve payment | OK |
| Duplicate reference | **409** |
| Confirm payment | Confirmed |
| Activate subscription (atomic confirm + activate) | Payment=Confirmed, Subscription=Active |
| Reuse payment | **409** |
| Create + reject | Rejected |
| Create, confirm, void | Voided |
| List by org | 3 payments returned |
| Double confirm | **409** |
| No gateway/webhook/QR/POS tables | Verified |

## 13. Security and authorization

- No credentials committed beyond local-dev Docker password (development/design-time only).
- No PHI / clinical entities in Platform payment persistence.
- No payment credentials, GCash PINs, OTPs, card data stored.
- No fake production authentication.
- Actor references accept plain strings — **production blocker** requiring authenticated Platform operator.
- Routes remain development-stage until auth WP (R-045, R-050+).

## 14. HealthCare freeze

- `git ls-files -- HealthCare/` empty
- `/HealthCare/` ignored
- No HealthCare project in `ExItS.slnx`

## 15. Risks

| ID | Note |
|---|---|
| R-012 | Further mitigated — manual payments persist; automated billing/invoices still open |
| R-031 / R-032 | Identity/membership still not authenticated/persisted |
| R-035 | Still open — calendar EOM |
| R-045 | Expanded to payment mutation APIs |
| R-046 | Migration targeting discipline continues |
| R-047 | Mitigated — confirmed payment required for activation; still not gateway-verified |
| R-048 | No background scheduler |
| R-049 | Repeat-trial eligibility ambiguity |
| R-050 | Expanded to payment endpoints |
| R-051 | Manual confirmation fraud/error without separation of duties |
| R-052 | Duplicate reference scope is provisional |
| R-053 | Payment amount not auto-reconciled against catalog price |
| R-054 | Void/reversal has no invoice or credit-note linkage |
| R-055 | Unauthenticated payment mutation endpoints (prod gate) |
| R-056 | No reconciliation engine for manual payments |
| R-057 | Manual payment mistaken for automatic gateway integration |

## 16. Amount validation status

- Payment records the manual amount and currency.
- Explicit confirmation required.
- **Automated amount reconciliation against catalog price is deferred.** No price engine, tax, discount, FX, or proration.

## 17. Git evidence

| Field | Value |
|---|---|
| Feature commit | _(to be recorded)_ |
| Message | `feat(platform): implement manual payment activation` |
| Hash-record commit | _(this docs commit)_ |

## 18. Next work package

**P3-WP04 — Entitlement Snapshots and Grace Rules** (do not begin until authorized).
