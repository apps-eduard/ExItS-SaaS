# Platform Admin Web — Product + Commercial Administration Screen Specifications

**Status:** Documentation Only — implementation not authorized  
**Source:** PLATFORM-WEB-DOC-07  
**Branch:** `docs/platform-admin-web-v2`

---

## 0. Money ownership boundaries (non-negotiable)

| Money | Owner | Binding |
|---|---|---|
| SaaS subscription / billing payments | Platform | Implemented / Required |
| POS operational money (sales, tenders, expenses, shifts) | POS product | Required — never Platform SaaS billing |
| PLM operational money (loan ledgers, collections, cash) | PLM product | Required — never Platform SaaS billing |

These screens must never display product operational money as Platform billing.

Product usage signals that cross into Platform (e.g., for usage-based billing) must travel through approved contracts (D-P12-03, unresolved). This DOC does not invent that transport.

---

## 1. Capability requirement IDs (DOC-07)

| ID | Description |
|---|---|
| `PWEB-CAP-PRODUCT-LIST` | List catalog products |
| `PWEB-CAP-PRODUCT-GET` | Get product detail/metadata |
| `PWEB-CAP-PRODUCT-CREATE` | Register a new product in the catalog |
| `PWEB-CAP-PRODUCT-MANAGE` | Activate/deactivate/retire product |
| `PWEB-CAP-PLAN-LIST` | List plans for a product |
| `PWEB-CAP-PLAN-GET` | Get plan detail including versions and feature grants |
| `PWEB-CAP-PLAN-CREATE` | Create a new plan |
| `PWEB-CAP-PLAN-MANAGE` | Activate/deactivate/retire plan; manage plan versions |
| `PWEB-CAP-TRIAL-LIST` | List trial definitions |
| `PWEB-CAP-TRIAL-MANAGE` | Create/retire trial definitions |
| `PWEB-CAP-SUBSCRIPTION-LIST` | List subscriptions (filterable by org/product/status) |
| `PWEB-CAP-SUBSCRIPTION-GET` | Get subscription detail |
| `PWEB-CAP-SUBSCRIPTION-MANAGE` | Change subscription state (activate, suspend, cancel, expire, reactivate) |
| `PWEB-CAP-SUBSCRIPTION-PLAN-CHANGE` | Upgrade/downgrade/schedule plan change |
| `PWEB-CAP-ENTITLEMENT-LIST` | List entitlements for an organization/product |
| `PWEB-CAP-ENTITLEMENT-GET` | Get entitlement snapshot detail |
| `PWEB-CAP-ENTITLEMENT-OVERRIDE` | Create/revoke feature overrides |
| `PWEB-CAP-BILLING-LIST` | List SaaS payment records |
| `PWEB-CAP-BILLING-GET` | Get SaaS payment detail |
| `PWEB-CAP-BILLING-RECORD` | Record manual SaaS payment |
| `PWEB-CAP-BILLING-CONFIRM` | Confirm/reject/void SaaS payment |
| `PWEB-CAP-USAGE-LIST` | List billable usage events (future; not implemented) |
| `PWEB-CAP-USAGE-CORRECT` | Correct/void a usage event (future; not implemented) |
| `PWEB-CAP-PERSONAL-FEATURE-LIST` | List personal feature definitions |
| `PWEB-CAP-PERSONAL-FEATURE-MANAGE` | Update personal feature definitions |

Backend existence is **not claimed**. DOC-09 will verify.

---

## 2. A) Product Catalog

### Purpose
Administer Platform-owned product catalog: the registry of products available in the ExItS portfolio (e.g., PinoyBusinessPOS, Pinoy Loan Manager when registered, future products). This is control-plane metadata, not product operational UI.

### Route concept
Top-level "Products" page in the Platform shell Commercial group.

### Primary personas
- Platform Administrator

### Access / authorization expectation
- `platform.catalog.manage` or equivalent view permission.

### Data displayed
- Product list: name, slug/identifier, status (active/inactive/retired), creation date.
- Per-product summary: plan count, active subscription count (if backed).

### Primary actions
- Open product detail (navigation).
- Create product (if `PWEB-CAP-PRODUCT-CREATE` is available and permitted).

### Secondary actions
- Filter by status. Export (if permitted).

### Search
- Search by product name/slug.

### Filtering / sorting
- Status filter. Sort by name or created date.

### Pagination
- Server pagination.

### Table / card behavior
- Desktop: table. Mobile: card list.

### Loading / empty / zero-result / error / forbidden
- Standard per DOC-06 template.

### Destructive actions
- Retire product requires confirmation dialog.

### Audit implications
- Product create/activate/deactivate/retire are audited server-side.

### Responsive behavior
- Standard collection page responsive rules.

### Accessibility
- Semantic table headers. Keyboard-navigable rows.

### Required backend capabilities
- `PWEB-CAP-PRODUCT-LIST`

### Explicit non-goals
- Do not show POS/PLM operational workflows or product-internal configuration.

---

## 3. B) Product Detail

### Purpose
View and manage Platform-level metadata for a single product: its plans, trial definitions, feature definitions, and commercial relationships.

### Route concept
Product detail workspace with tabs (within Products navigation).

### Primary personas
- Platform Administrator

### Access / authorization expectation
- `platform.catalog.manage` or equivalent.

### Data displayed
- Product identity: name, slug, status, description, creation/update dates.
- Plans tab: list of plans with status, version count, feature grant summary.
- Trial definitions tab: list of trial definitions with status.
- Feature definitions tab: list of feature definitions.
- Personal features tab: personal feature definitions (if applicable to this product).

### Primary actions
- Edit product metadata (if permitted).
- Create plan (if `PWEB-CAP-PLAN-CREATE` available).
- Create trial definition (if `PWEB-CAP-TRIAL-MANAGE` available).

### Secondary actions
- Activate/deactivate/retire product (confirmation required).
- Navigate to plan detail.

### Search / filtering / sorting
- Within tabs: search by plan name; filter by status.

### Pagination
- Server pagination within tabs.

### Table / card behavior
- Tabs use table (desktop) / card (mobile).

### Loading / empty / zero-result / error / forbidden
- Per-tab skeleton + error handling per DOC-06.

### Destructive actions
- Retire product: confirmation dialog with explicit warning.
- Deactivate product: confirmation dialog.

### Audit implications
- All product state changes audited.

### Required backend capabilities
- `PWEB-CAP-PRODUCT-GET`
- `PWEB-CAP-PLAN-LIST`
- `PWEB-CAP-TRIAL-LIST`
- `PWEB-CAP-PERSONAL-FEATURE-LIST`

### Explicit non-goals
- No product-operational configuration (POS categories, PLM loan products).

---

## 4. C) Plans / Pricing Administration

### Purpose
Administer Platform SaaS plans: plan definitions, plan versions, feature grants, and pricing metadata. This is subscription/pricing configuration, not product operational pricing.

### Route concept
Plan detail page navigated from product detail.

### Primary personas
- Platform Administrator
- Commercial/Billing Operator

### Access / authorization expectation
- `platform.plans.manage` or equivalent.

### Data displayed
- Plan identity: name, status, product association, commercial package summary.
- Plan versions: list with status (draft/published), feature grants per version.
- Feature grant detail per version.

### Primary actions
- Create draft plan version.
- Publish plan version (confirmation required).

### Secondary actions
- Edit plan metadata. Activate/deactivate/retire plan (confirmation).
- Replace draft version grants.

### Search / filtering / sorting
- Filter versions by status. Sort by creation date.

### Pagination
- Server pagination if version list is long.

### Loading / empty / zero-result / error / forbidden
- Standard per DOC-06.

### Destructive actions
- Retire plan: confirmation dialog.
- Publishing a version is non-destructive but significant: confirmation dialog showing affected subscribers.

### Audit implications
- Plan create/activate/deactivate/retire and version publish are audited.

### Required backend capabilities
- `PWEB-CAP-PLAN-GET`
- `PWEB-CAP-PLAN-CREATE`
- `PWEB-CAP-PLAN-MANAGE`

### Explicit non-goals
- No POS/PLM product-level pricing rules.

---

## 5. D) Subscriptions Administration

### Purpose
View and manage organization/product subscription state within Platform scope.

### Route concept
Top-level "Subscriptions" page in the Commercial group, and per-organization subscription tab in the Organization workspace.

### Primary personas
- Platform Administrator
- Commercial/Billing Operator
- Operations/Support Operator (view)

### Access / authorization expectation
- `platform.subscriptions.view` / `platform.subscriptions.manage` as applicable.

### Data displayed
- Subscription list: organization name, product, plan, status (trialing/active/past-due/suspended/cancelled/expired), trial dates, plan change history summary.

### Primary actions
- Open subscription detail.
- Change subscription state (if `PWEB-CAP-SUBSCRIPTION-MANAGE`).

### Secondary actions
- Filter by status/product/plan. Export.
- Preview plan change (if `PWEB-CAP-SUBSCRIPTION-PLAN-CHANGE`).

### Search / filtering / sorting
- Search by organization name.
- Filter by subscription status, product, plan.
- Sort by created date, status, organization.

### Pagination
- Server pagination.

### Table / card behavior
- Desktop: table. Mobile: card list.

### Loading / empty / zero-result / error / forbidden
- Standard per DOC-06.

### Destructive actions
- Suspend subscription: confirmation dialog with warning about impact.
- Cancel subscription: confirmation with explicit consent.
- Reactivate: confirmation.

### High-risk action UX (§5 of the command)
- **Changing subscription state** (suspend/cancel/reactivate):
  - Confirmation dialog showing organization name, current state, target state, and impact summary.
  - Default focus on Cancel.
  - Server authorization and audit required.
  - UI confirmation never replaces server-side authorization.
- **Changing plan** (upgrade/downgrade):
  - Preview step showing current plan, target plan, effective date, and any prorated billing impact.
  - Confirmation dialog after preview.
  - Scheduled downgrades must clearly indicate when they take effect.

### Audit implications
- All subscription state changes and plan changes are audited.

### Required backend capabilities
- `PWEB-CAP-SUBSCRIPTION-LIST`
- `PWEB-CAP-SUBSCRIPTION-GET`
- `PWEB-CAP-SUBSCRIPTION-MANAGE`
- `PWEB-CAP-SUBSCRIPTION-PLAN-CHANGE`

### Explicit non-goals
- Do not display POS/PLM operational subscription-like concepts (e.g., borrower payment plans).

---

## 6. E) Entitlements Administration

### Purpose
View and manage product entitlements and feature overrides within Platform scope. Entitlements represent commercial feature access derived from subscription/plan. They are distinct from product-local permissions.

### Route concept
Top-level "Entitlements" page in the Commercial group, and per-organization entitlements tab.

### Primary personas
- Platform Administrator
- Commercial/Billing Operator

### Access / authorization expectation
- `platform.entitlements.view` / `platform.entitlements.manage`.

### Data displayed
- Entitlement list per organization/product: feature name, source (plan grant vs override), status, effective dates.
- Feature override list: override reason, created by, expiration.
- Entitlement snapshot detail: version, schema, reconciliation status.

### Primary actions
- Create feature override (if `PWEB-CAP-ENTITLEMENT-OVERRIDE`).

### Secondary actions
- Revoke feature override (confirmation required).
- Generate/reconcile entitlement snapshot.

### Search / filtering / sorting
- Filter by product, feature, override status.
- Sort by feature name or effective date.

### Pagination
- Server pagination.

### Loading / empty / zero-result / error / forbidden
- Standard per DOC-06.

### Destructive actions
- **Revoking a feature override**:
  - Confirmation dialog showing feature name, organization, and impact.
  - Default focus on Cancel.
  - Server audit required.
- **Granting a feature override**:
  - Confirmation dialog showing feature, reason, and expiration.
  - Not destructive but significant; requires explicit confirmation.

### Audit implications
- Override create/revoke and snapshot reconciliation are audited.

### Required backend capabilities
- `PWEB-CAP-ENTITLEMENT-LIST`
- `PWEB-CAP-ENTITLEMENT-GET`
- `PWEB-CAP-ENTITLEMENT-OVERRIDE`

### Explicit non-goals
- Entitlements are not product-local permissions. Do not conflate entitlement overrides with POS role grants or PLM operational grants.

---

## 7. F) Billing / Invoice Administration

### Purpose
Administer Platform SaaS billing records: manual SaaS payments, payment confirmation/rejection/voiding, and billing history. This is strictly Platform SaaS billing — subscription payments from organizations to ExItS.

### Route concept
Top-level "Payments" page in the Commercial group, and per-organization billing tab.

### Primary personas
- Platform Administrator
- Commercial/Billing Operator

### Access / authorization expectation
- `platform.subscriptions.manage` or billing-specific permissions as defined by the authorization matrix.

### Data displayed
- SaaS payment list: organization, product, amount, currency, status (pending/confirmed/rejected/voided), reference, payment date, recorded by.
- Payment detail: full metadata, linked subscription, confirmation/rejection reason.

### Primary actions
- Record manual SaaS payment (if `PWEB-CAP-BILLING-RECORD`).
- Confirm payment (if `PWEB-CAP-BILLING-CONFIRM`).

### Secondary actions
- Reject payment (confirmation required).
- Void payment (confirmation required, high-impact).
- Export billing records (if permitted).

### Search / filtering / sorting
- Search by organization name or payment reference.
- Filter by status, product, date range.
- Sort by date, amount, status.

### Pagination
- Server pagination.

### Table / card behavior
- Desktop: table with tabular-nums for amounts. Mobile: card list.

### Loading / empty / zero-result / error / forbidden
- Standard per DOC-06.

### High-risk action UX
- **Recording a manual payment**:
  - Form with organization, product, amount, currency, reference, and reason fields.
  - Confirmation dialog showing all details before submission.
  - Idempotency key managed by the client to prevent duplicate submissions.
- **Confirming a payment**:
  - Confirmation dialog showing payment details and subscription activation impact.
- **Rejecting / voiding a payment**:
  - Destructive confirmation dialog. Default focus on Cancel.
  - Reason field required for rejection/void.

### Destructive actions
- Payment rejection and voiding are destructive. Confirmation required with reason.

### Audit implications
- All payment state transitions are audited with actor, reason, and timestamp.

### Money ownership boundary check
- This screen displays **only** Platform SaaS billing records (`SaaSPayment*`).
- POS sales/tenders/expenses must **never** appear here.
- PLM loan payments/collections/disbursements must **never** appear here.
- If a future integration surfaces product operational money summaries for billing correlation, they must be clearly labeled as product-sourced and must not be editable from Platform billing.

### Required backend capabilities
- `PWEB-CAP-BILLING-LIST`
- `PWEB-CAP-BILLING-GET`
- `PWEB-CAP-BILLING-RECORD`
- `PWEB-CAP-BILLING-CONFIRM`

### Explicit non-goals
- Do not display PLM borrower balances as Platform billing.
- Do not display POS sales as Platform billing.
- Do not implement payment gateway integration in this documentation.

---

## 8. G) Usage / Metering Administration

### Purpose
Provide visibility into billable usage events for usage-based billing, corrections, and audit. This capability is **future / not currently implemented**.

### Route concept
Potential future page in the Commercial group or within subscription/entitlement detail.

### Primary personas
- Platform Administrator
- Commercial/Billing Operator

### Access / authorization expectation
- Future billing/metering permission.

### Data displayed (planned)
- Usage event list: organization, product, event type, quantity, timestamp, status.
- Usage corrections: original event, correction, reason, actor.

### Documented product contract concepts
PLM documents a preferred Platform usage billable event concept:
- **LOAN_DISBURSED**: a loan disbursement is the preferred billable event for PLM usage-based billing.
- **LOAN_DISBURSEMENT_REVERSED**: reversal of a disbursement.

These are **PLM product concepts** that would cross into Platform billing through an approved contract (D-P12-03, unresolved). This DOC does not invent that transport.

Important distinctions:
- Pre-release loan cancellation is **not** a Platform usage event.
- The usage event is the disbursement fact, not the loan approval.
- Platform receives the usage signal; Platform does not own the loan operational workflow.

### Primary actions (future)
- View usage events.
- Correct/void a usage event (with reason and confirmation).

### High-risk action UX
- **Correcting a usage event**:
  - Confirmation dialog with original event details, correction, and reason.
  - Audit trail for all corrections.

### Required backend capabilities
- `PWEB-CAP-USAGE-LIST` (future)
- `PWEB-CAP-USAGE-CORRECT` (future)

### Explicit non-goals
- Do not implement PLM loan processing or approval workflows.
- Do not duplicate PLM operational financial records.
- Do not invent the D-P12-03 usage transport in this documentation.
- Do not display POS operational transactions as usage events.
