# UI Standards page — development only

**Status:** ACTIVE  
**Scope:** `ExItS.PinoyBusinessPOS.React`  
**Route:** `/ui-standards` (`/ui-standard` redirects here)

The UI Standards page is the **Organization Enterprise Hybrid Glass** live reference (five authenticated workflow pages). Component lock playgrounds remain in-repo for unit tests and lock files but are no longer showcase navigation. It is **not** a product feature. It exists only when the app runs with the Vite dev server (`npm run dev`) or under Vitest.

It is **not** included in:

- production builds (`npm run build`, `vite build`, the `Dockerfile.pos-react` image)
- preview builds (`npm run preview`, `vite preview`, the Playwright web server, any public preview host)

## How it is kept out

| Part | Where | Production / preview |
| --- | --- | --- |
| Route and page | `src/app/router.tsx` registers `/ui-standards` and `/ui-standard` only when `import.meta.env.DEV` is true, and loads the page with a lazy `import()` | Vite replaces `import.meta.env.DEV` with `false`, so the routes, the page chunk, and everything it imports are removed. `/ui-standards` falls through to the route error page. |
| Showcase CSS | `src/features/ui-standards/ui-standards.css`, imported only by `UiStandardsPage.tsx` | Not bundled, because the page is not bundled. |
| Organization Hybrid Glass CSS | `src/features/ui-standards/org-enterprise-hybrid-glass/org-enterprise-hybrid-glass.css`, imported only by `UiStandardsPage.tsx` | Same DEV tree-shake; never a Personal Soft Glass default. Includes the experimental Background playground (`.org-ehg-bg-lab*`) — local preview only, not production Org pages. See [exits-organization-enterprise-hybrid-glass.md](./exits-organization-enterprise-hybrid-glass.md). |
| Top bar shortcut | `ShellUiStandardsButton` renders only when `import.meta.env.DEV` is true and the mode is local validation | Not rendered. |

Measured on Oct 18, 2026 after this change: the production CSS went from 1,155 KB to 945 KB raw (140 KB to 115 KB gzip), and the main JS bundle no longer contains the page.

## Showcase classes used by product code

A few showcase classes are rendered by product components. Their rules stay in `src/styles/globals.css`:

- `ui-standards-qcheckbox*` — `ExitsCheckbox`
- `ui-standards-qbtn--raised` — Personal businesses actions
- `ui-standards-qcard`, `ui-standards-qcard--settings-card`, `ui-standards-qcard__settings-*` — Personal utang settings card

The locked Primary Outlined input and dropdown (`ext input primary outlined`, `ext dropdown primary outlined`) are used by the Update Balance dialog. Those rules live in `src/features/ui-standards/exits-qfield.css`. The playground stylesheet imports that file, and `ExitsOutlineField` imports it too, so the dialog keeps the lock in production. The rest of the showcase stays out of production.

Every other `ui-standards-*` rule belongs in `ui-standards.css`.

## Rules for contributors

- Do not import `UiStandardsPage`, any `features/ui-standards/UiStandards*` component, or `ui-standards.css` from product code. Product code may import the small lock constant files (`intent-color-global-default.ts` and similar).
- New showcase styles go in `ui-standards.css`, not `globals.css`.
- If a product component needs a showcase look, give it a product class in `globals.css` (or add the showcase class to the product list above and keep its rules in `globals.css`).
- Do not link to `/ui-standards` from product UI except through `ShellUiStandardsButton`.

## Guard test

`src/features/ui-standards/ui-standards-dev-only.test.ts` fails if:

- the router imports the page statically or registers the route outside `import.meta.env.DEV`
- the shell shortcut stops checking `import.meta.env.DEV`
- `globals.css` gains a `ui-standards-*` class that is not on the product list
- anything other than `UiStandardsPage.tsx` imports `ui-standards.css`
- product code imports a `UiStandards*` component

## Verify a build

```powershell
npx vite build
rg -c "ui-standards-layout|ui-standard-btn-pg-preview" dist/assets
```

The search should find nothing.
