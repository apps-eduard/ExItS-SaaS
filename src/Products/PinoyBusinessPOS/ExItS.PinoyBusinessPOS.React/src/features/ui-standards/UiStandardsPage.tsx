import { useMemo, useState, type ReactNode } from "react";
import { Navigate } from "react-router-dom";
import {
  ArrowLeft,
  ArrowRight,
  Ban,
  Check,
  CheckCircle2,
  CirclePause,
  ExternalLink,
  Eye,
  Info,
  MoreHorizontal,
  Pause,
  Pencil,
  Plus,
  Power,
  Printer,
  RefreshCw,
  RotateCcw,
  Save,
  Send,
  Trash2,
  X,
  XCircle,
} from "lucide-react";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  cycleExitsTableSort,
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableCheckbox,
  ExitsTableContainer,
  ExitsTableFooter,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableOutputActions,
  ExitsTablePagination,
  ExitsTableRow,
  ExitsTableToolbar,
  type ExitsTableSortDirection,
} from "@/components/exits/ExitsTable";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { formatUnitOfMeasureLabel } from "@/features/purchasing/purchase-order-create-connected";

type StandardsTab = "tables" | "buttons";
type DemoSkuFilter = "all" | "hasSku" | "noSku";
type DemoSortKey = "product" | "sku" | "quantity" | "unitCost" | "lineTotal";

type DemoLine = {
  id: string;
  name: string;
  sku: string;
  qty: number;
  unitOfMeasureCode: string;
  unitCost: number;
  lineTotal: number;
};

const DEMO_LINES: DemoLine[] = [
  {
    id: "apple",
    name: "Apple",
    sku: "PH-FRU-APPLE",
    qty: 2,
    unitOfMeasureCode: "Kilogram",
    unitCost: 180,
    lineTotal: 360,
  },
  {
    id: "banana",
    name: "Banana Lakatan",
    sku: "PH-FRU-BANANA",
    qty: 3,
    unitOfMeasureCode: "Kilogram",
    unitCost: 76,
    lineTotal: 228,
  },
  {
    id: "battery",
    name: "Battery AA Pack",
    sku: "PH-GEN-BATTERY-AA",
    qty: 1,
    unitOfMeasureCode: "Pack",
    unitCost: 61.75,
    lineTotal: 61.75,
  },
];

const DEMO_ORDER_TOTAL = 649.75;
const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

function qtyLabel(line: DemoLine): string {
  const uom = formatUnitOfMeasureLabel(line.unitOfMeasureCode);
  return uom ? `${line.qty} ${uom}` : String(line.qty);
}

function SampleRow({ children }: { children: ReactNode }) {
  return <div className="flex flex-wrap items-center gap-2">{children}</div>;
}

function SampleGroup({ title, children }: { title: string; children: ReactNode }) {
  const slug = title
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "");
  return (
    <section className="grid gap-2" data-testid={`ui-standards-btn-group-${slug}`}>
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">{title}</h3>
      {children}
    </section>
  );
}

export function UiStandardsPage() {
  const { t } = useI18n();
  const [tab, setTab] = useState<StandardsTab>("tables");
  const [searchInput, setSearchInput] = useState("");
  const [skuFilter, setSkuFilter] = useState<DemoSkuFilter>("all");
  const [sortKey, setSortKey] = useState<DemoSortKey | null>(null);
  const [sortDirection, setSortDirection] = useState<ExitsTableSortDirection>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set());
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const filteredSortedLines = useMemo(() => {
    const queryText = searchInput.trim().toLowerCase();
    let rows = DEMO_LINES.filter((line) => {
      if (skuFilter === "hasSku" && !line.sku.trim()) return false;
      if (skuFilter === "noSku" && line.sku.trim()) return false;
      if (!queryText) return true;
      return (
        line.name.toLowerCase().includes(queryText) || line.sku.toLowerCase().includes(queryText)
      );
    });

    if (sortKey && sortDirection) {
      const dir = sortDirection === "asc" ? 1 : -1;
      rows = [...rows].sort((a, b) => {
        switch (sortKey) {
          case "product":
            return a.name.localeCompare(b.name) * dir;
          case "sku":
            return a.sku.localeCompare(b.sku) * dir;
          case "quantity":
            return (a.qty - b.qty) * dir;
          case "unitCost":
            return (a.unitCost - b.unitCost) * dir;
          case "lineTotal":
            return (a.lineTotal - b.lineTotal) * dir;
          default:
            return 0;
        }
      });
    }
    return rows;
  }, [searchInput, skuFilter, sortKey, sortDirection]);

  const pageCount = Math.max(1, Math.ceil(filteredSortedLines.length / pageSize) || 1);
  const safePage = Math.min(page, pageCount);
  const pagedLines = useMemo(() => {
    const start = (safePage - 1) * pageSize;
    return filteredSortedLines.slice(start, start + pageSize);
  }, [filteredSortedLines, safePage, pageSize]);

  const visibleIds = pagedLines.map((line) => line.id);
  const allVisibleSelected =
    visibleIds.length > 0 && visibleIds.every((id) => selectedIds.has(id));
  const someVisibleSelected = visibleIds.some((id) => selectedIds.has(id));

  if (!isFrontendLocalValidationMode()) {
    return <Navigate to="/" replace />;
  }

  function toggleSort(key: DemoSortKey) {
    const next = cycleExitsTableSort(sortKey, sortDirection, key);
    setSortKey(next.key as DemoSortKey | null);
    setSortDirection(next.direction);
    setPage(1);
  }

  function toggleSelectAllVisible() {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (allVisibleSelected) {
        for (const id of visibleIds) next.delete(id);
      } else {
        for (const id of visibleIds) next.add(id);
      }
      return next;
    });
  }

  function toggleSelectOne(id: string) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function noopOutput() {
    // Reference page — no real file generation.
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="ui-standards-page">
      <PageHeader title={t("uiStandards.title")} description={t("uiStandards.description")} />

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("uiStandards.prefsHint")}</p>

      <UnderlineTabBar
        ariaLabel={t("uiStandards.sections")}
        testId="ui-standards-tabs"
        activeKey={tab}
        onChange={(key) => setTab(key as StandardsTab)}
        items={[
          { key: "tables", label: t("uiStandards.tabTables"), testId: "ui-standards-tab-tables" },
          { key: "buttons", label: t("uiStandards.tabButtons"), testId: "ui-standards-tab-buttons" },
        ]}
      />

      {tab === "tables" ? (
        <div className="grid gap-3" data-testid="ui-standards-tables-section">
          <ExitsTableContainer data-testid="ui-standards-table">
            <ExitsTableToolbar
              search={
                <SearchField
                  label={t("exitsTable.searchProducts")}
                  value={searchInput}
                  placeholder={t("exitsTable.searchProducts")}
                  onChange={(e) => {
                    setSearchInput(e.target.value);
                    setPage(1);
                  }}
                  onClear={() => {
                    setSearchInput("");
                    setPage(1);
                  }}
                  data-testid="ui-standards-table-search"
                />
              }
              filter={
                <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                  <span className="sr-only">{t("exitsTable.filter")}</span>
                  <select
                    className="exits-select"
                    value={skuFilter}
                    onChange={(e) => {
                      setSkuFilter(e.target.value as DemoSkuFilter);
                      setPage(1);
                    }}
                    aria-label={t("exitsTable.filter")}
                    data-testid="ui-standards-table-filter"
                  >
                    <option value="all">{t("exitsTable.filterAll")}</option>
                    <option value="hasSku">{t("exitsTable.filterHasSku")}</option>
                    <option value="noSku">{t("exitsTable.filterNoSku")}</option>
                  </select>
                </label>
              }
              selection={
                selectedIds.size > 0 ? (
                  <>
                    <span data-testid="ui-standards-selected-count">
                      {t("exitsTable.selectedCount").replace("{count}", String(selectedIds.size))}
                    </span>
                    <Button
                      type="button"
                      variant="ghost"
                      className="min-h-8 px-2"
                      onClick={() => setSelectedIds(new Set())}
                    >
                      {t("exitsTable.clearSelection")}
                    </Button>
                  </>
                ) : null
              }
              output={
                <ExitsTableOutputActions
                  csvLabel={t("exitsTable.exportCsv")}
                  xlsxLabel={t("exitsTable.exportExcel")}
                  pdfLabel={t("exitsTable.exportPdf")}
                  printLabel={t("exitsTable.print")}
                  menuLabel={t("exitsTable.exportPrintMenu")}
                  onCsv={noopOutput}
                  onXlsx={noopOutput}
                  onPdf={noopOutput}
                  onPrint={noopOutput}
                />
              }
            />

            <ExitsTable>
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead cellAlign="center">
                    <ExitsTableCheckbox
                      checked={allVisibleSelected}
                      indeterminate={someVisibleSelected && !allVisibleSelected}
                      onChange={() => toggleSelectAllVisible()}
                      aria-label={t("exitsTable.selectAll")}
                      data-testid="ui-standards-select-all"
                    />
                  </ExitsTableHead>
                  <ExitsTableHead
                    cellAlign="text"
                    sortable
                    sortDirection={sortKey === "product" ? sortDirection : null}
                    onSort={() => toggleSort("product")}
                  >
                    {t("purchasing.colProduct")}
                  </ExitsTableHead>
                  <ExitsTableHead
                    cellAlign="text"
                    sortable
                    sortDirection={sortKey === "sku" ? sortDirection : null}
                    onSort={() => toggleSort("sku")}
                  >
                    {t("catalog.sku")}
                  </ExitsTableHead>
                  <ExitsTableHead
                    cellAlign="numeric"
                    sortable
                    sortDirection={sortKey === "quantity" ? sortDirection : null}
                    onSort={() => toggleSort("quantity")}
                  >
                    {t("purchasing.qty")}
                  </ExitsTableHead>
                  <ExitsTableHead
                    cellAlign="money"
                    sortable
                    sortDirection={sortKey === "unitCost" ? sortDirection : null}
                    onSort={() => toggleSort("unitCost")}
                  >
                    {t("purchasing.unitCost")}
                  </ExitsTableHead>
                  <ExitsTableHead
                    cellAlign="money"
                    sortable
                    sortDirection={sortKey === "lineTotal" ? sortDirection : null}
                    onSort={() => toggleSort("lineTotal")}
                  >
                    {t("purchasing.lineTotal")}
                  </ExitsTableHead>
                </ExitsTableRow>
              </ExitsTableHeader>
              <ExitsTableBody>
                {pagedLines.map((line) => {
                  const selected = selectedIds.has(line.id);
                  return (
                    <ExitsTableRow key={line.id} selected={selected} interactive>
                      <ExitsTableCell cellAlign="center">
                        <ExitsTableCheckbox
                          checked={selected}
                          onChange={() => toggleSelectOne(line.id)}
                          aria-label={t("exitsTable.selectRow")}
                        />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        {line.name}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" className="text-muted">
                        {line.sku}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric">{qtyLabel(line)}</ExitsTableCell>
                      <ExitsTableCell cellAlign="money">
                        <MoneyDisplay amount={line.unitCost} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" emphasis="semibold">
                        <MoneyDisplay amount={line.lineTotal} />
                      </ExitsTableCell>
                    </ExitsTableRow>
                  );
                })}
              </ExitsTableBody>
              <ExitsTableFooter>
                <ExitsTableRow>
                  <ExitsTableCell cellAlign="actions" colSpan={5} emphasis="bold">
                    {t("incomingOrders.orderTotal")}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="money" emphasis="bold">
                    <MoneyDisplay amount={DEMO_ORDER_TOTAL} />
                  </ExitsTableCell>
                </ExitsTableRow>
              </ExitsTableFooter>
            </ExitsTable>

            <ExitsTableMobile>
              {pagedLines.map((line) => {
                const selected = selectedIds.has(line.id);
                return (
                  <ExitsTableMobileRow key={line.id} selected={selected}>
                    <div className="exits-table-mobile__lead">
                      <ExitsTableCheckbox
                        checked={selected}
                        onChange={() => toggleSelectOne(line.id)}
                        aria-label={t("exitsTable.selectRow")}
                      />
                      <div className="exits-table-mobile__lead-body">
                        <div className="exits-table-mobile__title-row">
                          <p className="exits-table-mobile__title">{line.name}</p>
                          <p className="exits-table-mobile__total">{formatPeso(line.lineTotal)}</p>
                        </div>
                        <p className="exits-table-mobile__meta">{line.sku}</p>
                        <p className="exits-table-mobile__math">
                          {qtyLabel(line)} × {formatPeso(line.unitCost)}
                        </p>
                      </div>
                    </div>
                  </ExitsTableMobileRow>
                );
              })}
              <li className="exits-table-mobile__footer">
                <span>{t("incomingOrders.orderTotal")}</span>
                <MoneyDisplay amount={DEMO_ORDER_TOTAL} />
              </li>
            </ExitsTableMobile>

            <ExitsTablePagination
              page={safePage}
              pageSize={pageSize}
              total={filteredSortedLines.length}
              pageSizeOptions={PAGE_SIZE_OPTIONS}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size);
                setPage(1);
              }}
              rowsPerPageLabel={t("exitsTable.rowsPerPage")}
              previousLabel={t("exitsTable.previous")}
              nextLabel={t("exitsTable.next")}
              rangeLabel={t("exitsTable.range")}
            />
          </ExitsTableContainer>

          <Card className="grid gap-2 p-3" data-testid="ui-standards-table-cheatsheet">
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
              {t("uiStandards.tableCheatTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("uiStandards.tableCheatLede")}
            </p>
            <pre className="m-0 overflow-x-auto rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3 text-[length:var(--exits-text-xs)] leading-relaxed">
{`EXITS TABLE
FULL TABLE
SIMPLE TABLE

SEARCH ON / OFF
FILTER ON / OFF
SORT ON / OFF
MULTI SELECT ON / OFF
OUTPUT ICONS ON / OFF
PAGE SIZE ON / OFF
PAGINATION ON / OFF
FOOTER ON / OFF
INLINE EDIT ON / OFF

OUTPUT ICONS = CSV + XLSX + PDF + Print
PAGE SIZE = 10 / 25 / 50 / 100 (default 25)

Example:
Convert this page to ExitsTable.
FULL TABLE.
MULTI SELECT OFF.
OUTPUT ICONS ON.`}
            </pre>
          </Card>
        </div>
      ) : null}

      {tab === "buttons" ? (
        <div className="grid gap-3" data-testid="ui-standards-buttons-section">
          <Card className="grid gap-4 p-3" data-testid="ui-standards-button-showcase">
            <div>
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("uiStandards.buttonPilotTitle")}
              </h2>
              <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {t("uiStandards.buttonPilotLede")}
              </p>
            </div>

            <SampleGroup title="PRIMARY">
              <SampleRow>
                <Button type="button"><Save className="size-4" aria-hidden />Save</Button>
                <Button type="button"><Plus className="size-4" aria-hidden />Create</Button>
                <Button type="button"><Plus className="size-4" aria-hidden />Add product</Button>
                <Button type="button"><Send className="size-4" aria-hidden />Submit</Button>
                <Button type="button">Continue<ArrowRight className="size-4" aria-hidden /></Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="SUCCESS">
              <SampleRow>
                <Button type="button" variant="success"><Check className="size-4" aria-hidden />Approve</Button>
                <Button type="button" variant="success"><Check className="size-4" aria-hidden />Accept order</Button>
                <Button type="button" variant="success"><CheckCircle2 className="size-4" aria-hidden />Complete</Button>
                <Button type="button" variant="success"><CheckCircle2 className="size-4" aria-hidden />Mark paid</Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="SECONDARY / MUTED">
              <SampleRow>
                <Button type="button" variant="secondary">Cancel</Button>
                <Button type="button" variant="secondary">Close</Button>
                <Button type="button" variant="secondary">Duplicate</Button>
                <Button type="button" variant="secondary">View history</Button>
                <Button type="button" variant="ghost"><X className="size-4" aria-hidden />Cancel editing</Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="OUTLINE">
              <SampleRow>
                <Button type="button" variant="outline">Change branch</Button>
                <Button type="button" variant="outline">Filter</Button>
                <Button type="button" variant="outline">Download</Button>
                <Button type="button" variant="outline">Secondary action</Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="GHOST">
              <SampleRow>
                <Button type="button" variant="ghost"><ArrowLeft className="size-4" aria-hidden />Back</Button>
                <Button type="button" variant="ghost"><X className="size-4" aria-hidden />Close</Button>
                <Button type="button" variant="ghost"><MoreHorizontal className="size-4" aria-hidden />More</Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="INFO">
              <SampleRow>
                <Button type="button" variant="info"><Eye className="size-4" aria-hidden />View details</Button>
                <Button type="button" variant="info"><Info className="size-4" aria-hidden />Information</Button>
                <Button type="button" variant="info"><ExternalLink className="size-4" aria-hidden />Preview</Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="WARNING">
              <SampleRow>
                <Button type="button" variant="warning"><Power className="size-4" aria-hidden />Deactivate</Button>
                <Button type="button" variant="warning"><Pause className="size-4" aria-hidden />Pause</Button>
                <Button type="button" variant="warning"><RotateCcw className="size-4" aria-hidden />Reset</Button>
                <Button type="button" variant="warning"><CirclePause className="size-4" aria-hidden />Suspend</Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="DANGER">
              <SampleRow>
                <Button type="button" variant="destructive"><XCircle className="size-4" aria-hidden />Decline</Button>
                <Button type="button" variant="destructive"><Trash2 className="size-4" aria-hidden />Delete</Button>
                <Button type="button" variant="destructive"><X className="size-4" aria-hidden />Remove</Button>
                <Button type="button" variant="destructive"><Ban className="size-4" aria-hidden />Void</Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="DANGER STRONG">
              <SampleRow>
                <Button type="button" variant="dangerStrong"><Trash2 className="size-4" aria-hidden />Delete permanently</Button>
                <Button type="button" variant="dangerStrong"><Ban className="size-4" aria-hidden />Permanently remove</Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="ICON ONLY">
              <SampleRow>
                <Button type="button" variant="outline" size="icon" title="Edit" aria-label="Edit">
                  <Pencil className="size-4" aria-hidden />
                </Button>
                <Button type="button" variant="destructive" size="icon" title="Delete" aria-label="Delete">
                  <Trash2 className="size-4" aria-hidden />
                </Button>
                <Button type="button" variant="outline" size="icon" title="Refresh" aria-label="Refresh">
                  <RefreshCw className="size-4" aria-hidden />
                </Button>
                <Button type="button" variant="outline" size="icon" title="Print" aria-label="Print">
                  <Printer className="size-4" aria-hidden />
                </Button>
                <Button type="button" variant="ghost" size="icon" title="More" aria-label="More">
                  <MoreHorizontal className="size-4" aria-hidden />
                </Button>
              </SampleRow>
            </SampleGroup>

            <SampleGroup title="ICON PLACEMENT">
              <SampleRow>
                <Button type="button"><Plus className="size-4" aria-hidden />Add product</Button>
                <Button type="button"><Save className="size-4" aria-hidden />Save</Button>
                <Button type="button" variant="destructive"><Trash2 className="size-4" aria-hidden />Delete</Button>
                <Button type="button">Continue<ArrowRight className="size-4" aria-hidden /></Button>
                <Button type="button" variant="info">Open<ExternalLink className="size-4" aria-hidden /></Button>
              </SampleRow>
            </SampleGroup>
          </Card>

          <Card className="grid gap-2 p-3" data-testid="ui-standards-button-cheatsheet">
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
              {t("uiStandards.buttonCheatTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-[var(--exits-warning)]">
              {t("uiStandards.buttonPilotBadge")}
            </p>
            <pre className="m-0 overflow-x-auto rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3 text-[length:var(--exits-text-xs)] leading-relaxed">
{`PRIMARY
SUCCESS
MUTED
OUTLINE
GHOST
INFO
WARNING
DANGER
DANGER STRONG
ICON ONLY
WITH ICON
NO ICON

Examples:
Save: PRIMARY + WITH ICON
Cancel: GHOST + WITH ICON
Approve: SUCCESS + WITH ICON
Deactivate: WARNING + WITH ICON
Delete: DANGER + WITH ICON
Delete permanently: DANGER STRONG + WITH ICON`}
            </pre>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
