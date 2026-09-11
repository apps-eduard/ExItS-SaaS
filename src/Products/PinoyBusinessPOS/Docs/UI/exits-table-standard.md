# ExItS Table Standard (PinoyBusinessPOS React)

**Status:** Authoritative UI contract
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`
**Implementation authority:** `ExItS.PinoyBusinessPOS.React/src/components/exits/ExitsTable.tsx`
**Approved visual reference:** Incoming Order detail table
(`ExItS.PinoyBusinessPOS.React/src/features/purchasing/IncomingOrderDetailPage.tsx`)

The approved Incoming Order / PO table implementation is the **canonical visual reference**.

Future PinoyBusinessPOS React tables **MUST** reuse the existing **ExitsTable** family rather than invent separate table styling or components, unless a task **explicitly authorizes** an exception.

This standard evolves with the shared component contract. Do **not** pin the standard permanently to a commit SHA.

---

## Ownership split

### Foundation owns

- Layout and styling
- Alignment
- Responsive / mobile presentation
- Density token application
- Presentation states (selected, interactive, busy)
- Toolbar / control placement
- Callback and prop contracts

### Page owns

- Data fetching and React Query
- Permissions
- Domain / business rules
- Search, filter, sort, selection, and pagination **state**
- Export dataset construction
- Mutations and inline-edit behavior

---

## Canonical component family

Reuse these existing primitives. Do **not** duplicate them.

| Component | Role |
|-----------|------|
| `ExitsTableContainer` | Outer bordered shell |
| `ExitsTableToolbar` | Search / filter / selection / output slots |
| `ExitsTable` | Desktop table scroll + `<table>` |
| `ExitsTableHeader` | `<thead>` |
| `ExitsTableHead` | Column header (optional sortable) |
| `ExitsTableBody` | `<tbody>` |
| `ExitsTableRow` | Row (optional `interactive` / `selected`) |
| `ExitsTableCell` | Cell (`cellAlign`, optional `emphasis`) |
| `ExitsTableFooter` | Totals / summary footer |
| `ExitsTableMobile` | Mobile list presentation |
| `ExitsTablePagination` | Range, page size, prev/next |
| `ExitsTableOutputActions` | CSV / XLSX / PDF / Print |
| `ExitsFileFormatIcon` | Format badge icons (`csv` / `xlsx` / `pdf`) |

Related presentation helpers already used by the pilot (not a second table family):

- `ExitsTableCheckbox` — selection column presentation
- Lucide `Printer` — print action icon

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
| **Header** | Optional select-all checkbox; column labels; sort indicators when enabled |
| **Body** | Rows |
| **Footer** | Totals / summaries when applicable |

### Bottom bar (`ExitsTablePagination`)

| Zone | Contents |
|------|----------|
| **LEFT** | Result range / count |
| **MIDDLE** | Rows per page |
| **RIGHT** | Pagination (Previous / page / Next) |

### Conceptual layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Search........................   Filter                  CSV XLSX PDF Print  │
├─────────────────────────────────────────────────────────────────────────────┤
│ ☐ Product       SKU               Quantity ↕      Unit cost ↕   Total ↕     │
├─────────────────────────────────────────────────────────────────────────────┤
│ rows                                                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                         Summary / Order total               │
├─────────────────────────────────────────────────────────────────────────────┤
│ 1–25 of N            Rows per page [25]              Previous  1  Next      │
└─────────────────────────────────────────────────────────────────────────────┘
```

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

### Alignment (`cellAlign`)

| Content | Align |
|---------|--------|
| Text / name | left (`text`) |
| SKU / code | left (`text`) |
| Status | contextual left / center |
| Quantity | right (`numeric`) |
| Money | right (`money`) |
| Percentage | right (`numeric`) |
| Actions | right (`actions`) |
| Checkbox | center (`center`) |

Numeric / money cells use **tabular-nums**.

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

## Density (locked)

Authoritative density names (root preference system):

- `compact`
- `balance`
- `comfort`

Use existing `--exits-table-*` CSS variables.
Do **not** introduce alternate table density names.
Table controls inherit density from the existing root preference system.

---

## Responsive rule (locked)

| Viewport | Presentation |
|----------|--------------|
| Desktop / tablet | Normal aligned `ExitsTable` |
| Mobile | Existing `ExitsTableMobile` presentation |

Do **not** force unreadable desktop columns onto phone width.
Do **not** invent a second mobile table design.

Typical mobile row:

```
Apple                              ₱900.00
PH-FRU-APPLE
5 kg × ₱180.00
```

Toolbar controls may stack.
Output Actions collapse to a single **Export & Print** menu that calls the **same** handlers as the desktop icons.

---

## Official Cursor vocabulary

Terms below are exact. Explicit task ON/OFF instructions override `FULL TABLE` / `SIMPLE TABLE` defaults.

### EXITS TABLE

Use the canonical existing ExitsTable foundation and approved visual style.

Does **not** automatically enable every optional feature.
Never create a new table visual language when this phrase is used.

### FULL TABLE
### FULLY IMPLEMENT THE TABLE

Enable all **applicable** standard capabilities:

- Search
- Filter
- Sorting
- Multi select
- Selection status
- Bulk-action area (when applicable)
- Output icons
- Result count
- Page size
- Pagination
- Loading state
- Empty state
- Error state
- Responsive / mobile
- Density / theme support

Footer / summary is enabled when the domain/page has a meaningful summary.
Sticky header may be enabled when useful for long tables.
**Inline edit is NOT automatically enabled** (page/domain behavior).

Explicit ON/OFF overrides `FULL TABLE` defaults.

Example: `FULL TABLE` + `MULTI SELECT OFF` → everything appropriate except multi select.

### SIMPLE TABLE

Canonical ExitsTable visuals with:

- Header / Body
- Responsive / mobile
- Theme
- Density
- Alignment

Optional footer when requested.

**OFF by default:** search, filter, sort, multi select, output icons, page size, pagination.

### SEARCH ON / OFF

**ON:** Standard toolbar Search (LEFT).
Server-side search preferred for pageable / large datasets; client-side acceptable for small bounded / detail datasets.
**OFF:** Do not render Search.

### FILTER ON / OFF

**ON:** Standard Filter position beside Search (MIDDLE). Filters must reflect real page/domain data — do not invent meaningless filters.
**OFF:** Do not render Filter.

### SORT ON / OFF

**ON:** Canonical sortable `ExitsTableHead`. Cycle: **none → ascending → descending → none**. Subtle sort icons. Page/query owns sort logic.
**OFF:** Non-sortable headers.

### MULTI SELECT ON

Means:

- Checkbox column
- Row checkbox
- Header select-all for **visible** rows
- Selected-row presentation
- “N selected”
- Clear selection
- Bulk-action area when applicable

Selection state belongs to the page.

### MULTI SELECT OFF

**Remove / hide:**

- Row checkboxes
- Select-all checkbox
- Selected-row selection treatment
- Selected count
- Clear selection
- Bulk-action selection area

Do **not** remove normal clickable / interactive row behavior.

### OUTPUT ICONS ON

Use `ExitsTableOutputActions` on the toolbar **RIGHT**.

| Action | Icon | Tooltip / aria-label |
|--------|------|----------------------|
| CSV | `ExitsFileFormatIcon` `csv` | Export CSV |
| XLSX | `ExitsFileFormatIcon` `xlsx` | Export Excel |
| PDF | `ExitsFileFormatIcon` `pdf` | Export PDF |
| Print | Lucide `Printer` | Print |

Desktop: four compact individual icons.
Mobile: single **Export & Print** menu (same handlers).
Do **not** create alternate output button designs.

### OUTPUT ICONS OFF

Do not render `ExitsTableOutputActions`.

### PAGE SIZE ON

Canonical options: **10 / 25 / 50 / 100**
Default: **25**
Maximum standard: **100**

Do **not** add `All`, `200`, `250`, or `500` unless explicitly authorized for a specific workflow.

### PAGE SIZE OFF

Hide the rows-per-page selector.
Does **not** automatically disable pagination unless also requested.

### PAGINATION ON

Use `ExitsTablePagination`: result range, page navigation, and page-size selector if `PAGE SIZE ON`.
Prefer server-side pagination for large / pageable datasets.

### PAGINATION OFF

No pagination controls. Render the bounded dataset supplied by the page.

### FOOTER ON / OFF

**ON:** Use `ExitsTableFooter` for meaningful totals/summary. Do not create a separate Total card when the total naturally belongs to the table.
**OFF:** No table footer summary.

### INLINE EDIT ON / OFF

**ON:** Use existing ExitsTable row/cell presentation. Page owns edit state, inputs, validation, Save/Cancel, and API mutation. Do **not** create a separate editable-table design.
**OFF:** Read-only cells (default unless requested).

---

## Output action semantics (locked)

Canonical component name: **`ExitsTableOutputActions`**
User shorthand: **Output icons**

Formats: **CSV / XLSX / PDF / Print**

### Export scope

| Selection | Scope |
|-----------|--------|
| **None** | Full matching filtered/sorted result set — **not** only the current pagination page |
| **One or more** | Selected matching records only |

For server-side datasets: do **not** fetch huge datasets into React merely for export; use a suitable server-side export/query strategy.
Page/domain controls permissions and data exposure.

Authoritative business totals (e.g. PO `order.totalAmount`) must **not** be replaced by filtered or selected line sums. When exporting a selection, label any selection sum separately (e.g. “Selected lines total”).

### Output icon visuals (locked)

| Format | Treatment |
|--------|-----------|
| CSV | Neutral file outline + subtle CSV (teal/green) accent on badge |
| XLSX | Neutral spreadsheet/file outline + subtle green XLS/XLSX accent |
| PDF | Neutral file outline + subtle red PDF accent |
| Print | Neutral Printer icon |

Color enhances recognition only. Resting buttons stay subtle/neutral.

Do **not** use: strongly colored full buttons, emoji, external images, brand icon packages, or four generic Download icons.

---

## Page size standard (locked)

| Constant | Value |
|----------|--------|
| `DEFAULT_PAGE_SIZE` | `25` |
| `PAGE_SIZE_OPTIONS` | `10`, `25`, `50`, `100` |
| `MAX_PAGE_SIZE` | `100` |

Page size affects the **displayed page only**. It must **not** change:

- Authoritative totals
- Export-all matching scope
- Business calculations

---

## Precedence

1. **Explicit task instruction**
2. **ExitsTable standard defaults** (`FULL TABLE` / `SIMPLE TABLE` / ON-OFF vocabulary)
3. **Page’s existing presentation**

Examples:

- `FULL TABLE, MULTI SELECT OFF` → multi select OFF
- `EXITS TABLE, OUTPUT ICONS ON` → canonical visuals + only the capabilities explicitly requested (plus any implied by those requests)
- `Keep existing server pagination` → preserve server behavior while presenting through ExitsTable

Never sacrifice domain correctness merely to match presentation.

---

## Short command examples

### Example 1

```
Convert Customers to ExitsTable.
FULL TABLE.
MULTI SELECT OFF.
OUTPUT ICONS ON.
```

Meaning: canonical style; search/filter/sort/page size/pagination/output enabled; selection disabled.

### Example 2

```
Use ExitsTable.
SEARCH ON.
SORT ON.
PAGE SIZE ON.
MULTI SELECT OFF.
OUTPUT ICONS OFF.
```

### Example 3

```
Convert Categories to ExitsTable.
INLINE EDIT ON.
SEARCH ON.
MULTI SELECT OFF.
```

### Example 4

```
Use ExitsTable.
FULL TABLE.
OUTPUT ICONS OFF.
```

### Example 5

```
Use ExitsTable.
Simple table.
FOOTER ON.
```

---

## Related paths

| Path | Role |
|------|------|
| `ExItS.PinoyBusinessPOS.React/src/components/exits/ExitsTable.tsx` | Shared implementation |
| `ExItS.PinoyBusinessPOS.React/src/features/purchasing/IncomingOrderDetailPage.tsx` | Approved visual reference / pilot |
| `ExItS.PinoyBusinessPOS.React/src/styles/globals.css` | `.exits-table*` / output icon styles |
| `.cursor/rules/pos-react-table-standard.mdc` | Short Cursor rule pointing here |
