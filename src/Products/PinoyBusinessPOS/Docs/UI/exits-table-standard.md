# ExItS Table Standard (PinoyBusinessPOS React)

**Status:** APPROVED / LOCKED (revised)
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`
**Implementation authority:** CURRENT `ExitsTable` family at
`ExItS.PinoyBusinessPOS.React/src/components/exits/ExitsTable.tsx`
**Approved visual reference:** `/ui-standards` → **Tables**
(`ExItS.PinoyBusinessPOS.React/src/features/ui-standards/UiStandardsTablesPanel.tsx`)

This revision incorporates the approved **Actions**, **Edit field menu**, **stealth inline editing**, **Reset**, and **portaled overlay** behaviors into the official ExItS Table Standard.

Future PinoyBusinessPOS React tables **MUST** reuse the existing **ExitsTable** family rather than invent separate table styling or components, unless a task **explicitly authorizes** an exception.

This standard evolves with the shared component contract. Do **not** pin the standard permanently to a commit SHA.

Do **not** create: `EditableTable`, `ActionTable`, `ExitsTable2`, `AdminTable`, or parallel table visual systems.

---

## Ownership split

### ExitsTable foundation owns

- Presentation and layout
- Column alignment (`cellAlign`) and sizing hints (`colSize`)
- Sorting presentation (`ExitsTableHead` sortable)
- Selection presentation (`ExitsTableCheckbox`, selected row)
- Actions layout (`ExitsTableActions`)
- Edit field menu presentation (`ExitsTableEditMenu`)
- Stealth inline editor chrome (`ExitsTableInlineEditor`)
- Portaled dropdown overlay behavior (via shared `DropdownMenu`)
- Responsive / mobile presentation (`ExitsTableMobile`)
- Accessibility conventions (aria-sort, menu roles, editor labels via page)
- Theme and density token application

### Page / domain owns

- Which columns are editable (`ExitsTableEditableField[]` supplied to the menu)
- Row-specific editability and permissions
- Draft values / baseline / dirty comparison
- Business validation rules
- Save mutation / API / React Query
- Authorization, audit, confirmation
- Calculated business values (e.g. line total)
- Inventory and other domain rules
- Search / filter / sort / selection / pagination **state**
- Export dataset construction

---

## Canonical component family

| Component | Role |
|-----------|------|
| `ExitsTableContainer` | Outer bordered shell |
| `ExitsTableToolbar` | Search / filter / selection / output slots |
| `ExitsTable` | Desktop table scroll + `<table>` |
| `ExitsTableHeader` | `<thead>` |
| `ExitsTableHead` | Column header (optional sortable, `cellAlign`, `colSize`, `stickyEnd`) |
| `ExitsTableBody` | `<tbody>` |
| `ExitsTableRow` | Row (`interactive`, `selected`, `editing`, `error`) |
| `ExitsTableCell` | Cell (`cellAlign`, `colSize`, `emphasis`, `truncate`, `stickyEnd`) |
| `ExitsTableFooter` | Totals / summary footer |
| `ExitsTableMobile` / `ExitsTableMobileRow` | Mobile list presentation |
| `ExitsTablePagination` | Range, page size, prev/next |
| `ExitsTableOutputActions` | CSV / XLSX / PDF / Print |
| `ExitsFileFormatIcon` | Format badge icons (`csv` / `xlsx` / `pdf`) |
| `ExitsTableCheckbox` | Selection column presentation |
| `ExitsTableActions` | Compact END-aligned row action cluster |
| `ExitsTableEditMenu` | Pencil → field picker (portal dropdown) |
| `ExitsTableInlineEditor` | Stealth inline editor wrapper |
| `ExitsTableEditableField` | `{ key, label }` page-owned editable column metadata |

---

## Approved desktop layout (locked)

Cursor must **not** arbitrarily relocate standard controls.

### Toolbar

| Zone | Contents |
|------|----------|
| **LEFT** | Search |
| **MIDDLE** | Filter controls; selection status / Clear when applicable |
| **RIGHT** | Output Actions (`ExitsTableOutputActions`) |

### Table

| Region | Contents |
|--------|----------|
| **Header** | Optional select-all; column labels; sort indicators; optional Actions |
| **Body** | Rows (optional Actions / inline edit) |
| **Footer** | Totals / summaries when applicable |

### Bottom bar (`ExitsTablePagination`)

| Zone | Contents |
|------|----------|
| **LEFT** | Result range / count |
| **MIDDLE** | Rows per page |
| **RIGHT** | Pagination (Previous / page / Next) |

---

## Visual rules (locked)

- Tailwind + existing ExItS design tokens
- Subtle outer border; existing app radius
- Muted header background
- Light row separators
- **No** zebra stripes by default
- **No** heavy shadows
- **No** unnecessary nested cards around the table
- Professional compact SaaS / POS appearance

### Column alignment (`cellAlign`) — logical START / END

Use logical alignment for RTL. Implementation props:

| Content | Header | Cell | `cellAlign` |
|---------|--------|------|-------------|
| Text / name | START | START | `text` |
| SKU / code / reference | START | START | `text` |
| Status | START | START | `text` |
| Date / time | START (unless page intentionally differs) | START (unless page intentionally differs) | `text` |
| Quantity | END | END | `numeric` |
| Money / price / cost / total | END | END | `money` |
| Percentage | END | END | `numeric` |
| Numeric count | END | END | `numeric` |
| Checkbox | CENTER | CENTER | `center` |
| Actions | END | END | `actions` |

Numeric / money cells use **tabular-nums**.

### Sortable header alignment (locked)

For END-aligned numeric / money columns, the **label + sort indicator stay together at END**.

Example:

```
                Quantity ↕
               Unit cost ↕
              Line total ↕
```

Sortable controls must **not** force numeric headers back to START.

### Typography hierarchy

| Element | Treatment |
|---------|-----------|
| Header | muted + medium |
| Primary / name | primary foreground (may be medium) |
| SKU / supporting | muted |
| Unit cost | normal / medium — **not** as strong as line total |
| Line total | semibold |
| Grand / authoritative total | bold |

---

## Column sizing (`colSize`) — locked capability

Opt-in sizing hints on `ExitsTableHead` / `ExitsTableCell`. Actual prop values:

| `colSize` | Intent |
|-----------|--------|
| `checkbox` | Compact / fixed selection column |
| `flex` | Flexible name / product column |
| `sku` | Enough width for codes such as `PH-FRU-APPLE` without unnecessary wrap |
| `numeric` | Compact readable quantity / count |
| `money` | Compact readable currency |
| `actions` | Compact / content-sized actions |

**Principles:**

- Sizing is **page/table configuration** — do not hardcode every Product/SKU/Quantity table to one global width.
- Product / name → normally `flex`
- SKU / code → enough width to avoid unnecessary wrapping; long exceptional values may truncate (`truncate`)
- Numeric / money → compact but readable
- Actions / checkbox → compact

Do **not** destroy overall table width for unusual values.

---

## Density (locked)

Authoritative density names (root preference system):

- `compact`
- `balance`
- `comfort`

Use existing `--exits-table-*` CSS variables (including `--exits-table-inline-editor-height`, `--exits-table-action-size`).
Do **not** introduce alternate table density names.
Table controls, Actions, and stealth editors inherit density from the root preference system.

---

## Theme (locked)

Supports light / dark / system via ExItS tokens.
Editing focus uses semantic **Primary**.
Invalid uses semantic **Danger**.
Do **not** hardcode green or red hex for editing chrome.

Future Preferences → Primary Color must recolor selected / focus / Primary editing emphasis without changing Danger / Warning / Success semantics.

---

## Responsive rule (locked)

| Viewport | Presentation |
|----------|--------------|
| Desktop / tablet | Normal aligned `ExitsTable` |
| Mobile | Existing `ExitsTableMobile` presentation |

Do **not** force unreadable desktop columns onto phone width.
Do **not** invent a second mobile table design.
Mobile Actions must remain clear and touch-friendly — do not blindly squeeze the desktop Actions column.

Toolbar controls may stack.
Output Actions collapse to a single **Export & Print** menu that calls the **same** handlers as the desktop icons.

---

## Actions column (locked optional capability)

### ACTIONS ON / OFF

**ON:** Render an END-aligned Actions column using `ExitsTableActions` and locked **Button** Standard controls.
**OFF:** No Actions column.

Actions:

- END aligned
- Compact / density-aware
- No unnecessary wrapping
- RTL-aware (inline-end)
- Accessible names / tooltips

Default dense-row pattern (UI Standards demo):

| Action | Icon | Button |
|--------|------|--------|
| Edit | `Pencil` | ICON ONLY ROUND GHOST |
| More | `MoreHorizontal` | ICON ONLY ROUND GHOST |

Other common actions (page-owned): View (`Eye`), Delete (semantic Danger) — still locked Button Standard.

Do **not** create table-specific button visual systems.

### ACTIONS without INLINE EDIT

Valid:

```
ACTIONS ON
INLINE EDIT OFF
```

Example: View / Print / History / Delete without cell editing.

### Pencil vs More

| Trigger | Purpose |
|---------|---------|
| Pencil | Editable-field menu (`ExitsTableEditMenu`) |
| MoreHorizontal | Non-edit row actions |

Do not merge everything into one giant menu by default.

### Sticky Actions

`stickyEnd` on head/cell is a **supported special-use capability** (candidate for sticky Actions). It is **not** the default for every Actions column. Enable only when the page needs it.

---

## Inline edit (locked optional capability)

### INLINE EDIT ON / OFF

**Default: OFF** unless explicitly requested, already approved on the page, or migration preserves existing editing.

**ON:** Use field-menu + stealth editors as below. Page owns drafts, validation, save, permissions.
**OFF:** Read-only cells.

`FULL TABLE` alone must **not** automatically make business data editable.

### MULTI SELECT independence

`MULTI SELECT` and `INLINE EDIT` are independent. Any ON/OFF combination is valid.

### Edit action opens field menu (locked)

Pencil does **not** immediately turn every editable cell into an editor.

Pencil → `ExitsTableEditMenu` (portaled dropdown):

```
Edit field
────────────
SKU
Quantity
Unit cost
────────────
Edit all
```

Menu contents come from page-supplied `ExitsTableEditableField[]` (`key` + human `label`).
Do **not** hardcode SKU / Quantity / Unit cost into the foundation — those are UI Standards demo fields.

### Editable column configuration (locked)

Page supplies:

```ts
type ExitsTableEditableField = {
  key: string;   // stable id — not shown to users
  label: string; // human header / menu label
};
```

Example concepts (page-owned, not foundation hardcodes):

| Column | Editable |
|--------|----------|
| Product | no |
| SKU | yes |
| Quantity | yes |
| Unit cost | yes |
| Line total | no (calculated) |
| Actions | no |

### Row-specific editability

Pages may omit Pencil / pass empty `fields` when a row is not editable (e.g. posted vs draft).
ExitsTable does **not** decide domain status rules.

### Menu labels

Use human-facing titles (`Unit cost`), never property names (`unitCost`).

### Edit all (locked)

Last menu item after a separator.
Activates **all currently permitted / configured** editable fields for that row.
Must **not** bypass permissions, enable calculated/read-only columns, or invent fields.

### Single-field edit (locked)

Choosing one menu item activates **only** that field’s stealth editor.
Other cells remain normal table content.

### Stealth inline editor (locked)

`ExitsTableInlineEditor` (default `stealth`):

- Same table typography / font size
- Density-aware compact height (`--exits-table-inline-editor-height`)
- Subtle 1px border; restrained radius; minimal padding
- Correct START / END alignment
- Stable column width; nearly unchanged row height
- Transparent / near-transparent background
- Must still read as a **table row**, not a form dropped into a table

Quantity pattern: `[value] UOM` (UOM outside the numeric editor, END group).
Unit cost pattern: `₱ [amount]` (currency prefix outside editor, END group).
SKU: START-aligned stealth text editor.

### Editor focus (locked)

When the user selects a field from the Edit menu, that editor **receives focus automatically** (UI Standards / recommended page pattern). Do not require an extra click before typing.

### Focused / invalid visuals (locked)

| State | Treatment |
|-------|-----------|
| At-rest editing | Subtle editable border |
| Focused | Stronger semantic Primary border / soft focus |
| Invalid | Semantic Danger border; accessible error association |

### Row height / column stability (locked)

Display → edit must **not** cause dramatic row-height change, column resize, footer movement, or table geometry change.

### One row editing (recommended / current demo)

UI Standards edits **one row at a time** (`editingId`). Treat single active edit row as the default ExItS recommendation. Do not invent multi-row spreadsheet editing unless explicitly authorized.

### Read-only cells (locked)

Inline edit does **not** mean every cell is editable. Pages determine editable columns.
UI Standards demonstrates Product as read-only.

### Calculated cells (locked)

Example: Line Total remains **read-only** presentation.
While Quantity and/or Unit cost drafts are active, Line Total may **preview** the draft calculated value.
Never place Line Total inside an input.

### Inventory boundary (locked)

UI Standards Quantity is **demo data**.
`INLINE EDIT` Quantity does **not** authorize overwriting real inventory on-hand.
Production stock changes require audited domain workflows (receiving, adjustment, transfer, sale, waste/loss, etc.).
ExitsTable is presentation infrastructure only.

---

## Save (locked)

While editing, Actions show:

| Control | Icon | Button |
|---------|------|--------|
| Save | `Check` | ICON ONLY ROUND SUCCESS |
| Reset | `RotateCcw` | ICON ONLY ROUND GHOST + quiet Danger foreground (`.exits-table__action-reset`) |

Save:

- Validates through **page-owned** rules (only active editable fields in the UI Standards demo)
- Invokes page-owned save (local state in UI Standards; API in production)
- Exits editing on success (current UI Standards behavior)

Accessible name example: `Save Apple changes`.

Do **not** create `TableSaveButton`.

---

## Reset (locked) — not Cancel

The product owner replaced inline-edit **Cancel** with **Reset**.

### Semantics

| Term | Meaning |
|------|---------|
| **RESET** | Revert draft value(s) to last committed / original baseline |
| **CANCEL** | Abandon / close an operation |

They are **not** synonyms.
Do **not** document CircleX Cancel as the canonical inline-edit revert action.
Do **not** substitute CircleX Cancel in future table migrations unless the user/page explicitly requests Cancel.

### Icon / button

- Icon: **`RotateCcw`**
- Button: locked ghost / icon-round + `.exits-table__action-reset` (Danger-colored icon, quiet surface — **not** solid Danger)
- Accessible name example: `Reset Apple to original`
- Tooltip: `Reset`

Do **not** create `TableResetButton` / `InlineResetButton`.

### Actual CURRENT UI Standards behavior (authority)

1. On edit start, page snapshots baseline (`sku`, `qty`, `unitCost` drafts).
2. **Reset** restores **all** current draft fields from that baseline (single-field or Edit all).
3. If drafts already match baseline → Reset **exits** edit mode (mouse-friendly leave without Save).
4. If drafts differed → Reset restores originals and **remains in editing**.
5. Escape also exits without saving (page handler).
6. No separate dirty-state disable of Save/Reset buttons in CURRENT demo (both remain available while editing).
7. After Restore-while-editing, focus is not specially re-choreographed beyond normal DOM focus.

Document implementation truth — do not invent extra dirty UX during lock.

---

## Edit menu portal / overlay (locked)

`ExitsTableEditMenu` uses the shared `DropdownMenu` with **`portal` (default true)** → `document.body` fixed overlay.

Required:

- Not clipped by `.exits-table-scroll`, sticky regions, Card overflow
- Opening menu must **not** create table scrollbars or change table geometry
- Anchored to Pencil; `align="end"` (logical); collision padding (~10px); flip above when needed; clamp to viewport
- z-index below dialogs, above sticky Actions (`z-index: 70` in current dropdown)
- Short menus display fully; long field lists may scroll **inside the menu**, never by expanding the table
- Escape / outside click closes; Arrow navigation on menu items
- Sticky Actions compatible (portal escapes sticky stacking/clip)

On scroll of scrollports / resize: menu repositions (or closes if trigger leaves viewport).

Do **not** install another dropdown library.

---

## Validation presentation (locked)

- Invalid stealth editor: Danger border (`invalid` / error on `ExitsTableInlineEditor`)
- Prefer compact indication; avoid giant in-cell error paragraphs that destroy row height
- UI Standards may show a restrained **row-level** validation region beneath the editing row
- Errors must remain accessible (`aria-invalid`, `aria-describedby` / `role="alert"`)

### Saving presentation

If the page shows a saving state, disable editors and use locked Button busy/disabled patterns.
Production owns async mutation; table owns presentation only.
UI Standards may include a static “Saving” sample — no new saving mechanics required for lock.

---

## Footer alignment (locked)

Totals must remain aligned beneath the corresponding numeric / money column.

Example: Order Total amount aligns with Line Total values.
Actions column must **not** offset the total geometry.

---

## Mobile inline edit (locked)

On mobile (`ExitsTableMobile`):

- Same Edit field menu concept (labeled Edit trigger allowed)
- Chosen field becomes a compact stacked editor; others stay display
- Edit all → compact stacked form for configured fields
- Calculated / read-only remain display
- Labeled **Reset** / **Save** (RotateCcw / Check) for clarity
- No horizontal desktop editor squeezing

---

## Accessibility (locked)

| Concern | Convention |
|---------|------------|
| Sort | `aria-sort` on sortable heads |
| Actions | Accessible names (e.g. `Edit Apple`) |
| Edit menu | `role="menu"`, menuitems, Escape, ArrowUp/Down, focus to first item |
| Editors | Accessible labels (Input `label` / aria-label); errors associated |
| Save | Row-specific aria-label |
| Reset | Row-specific aria-label |

---

## RTL (locked)

- Logical START / END via CSS (`text-align: start|end`, `inset-inline-end` for sticky)
- Actions at inline-end
- Portaled menus use logical end alignment relative to trigger direction
- No hardcoded LTR layout hacks

---

## Official Cursor vocabulary

Terms below are exact. Explicit task ON/OFF instructions override `FULL TABLE` / `SIMPLE TABLE` defaults.

### EXITS TABLE

Use the canonical existing ExitsTable foundation and approved visual style.
Does **not** automatically enable every optional feature.

### FULL TABLE / FULLY IMPLEMENT THE TABLE

Enable all **applicable** standard capabilities:

- Search, Filter, Sorting, Multi select (when applicable)
- Selection status / Clear
- Output icons, Result count, Page size, Pagination
- Loading / Empty / Error states
- Responsive / mobile
- Density / theme

Footer when domain has a meaningful summary.
Sticky header when useful for long tables.

**ACTIONS** and **INLINE EDIT** remain **optional** — `FULL TABLE` does **not** auto-enable business editing.

### SIMPLE TABLE

Header / Body / responsive / theme / density / alignment.
Optional footer when requested.
**OFF by default:** search, filter, sort, multi select, output icons, page size, pagination, Actions, inline edit.

### SEARCH / FILTER / SORT / MULTI SELECT / OUTPUT ICONS / PAGE SIZE / PAGINATION / FOOTER

Unchanged from prior locked meanings (see historical sections below for detail).

### ACTIONS ON / OFF

**ON:** Actions column per this standard.
**OFF:** No Actions column.

### INLINE EDIT ON / OFF

**ON:** Field-menu + stealth editors + Save / Reset per this standard.
**OFF:** No inline editing (default unless explicit).

### EDIT MODE: FIELD MENU

Canonical edit mode. Pencil opens the field picker (not immediate multi-cell editors).

### EDITABLE: \<columns\>

Page-configured human labels / keys for the Edit menu.
Example: `EDITABLE: SKU, QUANTITY, UNIT COST`

### EDIT ALL

Final menu action after separator — all permitted configured editable fields.

### ROW EDIT

Preferred general inline-edit framing: editing happens in the context of a row (field menu → selected cells).

### CELL EDIT

Special dense / data-management use (e.g. double-click single cell). Prefer field-menu row edit for general cases. Documented as supported special pattern in UI Standards — not the default Edit path.

### STICKY ACTIONS

Supported via `stickyEnd` — special use / not default.

### RESET

Canonical inline-edit revert terminology (`RotateCcw`). Do **not** substitute Cancel unless explicitly requested.

---

## Output action semantics (locked)

Canonical component: **`ExitsTableOutputActions`**
Formats: **CSV / XLSX / PDF / Print**

| Selection | Scope |
|-----------|--------|
| **None** | Full matching filtered/sorted result set — **not** only the current page |
| **One or more** | Selected matching records only |

For server-side datasets: use server export strategy; do not fetch huge datasets into React merely for export.

---

## Page size standard (locked)

| Constant | Value |
|----------|--------|
| Default | `25` |
| Options | `10`, `25`, `50`, `100` |
| Max standard | `100` |

Page size affects the **displayed page only**. It must **not** change authoritative totals, export-all matching scope, or business calculations.

---

## SEARCH / FILTER / SORT / MULTI SELECT detail (locked)

### SEARCH ON / OFF

**ON:** Toolbar Search (LEFT). Server-side preferred for large datasets.
**OFF:** Do not render Search.

### FILTER ON / OFF

**ON:** Filter beside Search (MIDDLE). Real domain filters only.
**OFF:** Do not render Filter.

### SORT ON / OFF

**ON:** Sortable `ExitsTableHead`. Cycle: **none → ascending → descending → none**.
**OFF:** Non-sortable headers.

### MULTI SELECT ON

Checkbox column, row checkbox, header select-all for **visible** rows, selected presentation, “N selected”, Clear, bulk area when applicable.

### MULTI SELECT OFF

Remove selection chrome only — keep normal interactive row behavior.

### OUTPUT ICONS ON / OFF

**ON:** `ExitsTableOutputActions` toolbar RIGHT.
**OFF:** Do not render.

### PAGINATION ON / OFF / PAGE SIZE ON / OFF / FOOTER ON / OFF

As previously locked: `ExitsTablePagination`; page size selector independent of pagination hide unless both requested.

---

## Precedence

1. **Explicit task instruction**
2. **ExitsTable standard defaults** (`FULL TABLE` / `SIMPLE TABLE` / ON-OFF vocabulary)
3. **Page’s existing presentation**

Never sacrifice domain correctness merely to match presentation.

---

## Short command examples

### Example 1

```
EXITS TABLE
FULL TABLE
ACTIONS ON
```

Canonical full table + Actions column. Inline edit still OFF unless requested.

### Example 2

```
EXITS TABLE
ACTIONS ON
INLINE EDIT ON
EDIT MODE: FIELD MENU
EDITABLE: SKU, QUANTITY, UNIT COST
```

Pencil menu: SKU, Quantity, Unit cost, Edit all. Save / Reset while editing.

### Example 3

```
EXITS TABLE
MULTI SELECT OFF
ACTIONS ON
INLINE EDIT ON
EDITABLE: PRICE
```

No checkboxes; Actions on; Price via field-menu workflow.

### Example 4

```
EXITS TABLE
ACTIONS ON
INLINE EDIT OFF
```

Row actions without cell editing.

### Example 5

```
EXITS TABLE
INLINE EDIT OFF
```

No inline editing even if Actions exists.

### Example 6

```
Convert Customers to ExitsTable.
FULL TABLE.
MULTI SELECT OFF.
OUTPUT ICONS ON.
```

---

## UI Standards reference

`/ui-standards` → **Tables** is the human visual authority for the approved implementation, including:

- Full table
- Alignment
- Actions
- Edit field menu (portaled)
- Field edit / Edit all
- Stealth editors + auto-focus
- Save / Reset
- Validation
- Responsive / mobile samples
- Cursor shorthand cheatsheet

Mark UI Standards table wording **APPROVED / LOCKED** (not pilot / extension).

---

## Related paths

| Path | Role |
|------|------|
| `ExItS.PinoyBusinessPOS.React/src/components/exits/ExitsTable.tsx` | Shared implementation |
| `ExItS.PinoyBusinessPOS.React/src/features/ui-standards/UiStandardsTablesPanel.tsx` | Approved visual reference |
| `ExItS.PinoyBusinessPOS.React/src/components/ui/dropdown-menu.tsx` | Portaled overlay primitive |
| `ExItS.PinoyBusinessPOS.React/src/styles/globals.css` | `.exits-table*` styles |
| `.cursor/rules/pos-react-table-standard.mdc` | Short Cursor rule pointing here |
| `Docs/UI/exits-button-standard.md` | Locked Button Standard (reuse; do not fork) |
