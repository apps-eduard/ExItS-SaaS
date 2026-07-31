# Phase 12 — Reusable SaaS Product Foundation and Bootstrap

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-11-web-ui-reporting-design-system.md)

## Status

**In progress.** **P12-WP01**–**P12-WP04** are **complete**. Exact next: **P12-WP05 — Product Bootstrap Prompt** when authorized (do not begin).

Phase 11 remains closed. Authoritative foundation: [`docs/Product-Foundation/exits-product-foundation-reference.md`](../Product-Foundation/exits-product-foundation-reference.md). Templates: [`docs/Product-Foundation/Templates/`](../Product-Foundation/Templates/README.md). Product context rule: `.cursor/rules/exits-product-context.mdc`.

## Progress

| WP | Status | Report / tip |
|---|---|---|
| P12-WP01 — Platform–Product Contract Audit | **Complete** | [report](../reports/P12-WP01-platform-product-contract-audit.md) · `32889be0851fa0969e8abfa6b7c66784b12e9e8b` |
| P12-WP02 — Authoritative Product Foundation Reference | **Complete** | [report](../reports/P12-WP02-authoritative-product-foundation-reference.md) · `8f151d658011a3ad0854aab9f8774361f8a788a6` |
| P12-WP03 — Product Documentation Templates | **Complete** | [report](../reports/P12-WP03-product-documentation-templates.md) · `65b02a1dd9336b39b79fc41527969f6289ad7072` |
| P12-WP04 — Cursor Product Context Rule | **Complete** | [report](../reports/P12-WP04-cursor-product-context-rule.md) · `1243c78d65e347b23949b19ce2edf564fe972aad` |
| P12-WP05 — Product Bootstrap Prompt | Not started | — |
| P12-WP06 — Reference Product Dry Run | Not started | — |
| P12-WP07 — Foundation Hardening and Closeout | Not started | — |

## Purpose

Phase 12 creates the permanent ExItS product foundation used to add future SaaS products without requiring Cursor or another AI agent to scan the entire Platform, POS, or historical repository.

The phase establishes a concise Platform–Product contract, reusable product documentation templates, product context-loading rules, and a deterministic bootstrap workflow for products such as:

- Loan System
- Pawnshop
- Buy Now Pay Later
- future ExItS SaaS products

The Platform remains the shared SaaS control plane. Every product remains an independently subscribed, independently authorized, independently persisted operational system.

## Phase Objective

Create one authoritative product-foundation reference that allows a new product to begin from a small approved context pack instead of repository-wide discovery.

A new product should normally require Cursor to read only:

1. `.cursor/rules/exits-workflow.mdc`
2. the shared ExItS product-foundation reference
3. the new product's approved `product-definition.md`
4. the new product's roadmap and current work package
5. files explicitly required by the active implementation task

Cursor must not scan unrelated product source trees, completed historical reports, unrelated migrations, or the full Platform implementation unless a concrete dependency requires it.

## Architectural Principles

1. Platform is the SaaS control plane.
2. Each product is an independent operational bounded context.
3. Each product has its own database.
4. No cross-product database access or foreign keys.
5. Platform product access does not automatically grant product operational authority.
6. Product subscriptions, plans, commercial states, and entitlements are independent per product.
7. Platform SaaS billing is separate from product operational money.
8. Shared libraries contain technical primitives only, not shared product-domain entities.
9. New product behavior must be explicitly approved; AI must not infer a “typical” business system.
10. Context loading must be narrow, deterministic, and auditable.

## Platform Responsibilities

Platform owns:

- organizations
- Platform memberships
- product catalog
- product availability
- plans
- subscriptions
- commercial state
- product entitlements
- SaaS subscription billing and payments
- Platform administration
- Platform audit
- trusted organization context
- trusted actor context or production identity integration when implemented

Platform does not own product operational records.

## Product Responsibilities

Each product owns:

- product domain data
- operational workflows
- product-local roles
- product-local grants
- product database and schema
- product migrations
- product API
- product web and/or mobile UI
- product operational audit
- product reports
- product-specific data retention
- product-specific privacy and security rules
- product operational payments and ledgers

## Independent Subscription Model

An organization may hold different commercial states for different products.

Example:

```text
Organization: ABC Trading

Subscriptions
├── PinoyBusinessPOS
│   ├── Plan: Standard
│   └── State: Active
├── Pawnshop
│   ├── Plan: Professional
│   └── State: Trialing
├── LoanSystem
│   ├── Plan: Basic
│   └── State: PastDue
└── BuyNowPayLater
    └── Not subscribed
```

Rules:

- changing one product subscription must not change another product subscription
- one product's entitlement must not authorize another product
- one product's cancellation must not delete or mutate another product's data
- Platform may expose a unified product launcher, but each product enforces its own operational authorization

## Database Isolation

Required database pattern:

```text
Platform              → ExItS_Platform
PinoyBusinessPOS      → ExItS_PinoyBusinessPOS
Pawnshop              → ExItS_Pawnshop
Loan System           → ExItS_LoanSystem
Buy Now Pay Later     → ExItS_BuyNowPayLater
```

Each product may define its own schema according to repository conventions.

Products must not:

- query Platform tables directly
- query another product database
- create cross-product foreign keys
- share operational tables
- use another product as an identity authority
- reuse another product's role-assignment table
- write product operational money into Platform SaaS-payment tables

Integration must occur through approved contracts or APIs.

## Access Flow

```text
Actor
  ↓
Platform identity and organization context
  ↓
Platform product subscription and entitlement check
  ↓
Product entry permitted or denied
  ↓
Product-local role and grant evaluation
  ↓
Product resource/workflow authorization
  ↓
Product database only
```

Effective product authorization requires all applicable checks:

1. trusted actor context
2. trusted organization context
3. valid Platform product access
4. allowed commercial state
5. required product entitlement
6. active product-local role or assignment
7. required product-local grant
8. resource ownership and workflow invariants

No single check bypasses another.

## Billing Separation

Platform SaaS money and product operational money must remain separate.

Examples:

- POS sales belong to POS
- Pawnshop loan releases, renewals, redemptions, and interest payments belong to Pawnshop
- Loan System disbursements and repayments belong to Loan System
- BNPL installments belong to Buy Now Pay Later
- subscription invoices and subscription payments belong to Platform

Do not reuse Platform SaaS-payment records as product financial ledgers.

## Shared Technical Foundation

Safe shared libraries may contain:

- organization-context contracts
- actor-context contracts
- product-access and entitlement contracts
- ProblemDetails conventions
- idempotency primitives
- audit abstractions
- pagination/filter primitives
- date/time abstractions
- observability primitives
- localization infrastructure
- reusable web design components
- reusable MAUI design components

Do not place these in shared libraries:

- POS Sale
- PawnTicket
- Loan Account
- BNPL Agreement
- product-specific payment ledger
- product-specific role assignment
- product-specific approval workflow
- product-specific report projection

## Product Foundation Files

Authoritative (P12-WP02) and templates (P12-WP03):

```text
docs/Product-Foundation/
├── README.md
├── exits-product-foundation-reference.md
└── Templates/
    ├── README.md
    ├── product-definition.md
    ├── architecture.md
    ├── security.md
    ├── authorization-matrix.md
    ├── development-plan.md
    ├── roadmap.md
    ├── work-package-report.md
    ├── risks-and-decisions.md
    ├── deployment-notes.md
    ├── FILE-MANIFEST.md
    └── product-docs-readme.md
```

Bootstrap prompt arrives in a later work package (P12-WP05).

The authoritative reusable reference is:

```text
docs/Product-Foundation/exits-product-foundation-reference.md
```

## Product Documentation Structure

Each **new** product should maintain a concise authoritative documentation pack at:

```text
src/Products/<ProductName>/Docs/
├── README.md                 # optional
├── product-definition.md
├── architecture.md
├── security.md
├── authorization-matrix.md
├── development-plan.md
├── roadmap.md
├── risks-and-decisions.md
├── FILE-MANIFEST.md
├── deployment-notes.md       # optional until packaging
└── reports/
```

Testing expectations live in `development-plan.md` (no separate testing template).

(D-P12-02: intended root for new products. Existing PinoyBusinessPOS portfolio docs under `docs/` are not mass-migrated by Phase 12 foundation WPs.)

## Context Loading Policy

For new product work, Cursor must initially load only:

- permanent workflow rules
- `docs/Product-Foundation/exits-product-foundation-reference.md`
- the active product's authoritative documentation
- files explicitly referenced by the active work package

Cursor must not initially scan:

- other product source trees
- completed historical reports
- unrelated migrations
- unrelated tests
- generated output
- the complete Platform implementation
- the complete POS implementation

Additional context may be loaded only when required by:

- an explicit contract reference
- a direct project dependency
- a compilation error
- a failing test
- an architecture invariant
- an approved shared component
- a concrete integration requirement

Before expanding context, Cursor must state the reason.

## New Product Bootstrap Workflow

### Step 1 — Product-owner definition

Create and approve:

```text
Products/<ProductName>/Docs/product-definition.md
```

It must define:

- product code
- product name
- target organizations
- target users
- product database
- product schema
- API/UI targets
- MVP capabilities
- explicit exclusions
- product-local roles
- product-local grants
- Platform product code
- proposed plans and entitlement groups
- sensitive data classes
- operational money model
- external integrations
- compliance requirements

### Step 2 — Scope gate

Cursor must stop when material business policy is missing.

Do not infer behavior from a typical Loan, Pawnshop, or BNPL system.

Missing decisions must be reported as a concise product-owner decision list.

### Step 3 — Documentation generation

After product definition approval, generate:

- architecture
- security
- authorization matrix
- development plan
- roadmap
- testing strategy
- file manifest

Do not generate feature code during documentation bootstrap unless explicitly authorized.

### Step 4 — Product skeleton

Create only the approved solution/project/database skeleton.

Required boundaries:

- separate database
- no cross-product references
- approved Platform contract only
- product-local authorization boundary
- no fabricated workflows

### Step 5 — Work-package implementation

Implement one approved work package at a time.

Every work-package prompt should include:

- objective
- business rules
- authorization
- persistence
- API
- UI
- online/offline policy
- security and concurrency
- tests
- documentation
- explicit exclusions
- acceptance
- Git workflow

## Phase Work Packages

### P12-WP01 — Platform–Product Contract Audit

#### Status

**Complete** — see [P12-WP01 report](../reports/P12-WP01-platform-product-contract-audit.md).

#### Objective

Inspect the actual Platform and completed POS implementation and document the smallest stable Platform–Product contract required by future products.

#### Deliverables

- Platform responsibility inventory
- product responsibility inventory
- current product catalog/plan/subscription/entitlement flow
- trusted actor and organization context flow
- current integration contracts
- database-isolation validation
- operational-money separation validation
- approved contract gaps

#### Acceptance

- no undocumented direct product-to-Platform database dependency remains
- no cross-product database dependency is authorized
- contract reflects actual implementation, not assumptions

### P12-WP02 — Authoritative Product Foundation Reference

#### Status

**Complete** — see [P12-WP02 report](../reports/P12-WP02-authoritative-product-foundation-reference.md). Authoritative file: [`docs/Product-Foundation/exits-product-foundation-reference.md`](../Product-Foundation/exits-product-foundation-reference.md).

#### Objective

Create `docs/Product-Foundation/exits-product-foundation-reference.md` as the concise permanent reference for future products.

#### Deliverables

- Platform–Product boundary
- subscription independence
- authorization intersection
- database isolation
- billing separation
- shared-library boundaries
- context-loading policy
- product bootstrap workflow
- scope-gate rules

#### Acceptance

- future product prompts can reference this file instead of scanning the entire repository
- file is concise enough for repeated AI use
- file contains no product-specific business assumptions

### P12-WP03 — Product Documentation Templates

#### Status

**Complete** — see [P12-WP03 report](../reports/P12-WP03-product-documentation-templates.md). Pack: [`docs/Product-Foundation/Templates/`](../Product-Foundation/Templates/README.md).

#### Objective

Create reusable templates for product planning and governance.

#### Deliverables

- product-definition template
- architecture template
- security template
- authorization template
- development-plan template
- roadmap template
- testing template
- file-manifest template

#### Acceptance

- templates contain required sections and decision gates
- templates do not pre-authorize domain behavior
- placeholders are obvious and cannot be mistaken for approved policy

### P12-WP04 — Cursor Product Context Rule

#### Status

**Complete** — see [P12-WP04 report](../reports/P12-WP04-cursor-product-context-rule.md). Rule: `.cursor/rules/exits-product-context.mdc`.

#### Objective

Add a permanent, token-efficient product context-loading rule.

#### Requirements

The rule must instruct Cursor to:

- read the shared foundation reference
- read only the active product docs
- read only current work-package files
- avoid unrelated products and history
- expand context only for a stated concrete reason
- stop rather than invent missing policy

#### Acceptance

- ordinary product work no longer begins with repository-wide scanning
- unrelated product source is excluded by default
- architecture tests or documentation checks prevent accidental weakening where practical

### P12-WP05 — Product Bootstrap Prompt

#### Objective

Create one reusable prompt that starts a new ExItS product safely.

#### Deliverables

- product context checklist
- repository validation
- product-definition scope gate
- documentation generation workflow
- architecture boundary enforcement
- Git workflow
- final response format

#### Acceptance

- prompt works for Loan System, Pawnshop, BNPL, and future products without domain assumptions
- prompt requires product-owner decisions before implementation

### P12-WP06 — Reference Product Dry Run

#### Objective

Validate the foundation using one documentation-only sample product.

Preferred sample:

- Pawnshop, Loan System, or Buy Now Pay Later as selected by the product owner

#### Rules

- create documentation only
- do not implement feature code
- do not create production migrations
- measure which files Cursor needed to read
- record token/context improvements qualitatively
- identify missing foundation rules

#### Acceptance

- the sample product can be planned without scanning POS implementation
- Platform integration is clear
- database and authorization boundaries are clear
- missing product policy is surfaced rather than invented

### P12-WP07 — Foundation Hardening and Closeout

#### Objective

Finalize the reusable product foundation after the dry run.

#### Required Evidence

- final foundation reference
- all templates
- Cursor context-loading rule
- bootstrap prompt
- sample-product dry-run report
- file-read/context audit
- architecture validation
- documentation usage guide
- open risks
- exact next phase or first product roadmap

#### Acceptance

- foundation is authoritative and versioned
- new products can start from the concise reference pack
- no requirement to scan entire Platform/POS repositories remains for normal work
- all tests pass
- documentation matches implementation
- validated commits are pushed
- `main = origin/main`
- working tree is clean

## Testing Strategy

### Documentation and architecture tests

Where practical, validate:

- every product declares its database
- product database names are unique
- no cross-product project reference exists without an approved technical contract
- no product migration targets another product database
- no cross-product EF relationship exists
- Platform product-access contracts are used instead of direct Platform-table queries
- product-local role data is not stored in Platform
- operational payment records are not stored as SaaS payments
- product documentation includes explicit exclusions and scope gates

### Bootstrap validation

For the dry run, record:

- files initially read
- files additionally required
- reason for each context expansion
- unresolved product-owner decisions
- whether unrelated product code was avoided

## Security Requirements

- trusted organization context
- trusted actor context
- product entitlement validation
- product-local authorization
- cross-organization concealment
- no cross-product data leakage
- no secret or credential duplication in product docs
- no sensitive full request-body logging
- no new browser/mobile authoritative storage without product authorization
- product-specific privacy and retention requirements must be documented before implementation

## Explicit Exclusions

Phase 12 does not implement:

- Loan System business workflows
- Pawnshop business workflows
- Buy Now Pay Later business workflows
- new subscription billing behavior
- production authentication
- universal cross-product reporting
- shared product-domain entities
- shared product financial ledgers
- accounting
- tax
- payment gateways
- Windows MAUI
- Phase 11 UI redesign work

## Phase Exit Criteria

Phase 12 is complete when:

- the actual Platform–Product contract is documented
- one concise authoritative product-foundation reference exists
- reusable product documentation templates exist
- a token-efficient Cursor context-loading rule is active
- a reusable product bootstrap prompt exists
- one documentation-only product dry run succeeds
- normal new-product work does not require repository-wide scanning
- no cross-product database or domain coupling is introduced
- subscription independence is preserved
- product-local authorization remains separate from Platform access
- all validation passes
- documentation is complete
- commits are pushed
- `main = origin/main`
- working tree is clean

## Suggested Phase Marker

```text
P12-reusable-saas-product-foundation
```

## Suggested Primary Report

```text
docs/reports/P12-reusable-saas-product-foundation.md
```

## Final Response Format for Each Work Package

1. Status
2. Delivered scope
3. Platform–Product contract impact
4. Product-context/token-efficiency impact
5. Files/templates created or changed
6. Architecture and database-isolation evidence
7. Security and authorization impact
8. Tests and validation
9. Preserved exclusions
10. Remaining risks
11. Git commits and final tip
12. Exact next work package
