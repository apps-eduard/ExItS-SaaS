# ExItS Responsive Data View Standard

**Status:** APPROVED / PILOT
**Scope:** `ExItS.PinoyBusinessPOS.React`
**Canonical components:**

| Piece | Path |
|-------|------|
| Layout types / helpers | `components/exits/responsive-data-view.ts` |
| Hook | `components/exits/useResponsiveDataLayout.ts` |
| Shell | `components/exits/ExitsResponsiveDataView.tsx` |
| List record | `components/exits/ExitsDataRecordCard.tsx` |
| Desktop table | `components/exits/ExitsTable.tsx` (existing locked family) |

**Visual authority:** `/ui-standards` → **Responsive Data View**

Desktop table rules remain owned by [exits-table-standard.md](./exits-table-standard.md).
This standard decides **when** to stay TABLE vs switch to LIST/CARD — it does **not** invent a second table system.

### UI Standards device preview

`/ui-standards` provides **Desktop / Tablet / Mobile** preview sizes for visual QA of the same `ExitsResponsiveDataView` sample:

| Preview | Width |
|---------|-------|
| Desktop | 1280px |
| Tablet | 834px |
| Mobile | 390px |

These are **design preview widths**, not new application breakpoints. Production still resolves layout from the real viewport via `useResponsiveDataLayout` (omit `layoutWidthPx`). The standards page passes `layoutWidthPx` so TABLE/LIST matches the selected preview even when the browser window is larger or smaller than the frame.

---

## Product decision

**Default:** do **not** force horizontal scrolling on tablet/mobile.

| Viewport | Presentation |
|----------|----------------|
| **Desktop (lg+)** | Full `ExitsTable` |
| **Tablet** | TABLE if the page marks the table as simple (`tableMinWidthPx=768`); otherwise LIST |
| **Mobile** | LIST / record cards by default |

**Exception (opt-in only):** `allowHorizontalScroll` keeps TABLE with overflow-x for genuinely grid-heavy pages. Document that choice on the page — never make it the global default.

---

## Modes

| Mode | When |
|------|------|
| **TABLE** | Enough width (`useMediaMin(tableMinWidthPx)`) or forced / h-scroll exception |
| **LIST** | Narrow viewports — each row → `ExitsDataRecordCard` (or `ExitsTableMobile` for existing demos) |

Optional dense TABLE (hide low-priority columns) may be added later per page; TABLE + LIST is enough for the global standard.

Default `tableMinWidthPx` = **1024** (comfortable multi-column tables).
Use **768** only for simple few-column tables that still read well on tablet.

---

## Column priority

Pages classify columns so LIST mode can map fields without dumping everything equally:

| Priority | Role in LIST |
|----------|----------------|
| `primary` | Title |
| `secondary` | Subtitle / supporting identity |
| `status` | Trailing status chip |
| `metric` | Key-value fields (qty, money) |
| `detail` | Expandable / secondary details |
| `hiddenOnCompact` | Desktop-only |

Types: `ResponsiveColumnMeta` in `responsive-data-view.ts`.

---

## Actions

| Layout | Pattern |
|--------|---------|
| TABLE | Existing `ExitsTableActions` / `TableActionButton` |
| LIST | **One** primary quick action + overflow (`More`) for the rest |

Reuse Button / confirm / danger semantics — do not invent list-only action chrome.

---

## Multi-select

| Layout | Default |
|--------|---------|
| TABLE | Allowed when the page needs it |
| LIST | **OFF** unless the page explicitly opts in |

Helper: `responsiveDataMultiSelectAllowed(layout, pageOptInOnList)`.

---

## Toolbar & pagination

- Toolbar is **optional**. Default Responsive Data View sample has **no search**.
- When a page needs search/filters: **one** shared toolbar for both layouts (wrap; avoid fixed wide rows on mobile).
- **One** shared `ExitsTablePagination` (or equivalent) under both layouts when pagination is ON.
- Empty / loading / error stay page-owned via existing state components.

---

## Horizontal scroll

| Rule | |
|------|--|
| **Default** | No — switch to LIST |
| **Exception** | `allowHorizontalScroll` + `strategy="table"` (or h-scroll flag on the shell) |

---

## Page usage (concept)

```tsx
const { layout } = useResponsiveDataLayout({ tableMinWidthPx: 1024 });

// UI Standards preview only — production omits layoutWidthPx:
// const { layout } = useResponsiveDataLayout({
//   tableMinWidthPx: 1024,
//   layoutWidthPx: 390, // simulate Mobile
// });

<ExitsResponsiveDataView
  layout={layout}
  // toolbar optional — default has no search
  // toolbar={<ExitsTableToolbar search={...} filter={...} />}
  table={<ExitsTableContainer><ExitsTable>...</ExitsTable></ExitsTableContainer>}
  list={
    <ul className="exits-data-record-list">
      {rows.map((row) => (
        <ExitsDataRecordCard
          as="li"
          key={row.id}
          title={row.name}
          subtitle={row.sku}
          status={<StatusChip tone="success">Active</StatusChip>}
          fields={[{ label: "Balance", value: row.balance, emphasize: true }]}
          primaryAction={<TableActionButton action="edit" ... />}
          moreActions={<TableActionButton action="close" icon={<MoreHorizontal />} ... />}
        />
      ))}
    </ul>
  }
  pagination={<ExitsTablePagination ... />}
/>
```

Do **not** create `ExitsTable2` / parallel table visual systems.
