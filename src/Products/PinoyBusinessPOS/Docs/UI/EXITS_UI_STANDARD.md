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

## Action semantics

Source: `action-semantics.ts` / `action-icons.ts`.

| Action | Icon | Default intent | Notes |
|--------|------|----------------|-------|
| Save | `Save` | Primary | |
| Create / New | `Plus` | Primary when sole main action | Secondary → Outline |
| Add | `Plus` | Primary if main; Outline if secondary | |
| Edit | `Pencil` | Outline | |
| Cancel | none | Ghost (Outline on some surfaces) | |
| Delete | `Trash2` | Danger | |
| Deactivate | `CircleOff` | Warning | Danger only if truly destructive |
| Activate | `CircleCheck` | Success | |
| View / Preview | `Eye` | Outline | |
| Print | `Printer` | Outline | |
| Download / Export | `Download` | Outline | |
| Retry | `RotateCw` | Outline | |
| Search | `Search` | Outline | |
| Filter | `SlidersHorizontal` | Outline | |
| Close | `X` | Ghost | Icon-only utility |

### Primary hierarchy rule

**Prefer one Primary action per action group.**

Primary is determined by **hierarchy in the current action group**, not by the English verb alone.

Good:

- `[ Save ]` Primary + `[ Cancel ]` Ghost
- `[ Create customer ]` Primary + `[ Add existing ]` Outline

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

- use canonical action icons/intents (`TableActionButton`)
- provide `aria-label` + tooltip
- keep consistent size / hover / focus (shared `Button` `size="icon"`)

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

Use tokens only: `--exits-primary`, `--exits-success`, `--exits-info`, `--exits-warning`, `--exits-danger`, muted/outline/ghost neutrals.

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
