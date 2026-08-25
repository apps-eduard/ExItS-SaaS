# POS-POST-SUBSCRIPTION-ONBOARDING-01

**Package:** POS-POST-SUBSCRIPTION-ONBOARDING-01  
**Branch:** `feat/pos-react-client`  
**Scope:** React POS post–Start Business optional onboarding (Organization → Business setup → Products → Ready)

## Canonical flow

Personal → Choose Plan (`/personal/explore-pos`) → Trial / Subscribe → Create Organization (`/personal/start-business`) → subscription/entitlement active → session rotates to Organization Owner → **post-subscription onboarding** (`/onboarding`) → Ready → Start Selling (`/sell`)

### Onboarding steps (all optional / skippable)

1. **Organization Setup** — useful org/profile + operational contact fields already in Platform/POS models  
2. **Business Setup** — display preset for the org’s existing Platform business type (configuration preview; not a catalog import)  
3. **Product Template** — published Global Catalog templates via existing catalog-import APIs  
4. **Ready** — summary; **Start selling** or **Finish later**

Skipping never cancels organization creation, subscription/trial, entitlement, owner role, or POS access.

## Critical distinctions

| UI step | Source | Meaning |
|--------|--------|---------|
| Business Setup | Platform business type + client display presets | Configuration/preset messaging |
| Product Template | Platform `CatalogTemplate` / published templates | Product/catalog bundle import into POS-owned local snapshots |

Existing entity names such as catalog **BusinessTemplate** / **CatalogTemplate** remain **product catalog bundles**. This package does **not** repurpose them as config presets.

## Transaction boundary

Start Business succeeds first (org + subscription/trial + entitlement + owner + session). Onboarding is a **separate** optional phase. Closing the browser during onboarding does **not** roll back the organization.

## Progress persistence

| Item | Decision |
|------|----------|
| Existing React/Platform multi-step progress | None suitable |
| Decision | POS-owned `pos.organization_onboarding_progress` |
| Migration | `20260825101149_AddOrganizationOnboardingProgress` |
| Ensure | Creates `InProgress` only when called (new Start Business path) — **no backfill** for existing orgs |
| GET 404 | Existing org → no forced wizard |

Statuses: step `NotStarted` / `Completed` / `Skipped`; overall `InProgress` / `Completed` / `FinishedLater`.

## Resume / Finish later

- `InProgress` → resume `/onboarding` on org entry (`OnboardingResumeGate`)  
- `Completed` → normal POS  
- `FinishedLater` → normal POS + subtle **Finish setup** on Org More  
- Existing orgs without a progress row → never forced

## APIs

- `GET/POST/PUT /api/v1/pos/onboarding/progress` (+ `/ensure`)  
- Org profile: `GET/PUT /api/v1/platform/organizations/{id}`  
- Templates: `GET /api/v1/catalog/templates`  
- Import: `POST /api/v1/pos/catalog-imports/template` (existing)

## Commercial rules

Unchanged: Starter/Pro Subscribe Now; Business 14-day trial + Subscribe Now; one trial per org/product; no automatic charge at trial expiry; paid activation requires confirmed payment.

Business setup and product import must not change plan/entitlement.

## Explicit exclusions

- No MAUI visual copy  
- No offline onboarding  
- No device-auth PWA policy change  
- No forced wizard for pre-existing organizations  
- No new giant distributed transaction wrapping Start Business + onboarding

## Related docs

- [POS-REACT-RMAP-22G-start-business-subscription.md](../Mobile-React/Reports/POS-REACT-RMAP-22G-start-business-subscription.md) — commercial Start Business (pre-onboarding)
- This report is the authoritative post-subscription onboarding addendum for React
