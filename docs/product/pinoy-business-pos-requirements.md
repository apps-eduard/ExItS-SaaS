# PinoyBusinessPOS Requirements

[Home](../index.md) | [Dashboard](../portfolio-progress.md) | [Final boundaries](../engineering/final-portfolio-boundaries.md) | [Capability boundary](../engineering/platform-product-capability-boundary.md)

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
- Partial/full payment
- Customer ledger and balance
- Overdue monitoring
- Statement and receipt sharing
- Trial restrictions that never hide existing balances

## Basic Store

- Product catalog and barcode
- Simple cash/digital sale
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
