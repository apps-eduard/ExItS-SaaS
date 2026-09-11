import { useMemo, useState, type ReactNode } from "react";
import { Navigate } from "react-router-dom";
import {
  ArrowLeft,
  ArrowRight,
  Ban,
  Check,
  CheckCircle2,
  ExternalLink,
  Eye,
  Loader2,
  MoreHorizontal,
  Pause,
  Pencil,
  Plus,
  Power,
  Printer,
  RefreshCw,
  RotateCcw,
  Save,
  Trash2,
  X,
  XCircle,
} from "lucide-react";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { Card } from "@/components/ui/card";
import { Button, buttonIconMotion } from "@/components/ui/button";
import { cn } from "@/lib/cn";
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

function SampleCard({
  label,
  children,
  testId,
  hint,
}: {
  label: string;
  children: ReactNode;
  testId?: string;
  hint?: string;
}) {
  return (
    <div
      className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
      data-testid={testId}
    >
      <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">{label}</span>
      <div className="flex justify-start">{children}</div>
      {hint ? (
        <span className="text-[length:var(--exits-text-xs)] text-muted">{hint}</span>
      ) : null}
    </div>
  );
}

function SampleGroup({ title, children }: { title: string; children: ReactNode }) {
  const slug = title
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "");
  return (
    <section
      className="grid gap-2 border-t border-border pt-3 first:border-t-0 first:pt-0"
      data-testid={`ui-standards-btn-group-${slug}`}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">{title}</h3>
      <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">{children}</div>
    </section>
  );
}

function RefreshRoundDemo() {
  const [deg, setDeg] = useState(0);
  const [hover, setHover] = useState(false);
  return (
    <Button
      type="button"
      size="icon"
      shape="round"
      variant="secondary"
      title="Refresh"
      aria-label="Refresh"
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      onClick={() => setDeg((n) => n + 360)}
    >
      <RefreshCw
        className="size-4 motion-reduce:transition-none"
        style={{
          transform: `rotate(${deg + (hover ? 20 : 0)}deg)`,
          transition: "transform var(--exits-motion-fast) var(--exits-ease-standard)",
        }}
        aria-hidden
      />
    </Button>
  );
}

function SaveSuccessDemo() {
  const [phase, setPhase] = useState<"idle" | "saving" | "saved">("idle");

  const onClick = () => {
    if (phase !== "idle") return;
    setPhase("saving");
    window.setTimeout(() => setPhase("saved"), 700);
    window.setTimeout(() => setPhase("idle"), 1800);
  };

  return (
    <Button
      type="button"
      shape="soft"
      disabled={phase === "saving"}
      aria-busy={phase === "saving"}
      onClick={onClick}
      className={cn(phase === "saved" && "border-[var(--exits-success)] text-[var(--exits-success)]")}
      variant={phase === "saved" ? "success" : "default"}
    >
      {phase === "saving" ? (
        <>
          <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
          Saving...
        </>
      ) : null}
      {phase === "saved" ? (
        <>
          <Check
            className="size-4 scale-100 opacity-100 transition-[opacity,transform] duration-150 ease-[var(--exits-ease-standard)] motion-reduce:transition-none"
            aria-hidden
          />
          Saved
        </>
      ) : null}
      {phase === "idle" ? (
        <>
          <Save className="size-4" aria-hidden />
          Save
        </>
      ) : null}
    </Button>
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
          <Card className="grid gap-4 p-3" data-testid="ui-standards-button-shapes">
            <div>
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("uiStandards.buttonShapesTitle")}
              </h2>
              <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {t("uiStandards.buttonShapesLede")}
              </p>
            </div>

            {(
              [
                {
                  label: "PRIMARY / Save",
                  render: (shape: "standard" | "soft" | "pill") => (
                    <Button type="button" shape={shape}>
                      <Save className="size-4" aria-hidden />
                      Save
                    </Button>
                  ),
                },
                {
                  label: "SUCCESS / Approve",
                  render: (shape: "standard" | "soft" | "pill") => (
                    <Button type="button" variant="success" shape={shape}>
                      <Check className="size-4" aria-hidden />
                      Approve
                    </Button>
                  ),
                },
                {
                  label: "WARNING / Deactivate",
                  render: (shape: "standard" | "soft" | "pill") => (
                    <Button type="button" variant="warning" shape={shape}>
                      <Power className="size-4" aria-hidden />
                      Deactivate
                    </Button>
                  ),
                },
                {
                  label: "DANGER / Delete",
                  render: (shape: "standard" | "soft" | "pill") => (
                    <Button type="button" variant="destructive" shape={shape}>
                      <Trash2 className="size-4" aria-hidden />
                      Delete
                    </Button>
                  ),
                },
                {
                  label: "MUTED / Cancel",
                  render: (shape: "standard" | "soft" | "pill") => (
                    <Button type="button" variant="secondary" shape={shape}>
                      <X className="size-4" aria-hidden />
                      Cancel
                    </Button>
                  ),
                },
              ] as const
            ).map((row) => (
              <div key={row.label} className="grid gap-2 border-t border-border pt-3 first:border-t-0 first:pt-0">
                <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-muted">{row.label}</p>
                <div className="grid gap-2 sm:grid-cols-3">
                  {(["standard", "soft", "pill"] as const).map((shape) => (
                    <div
                      key={shape}
                      className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
                      data-testid={`ui-standards-shape-${row.label.split(" / ")[0]?.toLowerCase()}-${shape}`}
                    >
                      <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">
                        {shape}
                      </span>
                      {row.render(shape)}
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </Card>

          <Card className="grid gap-4 p-3" data-testid="ui-standards-button-treatments">
            <div>
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("uiStandards.buttonTreatmentsTitle")}
              </h2>
              <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {t("uiStandards.buttonTreatmentsLede")}
              </p>
            </div>
            <div className="grid gap-2 sm:grid-cols-3">
              {(["flat", "elevated", "gradient"] as const).map((treatment) => (
                <div
                  key={treatment}
                  className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
                  data-testid={`ui-standards-treatment-primary-${treatment}`}
                >
                  <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">
                    {treatment}
                  </span>
                  <Button type="button" shape="soft" treatment={treatment}>
                    <Save className="size-4" aria-hidden />
                    Save
                  </Button>
                </div>
              ))}
            </div>
            <div className="grid gap-2 border-t border-border pt-3 sm:grid-cols-3">
              {(["flat", "elevated", "gradient"] as const).map((treatment) => (
                <div
                  key={treatment}
                  className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
                  data-testid={`ui-standards-treatment-danger-strong-${treatment}`}
                >
                  <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">
                    DANGER STRONG · {treatment}
                  </span>
                  <Button type="button" variant="dangerStrong" shape="soft" treatment={treatment}>
                    <Trash2 className="size-4" aria-hidden />
                    Delete permanently
                  </Button>
                </div>
              ))}
            </div>
          </Card>

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
              <SampleCard label="Save">
                <Button type="button" shape="soft">
                  <Save className="size-4" aria-hidden />
                  Save
                </Button>
              </SampleCard>
              <SampleCard label="Add product">
                <Button type="button" shape="soft">
                  <Plus className="size-4" aria-hidden />
                  Add product
                </Button>
              </SampleCard>
              <SampleCard label="Continue">
                <Button type="button" shape="soft">
                  Continue
                  <ArrowRight className="size-4" aria-hidden />
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="SUCCESS">
              <SampleCard label="Approve">
                <Button type="button" variant="success" shape="soft">
                  <Check className="size-4" aria-hidden />
                  Approve
                </Button>
              </SampleCard>
              <SampleCard label="Accept order">
                <Button type="button" variant="success" shape="soft">
                  <Check className="size-4" aria-hidden />
                  Accept order
                </Button>
              </SampleCard>
              <SampleCard label="Mark paid">
                <Button type="button" variant="success" shape="soft">
                  <CheckCircle2 className="size-4" aria-hidden />
                  Mark paid
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="MUTED">
              <SampleCard label="Cancel">
                <Button type="button" variant="secondary">
                  Cancel
                </Button>
              </SampleCard>
              <SampleCard label="Close">
                <Button type="button" variant="secondary">
                  Close
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="OUTLINE">
              <SampleCard label="Change branch">
                <Button type="button" variant="outline">
                  Change branch
                </Button>
              </SampleCard>
              <SampleCard label="Download">
                <Button type="button" variant="outline">
                  Download
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="GHOST">
              <SampleCard label="Back">
                <Button type="button" variant="ghost">
                  <ArrowLeft className="size-4" aria-hidden />
                  Back
                </Button>
              </SampleCard>
              <SampleCard label="More">
                <Button type="button" variant="ghost">
                  <MoreHorizontal className="size-4" aria-hidden />
                  More
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="INFO">
              <SampleCard label="View details">
                <Button type="button" variant="info">
                  <Eye className="size-4" aria-hidden />
                  View details
                </Button>
              </SampleCard>
              <SampleCard label="Preview">
                <Button type="button" variant="info">
                  Preview
                  <ExternalLink className="size-4" aria-hidden />
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="WARNING">
              <SampleCard label="Deactivate">
                <Button type="button" variant="warning">
                  <Power className="size-4" aria-hidden />
                  Deactivate
                </Button>
              </SampleCard>
              <SampleCard label="Pause">
                <Button type="button" variant="warning">
                  <Pause className="size-4" aria-hidden />
                  Pause
                </Button>
              </SampleCard>
              <SampleCard label="Reset">
                <Button type="button" variant="warning">
                  <RotateCcw className="size-4" aria-hidden />
                  Reset
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="DANGER">
              <SampleCard label="Decline">
                <Button type="button" variant="destructive">
                  <XCircle className="size-4" aria-hidden />
                  Decline
                </Button>
              </SampleCard>
              <SampleCard label="Delete">
                <Button type="button" variant="destructive">
                  <Trash2 className="size-4" aria-hidden />
                  Delete
                </Button>
              </SampleCard>
              <SampleCard label="Void">
                <Button type="button" variant="destructive">
                  <Ban className="size-4" aria-hidden />
                  Void
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="DANGER STRONG">
              <SampleCard label="Delete permanently">
                <Button type="button" variant="dangerStrong" shape="soft">
                  <Trash2 className="size-4" aria-hidden />
                  Delete permanently
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="ICON ONLY">
              {(
                [
                  {
                    action: "Edit",
                    variant: "outline" as const,
                    icon: Pencil,
                  },
                  {
                    action: "Refresh",
                    variant: "outline" as const,
                    icon: RefreshCw,
                  },
                  {
                    action: "Print",
                    variant: "outline" as const,
                    icon: Printer,
                  },
                  {
                    action: "Delete",
                    variant: "destructive" as const,
                    icon: Trash2,
                  },
                  {
                    action: "More",
                    variant: "ghost" as const,
                    icon: MoreHorizontal,
                  },
                ] as const
              ).map((row) => (
                <div
                  key={row.action}
                  className="grid gap-2 sm:col-span-2 lg:col-span-3"
                  data-testid={`ui-standards-icon-matrix-${row.action.toLowerCase()}`}
                >
                  <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-muted">
                    {row.action.toUpperCase()}
                  </p>
                  <div className="grid gap-2 sm:grid-cols-3">
                    {(["standard", "soft", "round"] as const).map((shape) => (
                      <SampleCard key={shape} label={shape} testId={`ui-standards-icon-${row.action.toLowerCase()}-${shape}`}>
                        <Button
                          type="button"
                          variant={row.variant}
                          size="icon"
                          shape={shape}
                          title={row.action}
                          aria-label={row.action}
                        >
                          <row.icon className="size-4" aria-hidden />
                        </Button>
                      </SampleCard>
                    ))}
                  </div>
                </div>
              ))}
            </SampleGroup>

            <SampleGroup title="ICON ONLY ROUND · INTENTS">
              <SampleCard label="ROUND GHOST · More" testId="ui-standards-round-ghost">
                <Button type="button" variant="ghost" size="icon" shape="round" title="More" aria-label="More">
                  <MoreHorizontal className="size-4" aria-hidden />
                </Button>
              </SampleCard>
              <SampleCard label="ROUND MUTED · Refresh" testId="ui-standards-round-muted">
                <Button
                  type="button"
                  variant="secondary"
                  size="icon"
                  shape="round"
                  title="Refresh"
                  aria-label="Refresh"
                >
                  <RefreshCw className="size-4" aria-hidden />
                </Button>
              </SampleCard>
              <SampleCard label="ROUND PRIMARY · Add" testId="ui-standards-round-primary">
                <Button type="button" size="icon" shape="round" title="Add" aria-label="Add">
                  <Plus className="size-4" aria-hidden />
                </Button>
              </SampleCard>
              <SampleCard label="ROUND INFO · View" testId="ui-standards-round-info">
                <Button type="button" variant="info" size="icon" shape="round" title="View" aria-label="View">
                  <Eye className="size-4" aria-hidden />
                </Button>
              </SampleCard>
              <SampleCard label="ROUND WARNING · Pause" testId="ui-standards-round-warning">
                <Button
                  type="button"
                  variant="warning"
                  size="icon"
                  shape="round"
                  title="Pause"
                  aria-label="Pause"
                >
                  <Pause className="size-4" aria-hidden />
                </Button>
              </SampleCard>
              <SampleCard label="ROUND DANGER · Delete" testId="ui-standards-round-danger">
                <Button
                  type="button"
                  variant="destructive"
                  size="icon"
                  shape="round"
                  title="Delete"
                  aria-label="Delete"
                >
                  <Trash2 className="size-4" aria-hidden />
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="STATES">
              <SampleCard label="Normal">
                <Button type="button" shape="soft">
                  <Save className="size-4" aria-hidden />
                  Normal
                </Button>
              </SampleCard>
              <SampleCard label="Hover / lift">
                <Button type="button" shape="soft" treatment="elevated">
                  <Save className="size-4" aria-hidden />
                  Hover / lift
                </Button>
              </SampleCard>
              <SampleCard label="Pressed (try)">
                <Button type="button" shape="soft" treatment="elevated">
                  Pressed (try)
                </Button>
              </SampleCard>
              <SampleCard label="Disabled">
                <Button type="button" shape="soft" disabled>
                  Disabled
                </Button>
              </SampleCard>
              <SampleCard label="Loading">
                <Button type="button" shape="soft" disabled aria-busy="true">
                  <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
                  Saving...
                </Button>
              </SampleCard>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted sm:col-span-2 lg:col-span-3">
                {t("uiStandards.buttonStatesHint")}
              </p>
            </SampleGroup>
          </Card>

          <Card className="grid gap-4 p-3" data-testid="ui-standards-button-motion">
            <div>
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("uiStandards.buttonMotionTitle")}
              </h2>
              <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {t("uiStandards.buttonMotionLede")}
              </p>
            </div>

            <SampleGroup title="STANDARD MOTION">
              <SampleCard label="Primary" hint="Color + press">
                <Button type="button" shape="soft">
                  <Save className="size-4" aria-hidden />
                  Save
                </Button>
              </SampleCard>
              <SampleCard label="Muted" hint="Quieter surface">
                <Button type="button" variant="secondary" shape="soft">
                  Cancel
                </Button>
              </SampleCard>
              <SampleCard label="Ghost" hint="Background fade only">
                <Button type="button" variant="ghost">
                  <MoreHorizontal className="size-4" aria-hidden />
                  More
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="PRESS FEEDBACK">
              <SampleCard label="Save" hint="Click to preview">
                <Button type="button" shape="soft">
                  <Save className="size-4" aria-hidden />
                  Save
                </Button>
              </SampleCard>
              <SampleCard label="Delete" hint="Calm press — no shake">
                <Button type="button" variant="destructive" shape="soft">
                  <Trash2 className={cn("size-4", buttonIconMotion.delete)} aria-hidden />
                  Delete
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="ELEVATED LIFT">
              <SampleCard label="Add" hint="Hover to preview">
                <Button type="button" shape="soft" treatment="elevated">
                  <Plus className={cn("size-4", buttonIconMotion.add)} aria-hidden />
                  Add
                </Button>
              </SampleCard>
              <SampleCard label="Main CTA" hint="Elevated + soft">
                <Button type="button" shape="soft" treatment="elevated">
                  <Save className="size-4" aria-hidden />
                  Save
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="ICON MOTION">
              <SampleCard label="Add" hint="Hover: slight scale">
                <Button type="button" shape="soft">
                  <Plus className={cn("size-4", buttonIconMotion.add)} aria-hidden />
                  Add
                </Button>
              </SampleCard>
              <SampleCard label="View" hint="Hover: slight scale">
                <Button type="button" variant="info" shape="soft">
                  <Eye className={cn("size-4", buttonIconMotion.view)} aria-hidden />
                  View
                </Button>
              </SampleCard>
              <SampleCard label="Delete" hint="Subtle lift only">
                <Button type="button" variant="destructive" shape="soft">
                  <Trash2 className={cn("size-4", buttonIconMotion.delete)} aria-hidden />
                  Delete
                </Button>
              </SampleCard>
              <SampleCard label="Warning" hint="Subtle only">
                <Button type="button" variant="warning" shape="soft">
                  <Power className={cn("size-4", buttonIconMotion.warning)} aria-hidden />
                  Deactivate
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="DIRECTIONAL MOTION">
              <SampleCard label="Continue" hint="Hover to preview">
                <Button type="button" shape="soft">
                  Continue
                  <ArrowRight className={cn("size-4", buttonIconMotion.continue)} aria-hidden />
                </Button>
              </SampleCard>
              <SampleCard label="Back" hint="Hover to preview">
                <Button type="button" variant="ghost">
                  <ArrowLeft className={cn("size-4", buttonIconMotion.back)} aria-hidden />
                  Back
                </Button>
              </SampleCard>
              <SampleCard label="Open" hint="Hover to preview">
                <Button type="button" variant="info" shape="soft">
                  Open
                  <ExternalLink className={cn("size-4", buttonIconMotion.open)} aria-hidden />
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="ROUND ICON MOTION">
              <SampleCard label="Refresh" hint="Hover / click once" testId="ui-standards-motion-round-refresh">
                <RefreshRoundDemo />
              </SampleCard>
              <SampleCard label="Add" hint="Hover: slight scale">
                <Button type="button" size="icon" shape="round" title="Add" aria-label="Add">
                  <Plus className={cn("size-4", buttonIconMotion.add)} aria-hidden />
                </Button>
              </SampleCard>
              <SampleCard label="Next" hint="Hover: directional">
                <Button type="button" size="icon" shape="round" title="Next" aria-label="Next">
                  <ArrowRight className={cn("size-4", buttonIconMotion.continue)} aria-hidden />
                </Button>
              </SampleCard>
              <SampleCard label="Delete" hint="Subtle lift only">
                <Button
                  type="button"
                  variant="destructive"
                  size="icon"
                  shape="round"
                  title="Delete"
                  aria-label="Delete"
                >
                  <Trash2 className={cn("size-4", buttonIconMotion.delete)} aria-hidden />
                </Button>
              </SampleCard>
              <SampleCard label="More" hint="Very subtle">
                <Button type="button" variant="ghost" size="icon" shape="round" title="More" aria-label="More">
                  <MoreHorizontal className={cn("size-4", buttonIconMotion.more)} aria-hidden />
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="LOADING MOTION">
              <SampleCard label="PRIMARY loading">
                <Button type="button" shape="soft" disabled aria-busy="true" className="min-w-[8.5rem]">
                  <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
                  Saving...
                </Button>
              </SampleCard>
              <SampleCard label="SUCCESS loading">
                <Button
                  type="button"
                  variant="success"
                  shape="soft"
                  disabled
                  aria-busy="true"
                  className="min-w-[9.5rem]"
                >
                  <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
                  Processing...
                </Button>
              </SampleCard>
              <SampleCard label="ICON ONLY ROUND loading">
                <Button
                  type="button"
                  size="icon"
                  shape="round"
                  disabled
                  aria-busy="true"
                  title="Loading"
                  aria-label="Loading"
                >
                  <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
                </Button>
              </SampleCard>
            </SampleGroup>

            <SampleGroup title="SUCCESS FEEDBACK">
              <SampleCard
                label="Save → Saved"
                hint="Showcase only — click once"
                testId="ui-standards-motion-success-demo"
              >
                <SaveSuccessDemo />
              </SampleCard>
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
{`BUTTON STANDARD — PILOT / NOT LOCKED

INTENT:
PRIMARY
SUCCESS
MUTED
OUTLINE
GHOST
INFO
WARNING
DANGER
DANGER STRONG

SHAPE:
STANDARD
SOFT
PILL
ROUND (icon-only circle)

TREATMENT:
FLAT
ELEVATED
GRADIENT

MOTION (conceptual — not Button API variants):
NONE
STANDARD (= transition + press)
CONTEXTUAL ICON (= specific icons only)

OTHER:
WITH ICON
NO ICON
ICON ONLY
ICON ONLY ROUND

Examples:
Edit: ICON ONLY ROUND GHOST
Refresh: ICON ONLY ROUND MUTED
Add: ICON ONLY ROUND PRIMARY
Delete: ICON ONLY ROUND DANGER
Continue: PRIMARY + SOFT + WITH ICON + DIRECTIONAL ICON
Main CTA: PRIMARY + SOFT + ELEVATED + WITH ICON
Save: PRIMARY + SOFT + ELEVATED + WITH ICON
Cancel: GHOST + STANDARD + WITH ICON
Approve: SUCCESS + SOFT + WITH ICON
Deactivate: WARNING + STANDARD + WITH ICON
Delete (text): DANGER + STANDARD + WITH ICON
Delete permanently: DANGER STRONG + SOFT + WITH ICON
Main CTA gradient: PRIMARY + SOFT + GRADIENT + WITH ICON`}
            </pre>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
