# PinoyBusinessPOS Requirements

[Home](../index.md) | [Dashboard](../portfolio-progress.md) | [Final boundaries](../engineering/final-portfolio-boundaries.md) | [Capability boundary](../engineering/platform-product-capability-boundary.md) | [Contracts](../engineering/platform-product-contracts.md)

## Positioning

PinoyBusinessPOS is a compact, multilingual, offline-capable retail management platform initially optimized for **Sari-Sari stores and mini groceries**, while being architected for broader Philippine small and medium retail businesses.

Examples of businesses the architecture should accommodate (generic retail — not industry-regulated add-ons in MVP):

- Convenience stores
- Pharmacies (generic inventory only; regulated medicine compliance is out of MVP unless separately approved)
- Hardware stores
- Clothing and footwear shops
- Office and school-supply stores
- Electronics and accessories stores
- Personal-care stores
- Pet-supply stores
- Small wholesalers
- Other straightforward retail businesses

**First-market focus remains** Sari-Sari stores and mini groceries. Listing other industries does **not** expand MVP scope.

## Domain language

Prefer generic terms in domain models and APIs:

- Business / Organization / Store / Branch / Customer / Product / Sale / Inventory / Supplier  
- CustomerCredit / CreditEntry / CreditPayment / Register  

Avoid hard-coding Sari-Sari-only or grocery-only assumptions, one-store-only limits, Utang as the only future credit mechanism, or Philippine-language labels inside domain entities (UI localization handles display language).

## Target users (initial)

- Sari-Sari Store owners
- Mini Grocery owners
- Cashiers
- Managers in later plans
- Owners/managers of other small retail formats as the product expands (same core model)

## Commercial progression

1. Three-month Utang trial
2. Paid Utang plan
3. Basic Store
4. Full POS after the first commercial MVP

## Utang MVP

- Customer profile
- Remarks-based credit entry
- Amount and optional due date
- Partial/full payment against existing debt via **Cash** or **GCash** (manual verification)
- Customer ledger and balance
- Overdue monitoring
- Statement and receipt sharing
- Trial restrictions that never hide existing balances

## MVP retail payment methods

PinoyBusinessPOS MVP supports these retail payment methods (conceptual method codes; UI labels via localization, not domain hard-coding):

```text
cash
gcash
customer-credit
```

| Method | Sale tender | Credit repayment | MVP verification |
|---|---|---|---|
| Cash | Yes | Yes | Cashier records tender + change |
| GCash | Yes | Yes | **Manual** cashier/authorized confirmation; reference required |
| Customer Credit / Utang | Yes (sale on credit) | N/A (is the obligation) | Requires active Customer Credit entitlement |

### Payment boundaries (do not mix)

```text
SaaS Payment          → business pays ExITS for software     (Platform)
Retail Sale Payment   → retail customer pays store for goods (POS)
Customer Credit Payment → customer pays existing Utang balance (POS)
```

Platform may later accept GCash for **SaaS** subscription billing. That is a separate Platform payment method and must **not** reuse POS retail-payment entities.

```text
Platform GCash → business pays ExITS
POS GCash      → retail customer pays the store
```

### Conceptual model (documentation only)

```text
Sale
└── SalePayments
    ├── cash
    ├── gcash
    └── customer-credit

CustomerCredit
└── CreditPayments
    ├── cash
    └── gcash
```

Do not create separate Sale aggregates per tender type. A Sale may later support multiple payment records; **split tender is deferred** unless separately approved. Model stays extensible for future methods.

### Cash payment (MVP)

Must support: sale amount, amount tendered, change due (system-calculated), payment timestamp, recording user, store/branch, register/shift when those capabilities exist, payment status, void/correction audit. Negative payment amounts are prohibited.

### GCash payment (MVP — manual)

Must support: amount (> 0), GCash reference number (**required**, normalized before compare/store), payment timestamp, recording user, store/branch, register/shift when available, optional internal note, payment status, void/correction audit.

Rules:

- Recording a reference does **not** mean the system verified the payment with GCash.
- Cashier must confirm receipt through the merchant’s normal GCash process before completing the transaction.
- Duplicate references must at least **warn**; hard-block vs warn-only is a later business rule (OD-11).
- Do **not** store GCash account credentials, PINs, OTPs, access tokens, or other secrets.

**Deferred (not MVP):** direct GCash API, automatic verification, webhooks, automatic QR generation, payment links, gateway settlement, processor-fee calculations, other e-wallets, cards, split payment, refund-to-original-channel, cash-drawer hardware, advanced tender reconciliation.

### Customer Credit / Utang (MVP)

New credit entry requires: customer, active Customer Credit entitlement, amount, entry date, due date where applicable, store/branch, recording user, audit record.

Ledger supports: credit entries, partial/full payments, remaining balance, overdue state, payment history. Credit repayments use `cash` or `gcash` (GCash repayment requires reference).

**After Utang trial expires — allowed:** view customers/balances/history; receive **Cash** or **GCash** payments against existing debt; complete existing balance payment; view payment history; upgrade/renew.

**Blocked after expiry:** create new credit; increase debt; add new credit entries.

**P2-WP03 Platform feature codes (commercial only; no POS entities):** `customer-credit-view`, `customer-credit-repay`, `customer-credit-create`.

**Trial duration policy:** The product requirement is a **three-calendar-month** Utang trial (`Trial expiration = trial start timestamp plus three calendar months`). The generic Platform `TrialDefinition` accepts a configured positive duration only — **90 days is not an approved substitute**. Calendar-month arithmetic and end-of-month behavior (e.g. start 31 Jan → 30 Apr vs another approved rule) remain assigned to a later product/catalog configuration work package and are not implemented in Platform Domain.

### Payment status and audit (conceptual)

Statuses: Completed, Voided, Corrected; Refunded later when refunds exist.

Audit: cash/GCash payment recorded; GCash reference changed; payment voided/corrected; credit payment recorded/reversed; payment method changed. Changing a completed payment requires authorization, reason, and audit.

### Offline behavior (high level)

Cash and manually confirmed GCash may be recorded offline with stable local IDs. Sync must be idempotent (no duplicate payments). GCash reference duplication checked locally where possible and again on server sync. Conflicts must not silently change financial records. Detail remains Phase 7.

## Basic Store

- Product catalog and barcode
- Simple sales with MVP payment methods (`cash`, `gcash`, `customer-credit`)
- Product-based Utang
- Basic inventory
- Expenses
- Basic dashboard and reports

## Full POS

- Suppliers and purchasing
- Advanced inventory
- Returns/refunds/exchanges
- Cash drawers and shifts
- Advanced roles and reports
- Multiple registers

## Experience requirements

- Compact information density without cramped touch targets
- Responsive phone, tablet and Windows layouts
- English and Filipino/Tagalog UI
- Light, dark and system themes
- Offline-first daily operations
- Accessible labels, focus states and error messages
- Native CSS / Razor components (no Ant Design, no Tailwind)
