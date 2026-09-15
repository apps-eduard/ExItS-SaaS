# ExItS UI Standard (POS React)

**Status:** ACTIVE  
**Scope:** `ExItS.PinoyBusinessPOS.React`  
**Compact visual reference:** `/ui-standards` (local-validation / DEV only; `/ui-standard` redirects here)  
**Expanded catalog:** same page — Detailed catalog tabs + Classic/Simple views  

This document locks **interaction semantics** so features reuse one set of primitives instead of inventing local Save/Edit/Delete/toast/confirm/drawer/modal patterns.

Related locked standards:

- [exits-button-standard.md](./exits-button-standard.md)
- [exits-table-standard.md](./exits-table-standard.md)
- [exits-tabs-standard.md](./exits-tabs-standard.md)
- [exits-chip-standard.md](./exits-chip-standard.md)
- [exits-motion-standard.md](./exits-motion-standard.md)

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
| States | `EmptyState` / `LoadingState` / `ErrorState` | `components/exits/` |

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

## Reference page cards (`/ui-standard`)

Exactly **one card per category**:

1. Buttons  
2. Toasts  
3. Confirm Dialog  
4. Form Drawer  
5. Modal  
6. Status & Chips  
7. Form Controls  
8. Navigation & Selection  
9. Table  
10. States  

Samples must exercise **production** components (real toast / dialog / drawer / modal), not static mocks.
