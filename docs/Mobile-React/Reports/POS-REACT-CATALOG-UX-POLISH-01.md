# POS-REACT CATALOG UX POLISH 01 — Products list, PageHeader, edit form

**Status:** COMPLETE  
**Start SHA:** `0d890b7aaca1a4b5ed898d8d5df916e1ecc1a3b8`  
**Implementation commit:** `9ae95c1542bc422c65e399e34c5749add62dfe7b`  
**Branch:** `feat/pos-react-client`

## Delivered

### Global motion and list patterns

- Shared `exits-page`, `exits-list`, `exits-animate-toolbar`, and `exits-animate-panel` CSS for catalog, suppliers, and inventory surfaces
- Staggered list card entrance animations

### Products list (`/catalog`)

- Action chips: New product, Business template, Global catalog, Categories, Today's prices
- Status filter chips (Active / Inactive / All) with pagination (20/page)
- Responsive product grid with price (`formatPeso`) and status chip on the right
- Back navigation to Manager home via `pageBackNav`

### PageHeader standard

- Icon-only info control: hover preview, tap to pin/unpin description
- Back arrow vertically aligned with title row (44px touch target)
- Optional `subtitle` (product name on edit) and `trailing` slot (status chip)

### Edit / create product form

- Section panels: Basics, Pricing & selling, Expiration, Packages, Image (edit only)
- Inline quick-add category inside Basics section
- Sticky action bar: primary Save (icon + spinner), destructive Deactivate, green-outline Reactivate
- `Button` variants: `outline`, `destructive`

### Related catalog surfaces

- Categories list uses global list + `StatusChip`
- Template import steps migrated to `ExitsChipBar`
- Expiring stock and suppliers list aligned to shared motion/chip patterns

## i18n

New keys in en, fil-PH, ceb-PH, ilo-PH, hil-PH:

- `catalog.status*`, `catalog.pageLabel`, `catalog.prevPage`, `catalog.nextPage`
- `catalog.sectionBasics`, `catalog.sectionPricing`, `catalog.sectionExpiration`, `catalog.sectionPackages`, `catalog.sectionImage`, `catalog.sectionCategoryQuickAdd`
- `pageHeader.infoToggle`

## Tests

| Suite | Result |
|-------|--------|
| Vitest full (`npm test`) | 723 passed |
| PageHeader.test.tsx | 6 passed |
| message-parity.test.ts | 10 passed |
| catalog-import.test.tsx | 9 passed |
| Typecheck (`tsc -b`) | PASS |
| Vite production bundle | PASS (PWA precache limit warning on main chunk — pre-existing; bundle emitted) |

## Exclusions

- Backend catalog contract changes
- Today's Prices UX (RMAP-06 scope)
- Image editor parity with MAUI (camera / use-standard flow)
- Deactivate confirmation dialog

## Next

- Optional: split main JS chunk or raise PWA precache limit for production SW
- Continue RMAP catalog parity (UOM/units polish, Today's Prices UX)
