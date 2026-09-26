# ExItS UI Standard (POS React)

**Status:** ACTIVE  
**Scope:** `ExItS.PinoyBusinessPOS.React`  
**Compact visual reference:** `/ui-standards` (local-validation / DEV only; `/ui-standard` redirects here)  
**Live samples + catalog:** single canonical page (search + category filters)

This document locks **interaction semantics** so features reuse one set of primitives instead of inventing local Save/Edit/Delete/toast/confirm/drawer/modal patterns.

The corresponding Cursor Project Rules under `.cursor/rules` (`exits-ui-architecture.mdc`, `exits-responsive-data.mdc`, `exits-ui-reuse-interactions.mdc`) enforce these standards for Agent-assisted development.

Related locked standards:

- [exits-button-standard.md](./exits-button-standard.md)
- [exits-table-standard.md](./exits-table-standard.md)
- [exits-responsive-data-view-standard.md](./exits-responsive-data-view-standard.md)
- [exits-tabs-standard.md](./exits-tabs-standard.md)
- [exits-chip-standard.md](./exits-chip-standard.md)
- [exits-motion-standard.md](./exits-motion-standard.md)
- [exits-upload-standard.md](./exits-upload-standard.md)

### Component Playgrounds

Heavy cards (Buttons, Status & Chips, Selects) include a compact **Playground**: live production preview + generated JSX + **Copy snippet** (toast: “Snippet copied”). Props are local demo state only.

### Global Preference Preview

`/ui-standards` shows Auto samples (Button / StatusChip / SearchField) wired to the **real** Preferences store, with optional inline Control Shape / Density / Theme / Motion controls.

### Do / Don’t guidance

A compact Do / Don’t card covers Primary hierarchy, Intent vs Appearance, Soft appearance ≠ Soft shape, and Responsive Data TABLE ↔ LIST.

---

## Canonical components

| Concern | Component | Path |
|--------|-----------|------|
| Button | `Button` | `components/ui/button.tsx` |
| Action icons / intents | `EXITS_ACTIONS` | `components/exits/action-semantics.ts` |
| Table icon actions | `TableActionButton` | `components/exits/TableActionButton.tsx` |
| Toast | `ToastProvider` / `useExitsToast` | `components/exits/ToastProvider.tsx` |
| Confirm | `ConfirmActionDialog` | `components/exits/ConfirmActionDialog.tsx` |
| Entity edit shell | `FormDrawer` | `components/exits/FormDrawer.tsx` |
| Short modal | `ExitsModal` | `components/exits/ExitsModal.tsx` |
| Status | `StatusChip` | `components/exits/StatusChip.tsx` |
| Responsive collections | `ExitsResponsiveDataView` | `components/exits/ExitsResponsiveDataView.tsx` |
| Upload | `ExitsUpload` | `components/exits/ExitsUpload.tsx` |
| Quantity | `QuantityStepper` | `components/exits/MoneyQuantity.tsx` |
| States | `EmptyState` / `LoadingState` / `ErrorState` | `components/exits/` |

`QuantityStepper` **standard** is `variant="outline"`: surface capsule with **primary border** **[ − ][ qty ][ + ]**; radius follows Preferences → Control Shape (`--exits-control-radius`). `variant="auto"` is the solid Primary capsule (white rim). `variant="field"` keeps the primary capsule with a **white/surface center** (input look). See UI Standards → Global Preference Preview. Legacy `variant="default"` form field chrome (`[ neutral − ][ qty ][ primary + ]`) is opt-in only and must not appear under Forms.

Changing a canonical component must update all consumers. Do **not** restyle via page-specific CSS selectors.

---

## Button model (intent ≠ appearance)

Intent/Tone = semantic meaning. Appearance/Treatment = how it is drawn. They compose.

| Intent | Appearance |
|--------|------------|
| Primary · Neutral · Success · Info · Warning · Danger | Solid · Outline · Ghost · Elevated · Gradient |

Elevated / Outline / Ghost / Gradient are **not** intents. Historical “Muted” maps to **Neutral + Solid** (`variant="secondary"` alias). Prefer `intent` + `appearance` (or `getActionButtonStyle`). See [exits-button-standard.md](./exits-button-standard.md).

## Action semantics

Source: `action-semantics.ts` / `action-icons.ts`. Use `getActionButtonStyle(action)`.

| Action | Icon | Intent | Appearance | Notes |
|--------|------|--------|------------|-------|
| Save | `Save` | Primary | Solid | |
| Create / New | `Plus` | Primary when main | Solid | Secondary → Neutral + Outline |
| Add | `Plus` | Primary if main | Solid | Secondary → Neutral + Outline |
| Edit | `Pencil` | Neutral | Outline | |
| Cancel | none | Neutral | Ghost | Outline on some surfaces |
| Delete | `Trash2` | Danger | Outline | Solid / strong inside destructive confirm |
| Deactivate | `CircleOff` | Warning | Outline | Danger only if truly destructive |
| Activate | `CircleCheck` | Success | Outline | Solid when main confirmation |
| View / Preview | `Eye` | Neutral | Outline | |
| Print | `Printer` | Neutral | Outline | |
| Download / Export | `Download` | Neutral | Outline | |
| Retry | `RotateCw` | Neutral | Outline | |
| Search | `Search` | Neutral | Outline | |
| Filter | `SlidersHorizontal` | Neutral | Outline | |
| Close | `X` | Neutral | Ghost | Icon-only utility |

### Primary hierarchy rule

**Prefer one Primary action per action group.**

Primary is determined by **hierarchy in the current action group**, not by the English verb alone.

Good:

- `[ Save ]` Primary+Solid + `[ Cancel ]` Neutral+Ghost
- `[ Create customer ]` Primary+Solid + `[ Add existing ]` Neutral+Outline

Bad:

- `[ Create customer ]` Primary + `[ Add existing ]` Primary

### Button order

| Surface | Order |
|---------|-------|
| Form footer | Cancel \| Save |
| Confirmation | Cancel \| Confirm / Destructive |
| Page header | Secondary first; primary/main visually strongest |

Respect document `dir` (RTL) — flex `justify-end` / logical gaps handle mirroring.

### Table actions

Icon-only controls are allowed in dense rows. They **must**:

- use canonical action icons + intent (`TableActionButton`)
- default row chrome appearance: Ghost (intent still from action semantics, e.g. Edit=Neutral, Delete=Danger)
- provide `aria-label` + tooltip
- keep consistent size / hover / focus (shared `Button` `size="icon"`)
- do not invent separate icon-only color semantics

---

## Toast

API:

```ts
const toast = useExitsToast();
toast.success("Changes saved successfully.");
toast.info("Quotation saved as draft.");
toast.warning("Check is pending clearing.");
toast.error("Payment could not be recorded.");
```

| Variant | Icon | Token treatment |
|---------|------|-----------------|
| SUCCESS | `CircleCheck` | `--exits-success` |
| INFO | `Info` | `--exits-info` |
| WARNING | `TriangleAlert` | `--exits-warning` |
| ERROR | `CircleAlert` | `--exits-danger` |

Do not invent custom colored toast markup on feature pages. `showToast` remains for legacy callers.

---

## ConfirmActionDialog

Variants: `default` | `info` | `warning` | `danger`.

Feature supplies: `variant`, `title`, `description`, `confirmLabel`, `pending`, `onConfirm` / `onCancel`.  
Dialog owns icon, layout, Cancel | Confirm order, Esc, overlay, focus trap, restore focus, busy state.

Legacy `ConfirmationDialog` in `SheetDialog` remains for existing call sites; prefer `ConfirmActionDialog` for new work.

---

## FormDrawer vs Modal vs Page

| Pattern | Use when |
|---------|----------|
| **FormDrawer** | Normal entity create/edit (customer, supplier, org profile, relationship contact, …) |
| **ExitsModal** | Short contextual interaction (record payment, 1–2 field quick edit, short decision) |
| **ConfirmActionDialog** | Destructive / status confirmation |
| **Page** | Large multi-step workflows (sale checkout, quotation editor, setup wizards) |

`EditCreditTermsModal` and `RecordPaymentModal` use **ExitsModal** (short forms).  
Branch access remains an on-page section today (FormDrawer migration deferred — medium UX risk).

---

## Filter control choice (chips vs searchable select)

| Option set | Prefer |
|------------|--------|
| Small / fixed (≈2–8, unlikely to grow) | Segmented control / filter chips (`ExitsChipBar`, `UnderlineTabBar`) |
| Large or growing (categories, branches, staff, products, …) | Searchable `ExitsSelect` / `ExitsMultiSelect` |

Do **not** render every category as a permanent chip row when the org may eventually have dozens or hundreds.

---

## Semantic colors

Use tokens only: `--exits-primary`, `--exits-success`, `--exits-info`, `--exits-warning`, `--exits-danger`, and Neutral appearance surfaces (solid / outline / ghost).

Do not hardcode `green-500` / `red-600` / `amber-500` in feature pages when semantic tokens exist.

---

## Preferences compatibility

Global components must honor:

- Theme: System / Light / Dark
- Control Shape: Standard / Pill / Soft
- Density: Compact / Balance / Comfort
- Motion: System / Reduced
- Primary palettes

Button `shape="auto"` follows Control Shape. Toast / dialog motion respects `prefers-reduced-motion`.

---

## Accessibility

- Focus trap + Esc + restore focus on FormDrawer / ExitsModal / ConfirmActionDialog
- Icon-only controls require `aria-label` (and tooltip where practical)
- Toast region: `aria-live="polite"`
- Confirm: `role="alertdialog"`

---

## Reference page cards (`/ui-standards`)

Exactly **one live card per category**:

1. Buttons
2. Toasts
3. Confirm Dialog
4. Form Drawer
5. Modal
6. Status & Chips
7. Form Controls
8. Upload (dropzone / tile / button)
9. Selects
10. Navigation & Selection
11. Responsive Data View (TABLE ↔ LIST)
12. Cards (types + Customer Purchase Summary invoice sample)
13. States
14. Do / Don’t

Plus the filterable standards catalog table. Data filter also surfaces the locked ExitsTable reference panel.

Samples must exercise **production** components (real toast / dialog / drawer / modal), not static mocks.

Classic / Simple view modes were removed — `/ui-standards` is the only UI Standard page.
