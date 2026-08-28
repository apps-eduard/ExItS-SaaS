# POS-REACT-DESKTOP-FREEZE-AUDIT-01

## Status

PASS (root causes identified and fixed on `feat/organization`)

## START_SHA

`f4ae4f7eda1d9ee6365016ccb7c4a21f517273fa`

## REPRO_STEPS

1. Open Organization Web at desktop width (≥900px, e.g. 1440×900).
2. Navigate repeatedly: Home → Sell → Inventory → Catalog → Customers → Orders → More → repeat.
3. Observe progressive slowdown / click deadness within a few cycles; refresh restores temporarily.
4. Repeat the same sequence at phone width (390×844): remains responsive for long sessions.

## DESKTOP_ONLY_COMPONENTS

| Surface | Desktop | Mobile |
|---|---|---|
| App shell sell layout | `height:100dvh; overflow:hidden` via sell-floor shell | No lock |
| Sell cart | Landscape side cart mounted | Bottom sheet when open |
| App top bar workspace | Center workspace switcher (`md+`) | Stacked under brand |
| Product grid | More tiles visible → more image object URLs | Fewer tiles |
| CSS `:has(.sell-floor-root)` (pre-fix) | Expensive ancestor invalidation on every DOM change while Sell is active | Less DOM churn |

No separate Ant Design desktop sidebar/table shell exists in this React client.

## CONSOLE_FINDINGS

Code-path evidence (full-suite noise + WorkspaceProvider):

- `organizations is not iterable` when `listEligibleOrganizations` treated a non-array body as `ok: true`.
- That throw becomes an unhandled rejection → `GlobalRuntimeErrorHost` full-screen overlay (`pointer-events` block) → clicks appear frozen until refresh.
- Desktop feels this sooner because Sell + wider grids create more concurrent work; the overlay itself is viewport-wide on both.

## PERFORMANCE_FINDINGS

- CSS `:has(.sell-floor-root)` on `.app-shell` forced style/layout invalidation of the shell on descendant mutations. Desktop Sell keeps a large product grid + landscape cart, so recalculate-style cost compounded after repeated Sell visits.
- `useCatalogProductImageUrl` depended on the `workspace` object reference. Parent re-renders that allocated a new `{ organizationId, branchId }` re-fetched every visible thumb and allocated new blob URLs (cost scales with desktop tile count).
- `refreshWorkspaces` was recreated whenever the `session` object identity changed, re-running the bootstrap effect and reloading orgs/branches even when `sessionStatus` stayed `authenticated`.

## MEMORY_FINDINGS

- Object URL churn from image hooks (create without stable deps) retained blobs longer under rapid remount.
- Workspace bootstrap Map resets on each unnecessary refresh increased retained query/provider work.

Heap/DOM/listener numeric Chrome Performance Monitor capture was not available in this agent environment; Playwright stress asserted DOM growth bounds and click latency instead (see e2e).

## ROOT_CAUSE

**Primary (confirmed by Playwright stress):** React Query initial failures were escalated to `GlobalRuntimeErrorHost`, which mounted a **full-screen `pointer-events` overlay**. Navigating to a page whose API returned 404 (e.g. `/customers` in the stress path) trapped all clicks — including bottom nav — until refresh. Desktop felt worse because more tabs/wider grids trigger more concurrent queries sooner; the same overlay also appears on phone once a failing route is hit.

**Contributing (desktop-amplified):**

1. CSS `:has(.sell-floor-root)` style invalidation on Sell.
2. Non-array organizations payload → iterable throw → same overlay class of hang.
3. Image hook workspace object identity refetch storms.
4. `refreshWorkspaces` tied to session object identity.

## FIX

1. **Stop auto-escalating query failures** to the global overlay unless `meta.reportGlobalError` (aligned with mutations). Inline `ErrorState` remains the default.
2. **Non-blocking error overlay:** backdrop uses `pointer-events-none`; only the panel captures clicks so shell/nav stay usable if an overlay does appear.
3. Replace `:has()` with router-driven `app-shell--sell-floor`.
4. Normalize `listEligibleOrganizations` + WorkspaceProvider iteration guard.
5. Image hooks depend on primitive ids; harden revoke.
6. `refreshWorkspaces` reads session from a ref.
7. Mount landscape cart panel only when side-cart layout is active.

## BEFORE/AFTER

| Check | Before | After |
|---|---|---|
| Sell shell styling | CSS `:has()` | Explicit class |
| Bad orgs payload | Uncaught throw + overlay | Soft failure / no iterable throw |
| Image hook deps | workspace object | primitive ids |
| Workspace refresh | session object identity | sessionStatus-stable |
| Landscape cart | Always mounted (CSS-hidden on phone) | Mounted only for side-cart layout |

## FULL_SUITE_RELATIONSHIP

**RELATED / FIXED (client path):** the recurring `organizations is not iterable` failure mode is the same class of bug as the runtime overlay hang. Client now normalizes payloads and guards iteration. Remaining full-suite mock gaps (tests returning non-array bodies) should now fail closed as `ok: false` instead of throwing.

## VALIDATION

- Unit: `list-eligible-organizations.test.ts`, `use-catalog-product-image.test.ts`
- E2E: `e2e/desktop-freeze-audit.spec.ts` (desktop + phone stress)
- `npm run typecheck` / `lint` / `test` / `build` (recorded in commit report)
