# MOBILE-REACT-DOC-AMEND-03 — Smart Workspace and Product Launch Context

**Package:** MOBILE-REACT-DOC-AMEND-03  
**Branch:** `docs/mobile-react-foundation`  
**Starting HEAD:** `6fdf69c7596d1d0c23d6eef686c2303825fd333a`  
**Baseline `origin/main`:** `5a9be9417b7a2217227ae93e9280102992861615`  
**Main drift:** none  

**Status:** Documentation amendment only. Implementation **NOT AUTHORIZED**. Merge **NOT AUTHORIZED**.

Does **not** rewrite [MOBILE-REACT-DOC-08-final-closeout.md](MOBILE-REACT-DOC-08-final-closeout.md).

Does **not** implement React, modify MAUI, modify backend, or create product-launch APIs.

---

## Reason

Finalize the Mobile React planning baseline for:

- Organization + Branch workspace selection
- automatic skip when there is only one valid choice
- chooser only when the user has a meaningful choice
- multiple organizations and multiple branches
- future multiple Mobile-capable ExItS products/experiences
- last-used workspace presentation without silent auto-entry
- workspace switching, offline PIN, and shared top-bar context

The Product Owner principle: **do not show a chooser page when there is nothing meaningful to choose.**

---

## Current MAUI / P28 evidence reviewed (read-only)

| Source | What it shows |
|---|---|
| [P28-WP14](../../reports/P28-WP14-unified-organization-branch-workspace-selection.md) | Workspace = Organization + selected Branch; login routing matrix; topbar display-only; burger Switch workspace; cart discard confirm; no silent device rebind |
| [organization-branches-and-fulfillment-locations.md](../../engineering/organization-branches-and-fulfillment-locations.md) | Primary/Main is a real branch; unified `/workspace-select` |
| [client-experience-boundaries.md](../../architecture/client-experience-boundaries.md) | Personal vs Owner vs POS; Start a Business journey; launching entitled products |
| `WorkspaceSelectionService.cs` | `PersonalHome` / `AutoSelect` / `ShowChooser` / `NoAccessibleBranch` |
| `WorkspaceSelectionModels.cs` | Same outcomes; Primary flag on accessible branches |
| `ProductAccessResolver.cs` | Evaluates `PosProductCodes.PinoyBusinessPos` — single-product hard-coding |
| `WorkspaceSelect.razor` | Org-grouped chooser; current-session check; empty state; cart confirm; online-required switch |
| `SignIn.razor` | After auth, AutoSelect binds workspace; otherwise `/workspace-select` |
| `WorkspaceSelectionServiceTests.cs` | 1+1 auto-select; 1 org many branches chooser; 2 orgs chooser; no orgs Personal; no Active branches empty |

**Not modified.** Those files remain current implementation evidence.

---

## Smart skip / chooser policy

MOBILE-D-065:

| Valid authorized choices | Client |
|---|---|
| Exactly one workspace or product | Skip the corresponding chooser; auto-enter |
| More than one | Show chooser |
| Zero | Explicit setup/access/Personal state — do not invent context |

Recommended order: identity → Personal vs organization → workspace → launchable Mobile product → destination. Skip empty intermediate screens.

---

## Primary / Main rule

MOBILE-D-066: Workspace remains **Organization + Branch**.

An organization with only its Primary/Main branch has **one** valid workspace and auto-selects when that is the sole authorized choice.

Do not treat “no additional branches” as “zero branches.” Do not invent a branch when no accessible Active branch exists.

---

## Multi-org / multi-branch behavior

| Case | Outcome |
|---|---|
| A — 1 org, 1 Active branch | Auto-select |
| B — 1 org, 2+ Active branches | Workspace chooser |
| C — 2+ orgs | Unified chooser grouped by organization |
| D — membership, 0 accessible Active branches | Empty/setup state |
| E — no eligible organization | Personal Home |

Cashier/device constraints that leave only one valid operational workspace also auto-enter. Do not present branches the user/device cannot enter. Wrong device branch: blocked explanation, no silent rebind.

---

## Last-used behavior

MOBILE-D-067: When multiple authorized workspaces exist, show the chooser. Current / last successfully used **may** be highlighted.

**Must not** silently auto-enter a previously used branch solely because it was last used.

Workspace switch with a non-empty cart requires explicit confirmation (Continue sale / Discard and switch). Do not move a cart across organization/branch.

---

## Offline PIN behavior

MOBILE-D-068: Offline PIN does not authorize arbitrary workspace switching.

Cold start offline: enrolled user → PIN → valid grant restores **grant-bound** context only; otherwise online authentication is required.

Switch workspace while offline: stay in current workspace; Internet-required persistent explanation (AMEND-01). Do not clear grant, pending work, cart, PIN, or enrollment.

When online validation later succeeds, **server revocation wins**. Do not keep a stale last-used workspace. Do not silently fall back to another branch for financial operations.

---

## Future product-aware behavior

MOBILE-D-069: Distinguish store catalog products from **ExItS SaaS products/experiences**.

| Launchable Mobile experiences | Outcome |
|---|---|
| Zero | Remain in Personal/Organization or access state; do not offer the product |
| One | Auto-launch; no product chooser |
| More than one | Product chooser |

Current Pinoy Business POS hard-coding is evidence, not a generic future contract. Hypothetical Product B/C labels are architectural examples only. Do not add PLM or parked products unless separately authorized.

Do not redefine Workspace as Organization + Branch + Product.

Product eligibility is derived from authorized capabilities. This amendment does **not** invent backend product-launch APIs; inspect API shape at implementation time if current contracts are not generic.

---

## Shared AppTopBar integration

MOBILE-D-070: One shell/AppTopBar family (MOBILE-D-062). It displays organization / branch / product from **centralized application state**.

Switch workspace / Switch product appear only when meaningful alternatives exist.

Even a single-branch org may show `ABC Grocery` / `Main` as context. Do not show a meaningless dropdown when there is no alternative.

Pages and the top bar must not independently query authorization or rebuild switchers.

---

## Decisions D-065 through D-070

| ID | Status |
|---|---|
| MOBILE-D-065 | **Accepted** — smart skip / chooser / explicit zero-choice |
| MOBILE-D-066 | **Accepted** — Workspace = Org + Branch; Primary/Main-only auto-select; no invented branch |
| MOBILE-D-067 | **Accepted** — last-used highlight, not silent auto-entry; cart/context-safe switch |
| MOBILE-D-068 | **Accepted** — offline PIN grant-bound; switch online-required; server revocation wins |
| MOBILE-D-069 | **Accepted** — product-aware launch; POS hard-coding is evidence |
| MOBILE-D-070 | **Accepted** — shared AppTopBar context; adaptive switch actions |
| MOBILE-D-060 | Remains **Open** |

---

## Documents amended

- [product-surfaces-and-ux.md](../product-surfaces-and-ux.md) — routing matrix, product launch, AppTopBar, failure states, component plan
- [frontend-architecture-and-reuse.md](../frontend-architecture-and-reuse.md) — resolver/chooser components; centralized context
- [offline-sync-auth-and-security.md](../offline-sync-auth-and-security.md) — grant-bound offline workspace; revocation
- [migration-testing-and-implementation-gates.md](../migration-testing-and-implementation-gates.md) — future validation cases (not implemented)
- [current-state-and-replacement-boundaries.md](../current-state-and-replacement-boundaries.md) — MAUI routing/product-code evidence pointer
- [decisions.md](../decisions.md)
- [documentation-status.md](../documentation-status.md)
- [README.md](../README.md)
- `FILE-MANIFEST.md`

Contradiction resolved in DOC-02 selling workflow: it no longer reads as if a workspace chooser is always the first tap.

Historical reports (DOC-08, AMEND-01, AMEND-02) were **not** rewritten as if they originally contained D-065–D-070.

---

## Explicit non-authorizations

| Item | Status |
|---|---|
| React implementation | **NOT AUTHORIZED** |
| PWA | **NOT AUTHORIZED** |
| Capacitor | **NOT AUTHORIZED** |
| MAUI retirement | **NOT AUTHORIZED** |
| MAUI / backend / migrations / product-launch APIs | **Unchanged / not implemented** |
| Merge | **NOT PERFORMED** |

Queue: **STOPPED FOR PRODUCT OWNER + CHATGPT FINAL REVIEW**
