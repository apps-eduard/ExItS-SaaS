import { Fragment, useEffect, useId, useMemo, useState, type ReactNode } from "react";
import {
  Check,
  Eye,
  LoaderCircle,
  MoreHorizontal,
  Pencil,
  RotateCcw,
  Trash2,
} from "lucide-react";
import { Button, buttonIconMotion } from "@/components/ui/button";
import {
  DropdownMenu,
  MenuItem,
  MenuSeparator,
  useDismissibleOpen,
} from "@/components/ui/dropdown-menu";
import {
  cycleExitsTableSort,
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableCheckbox,
  ExitsTableContainer,
  ExitsTableEditMenu,
  ExitsTableFooter,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableInlineEditor,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableOutputActions,
  ExitsTablePagination,
  ExitsTableRow,
  ExitsTableToolbar,
  type ExitsTableEditableField,
  type ExitsTableSortDirection,
} from "@/components/exits/ExitsTable";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { MoneyInput, QuantityInput } from "@/components/exits/MoneyQuantityInputs";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { Input } from "@/components/ui/input";
import { UiStandardsSection } from "@/features/ui-standards/UiStandardsSection";
import { formatUnitOfMeasureLabel } from "@/features/purchasing/purchase-order-create-connected";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import {
  UiStandardsCopyCommand,
  type UiStandardsStandardName,
} from "@/features/ui-standards/UiStandardsCopyCommand";
import { UiStandardsSampleCard } from "@/features/ui-standards/UiStandardsSampleCard";

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

const DEMO_LINES_SEED: DemoLine[] = [
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

/** Page-owned editable columns for this UI Standards demo (not hardcoded in ExitsTable). */
const DEMO_EDITABLE_FIELDS: ReadonlyArray<ExitsTableEditableField> = [
  { key: "sku", label: "SKU" },
  { key: "quantity", label: "Quantity" },
  { key: "unitCost", label: "Unit cost" },
];

type DemoEditFieldKey = "sku" | "quantity" | "unitCost";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

type EditErrors = {
  sku?: string;
  qty?: string;
  unitCost?: string;
};

type EditBaseline = {
  sku: string;
  qty: string;
  unitCost: string;
};

function qtyLabel(line: DemoLine): string {
  const uom = formatUnitOfMeasureLabel(line.unitOfMeasureCode);
  return uom ? `${line.qty} ${uom}` : String(line.qty);
}

function SampleFrame({
  label,
  children,
  hint,
  testId,
  className,
  command,
  commandContext,
  explanatory,
  standard = "Table",
}: {
  label: string;
  children: ReactNode;
  hint?: string;
  testId?: string;
  className?: string;
  command?: string;
  commandContext?: string;
  explanatory?: boolean;
  standard?: UiStandardsStandardName | UiStandardsStandardName[];
}) {
  return (
    <UiStandardsSampleCard
      label={label}
      testId={testId}
      hint={hint}
      className={className}
      bordered={false}
      standard={standard}
      command={command}
      commandContext={commandContext}
      explanatory={explanatory}
    >
      {children}
    </UiStandardsSampleCard>
  );
}

function StaticSampleGroup({ title, children }: { title: string; children: ReactNode }) {
  const slug = title
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "");
  return (
    <section
      className="grid gap-2 border-t border-border pt-3 first:border-t-0 first:pt-0"
      data-testid={`ui-standards-tables-group-${slug}`}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">{title}</h3>
      <div className="grid gap-3">{children}</div>
    </section>
  );
}

function IconAction({
  label,
  variant = "ghost",
  children,
  onClick,
  className,
  title,
}: {
  label: string;
  variant?: "ghost" | "destructive" | "success" | "info";
  children: ReactNode;
  onClick?: () => void;
  className?: string;
  /** Tooltip; defaults to aria-label. Prefer short title for Cancel editing. */
  title?: string;
}) {
  return (
    <Button
      type="button"
      variant={variant}
      size="icon"
      shape="round"
      title={title ?? label}
      aria-label={label}
      className={className}
      onClick={onClick}
    >
      {children}
    </Button>
  );
}

function MoreActionsMenu({ productName }: { productName: string }) {
  const menu = useDismissibleOpen(false);
  return (
    <DropdownMenu
      align="end"
      open={menu.open}
      onOpenChange={menu.setOpen}
      menuLabel={`Actions for ${productName}`}
      trigger={(triggerProps) => (
        <Button
          type="button"
          variant="ghost"
          size="icon"
          shape="round"
          id={triggerProps.id}
          aria-haspopup="menu"
          aria-expanded={triggerProps.expanded}
          aria-controls={triggerProps.controls}
          aria-label={`More actions for ${productName}`}
          title="More"
          data-testid="ui-standards-table-actions-more-menu"
          onClick={triggerProps.onClick}
          onKeyDown={triggerProps.onKeyDown}
        >
          <MoreHorizontal className={cn("size-4", buttonIconMotion.more)} aria-hidden />
        </Button>
      )}
    >
      <MenuItem onSelect={() => menu.close()}>View details</MenuItem>
      <MenuItem onSelect={() => menu.close()}>Edit</MenuItem>
      <MenuItem onSelect={() => menu.close()}>Duplicate</MenuItem>
      <MenuItem onSelect={() => menu.close()}>History</MenuItem>
      <MenuSeparator />
      <MenuItem destructive onSelect={() => menu.close()}>
        Delete
      </MenuItem>
    </DropdownMenu>
  );
}

type DisclosureProps = {
  isOpen: (id: string) => boolean;
  setOpen: (id: string, open: boolean) => void;
};

export function UiStandardsTablesPanel({ isOpen, setOpen }: DisclosureProps) {
  const { t } = useI18n();
  const [lines, setLines] = useState<DemoLine[]>(() => DEMO_LINES_SEED.map((line) => ({ ...line })));
  const [searchInput, setSearchInput] = useState("");
  const [skuFilter, setSkuFilter] = useState<DemoSkuFilter>("all");
  const [sortKey, setSortKey] = useState<DemoSortKey | null>(null);
  const [sortDirection, setSortDirection] = useState<ExitsTableSortDirection>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set());
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingFields, setEditingFields] = useState<ReadonlySet<DemoEditFieldKey>>(
    () => new Set(),
  );
  const [editSku, setEditSku] = useState("");
  const [editQty, setEditQty] = useState("2");
  const [editUnitCost, setEditUnitCost] = useState("180.00");
  const [editErrors, setEditErrors] = useState<EditErrors>({});
  const [editBaseline, setEditBaseline] = useState<EditBaseline | null>(null);
  const [cellEditValue, setCellEditValue] = useState("180.00");
  const [cellEditing, setCellEditing] = useState(false);
  const [mobileEditingId, setMobileEditingId] = useState<string | null>(null);
  const [mobileEditingFields, setMobileEditingFields] = useState<ReadonlySet<DemoEditFieldKey>>(
    () => new Set(),
  );
  const qtyErrorId = useId();
  const skuErrorId = useId();
  const unitCostErrorId = useId();
  const rowValidationId = useId();

  const filteredSortedLines = useMemo(() => {
    const queryText = searchInput.trim().toLowerCase();
    let rows = lines.filter((line) => {
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
  }, [lines, searchInput, skuFilter, sortKey, sortDirection]);

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

  const orderTotal = useMemo(
    () => lines.reduce((sum, line) => sum + line.lineTotal, 0),
    [lines],
  );

  function isEditingField(field: DemoEditFieldKey) {
    return editingFields.has(field);
  }

  function isMobileEditingField(field: DemoEditFieldKey) {
    return mobileEditingFields.has(field);
  }

  function previewLineTotal(line: DemoLine, fields: ReadonlySet<DemoEditFieldKey>) {
    const qty = fields.has("quantity") ? Number(editQty) || 0 : line.qty;
    const unitCost = fields.has("unitCost") ? Number(editUnitCost) || 0 : line.unitCost;
    if (fields.has("quantity") || fields.has("unitCost")) {
      return Number((qty * unitCost).toFixed(2));
    }
    return line.lineTotal;
  }

  const editErrorMessages = useMemo(
    () => [editErrors.sku, editErrors.qty, editErrors.unitCost].filter(Boolean) as string[],
    [editErrors],
  );

  useEffect(() => {
    if (!editingId && !mobileEditingId) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        cancelRowEdit();
        cancelMobileEdit();
      }
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [editingId, mobileEditingId]);

  useEffect(() => {
    if (!editingId) return;
    const activeId = editingId;
    const focusTestId = editingFields.has("sku")
      ? `ui-standards-edit-sku-${activeId}`
      : editingFields.has("quantity")
        ? `ui-standards-edit-qty-${activeId}`
        : editingFields.has("unitCost")
          ? `ui-standards-edit-unit-cost-${activeId}`
          : null;
    if (!focusTestId) return;
    const timer = window.setTimeout(() => {
      const el = document.querySelector<HTMLInputElement>(`[data-testid="${focusTestId}"]`);
      el?.focus();
      el?.select?.();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [editingId, editingFields]);

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

  function loadEditDraft(line: DemoLine) {
    const baseline: EditBaseline = {
      sku: line.sku,
      qty: String(line.qty),
      unitCost: line.unitCost.toFixed(2),
    };
    setEditBaseline(baseline);
    setEditSku(baseline.sku);
    setEditQty(baseline.qty);
    setEditUnitCost(baseline.unitCost);
    setEditErrors({});
  }

  function startFieldEdit(line: DemoLine, fieldKey: string) {
    const key = fieldKey as DemoEditFieldKey;
    if (!DEMO_EDITABLE_FIELDS.some((field) => field.key === key)) return;
    setMobileEditingId(null);
    setMobileEditingFields(new Set());
    setEditingId(line.id);
    setEditingFields(new Set([key]));
    loadEditDraft(line);
  }

  function startEditAll(line: DemoLine) {
    setMobileEditingId(null);
    setMobileEditingFields(new Set());
    setEditingId(line.id);
    setEditingFields(new Set(DEMO_EDITABLE_FIELDS.map((field) => field.key as DemoEditFieldKey)));
    loadEditDraft(line);
  }

  function cancelRowEdit() {
    setEditingId(null);
    setEditingFields(new Set());
    setEditBaseline(null);
    setEditErrors({});
  }

  /**
   * Reset: put draft values back to the originals from when edit started.
   * If already at originals, exit edit mode (mouse-friendly leave without Save).
   */
  function resetRowEdit() {
    if (!editBaseline) {
      cancelRowEdit();
      return;
    }
    const alreadyOriginal =
      editSku === editBaseline.sku &&
      editQty === editBaseline.qty &&
      editUnitCost === editBaseline.unitCost;
    if (alreadyOriginal) {
      cancelRowEdit();
      return;
    }
    setEditSku(editBaseline.sku);
    setEditQty(editBaseline.qty);
    setEditUnitCost(editBaseline.unitCost);
    setEditErrors({});
  }

  function cancelMobileEdit() {
    setMobileEditingId(null);
    setMobileEditingFields(new Set());
    setEditBaseline(null);
    setEditErrors({});
  }

  function resetMobileEdit() {
    if (!editBaseline) {
      cancelMobileEdit();
      return;
    }
    const alreadyOriginal =
      editSku === editBaseline.sku &&
      editQty === editBaseline.qty &&
      editUnitCost === editBaseline.unitCost;
    if (alreadyOriginal) {
      cancelMobileEdit();
      return;
    }
    setEditSku(editBaseline.sku);
    setEditQty(editBaseline.qty);
    setEditUnitCost(editBaseline.unitCost);
    setEditErrors({});
  }

  function validateEdit(lineId: string, fields: ReadonlySet<DemoEditFieldKey>): EditErrors {
    const errors: EditErrors = {};
    if (fields.has("sku")) {
      const sku = editSku.trim();
      if (!sku) {
        errors.sku = "SKU is required.";
      } else if (
        lines.some(
          (line) => line.id !== lineId && line.sku.trim().toLowerCase() === sku.toLowerCase(),
        )
      ) {
        errors.sku = "SKU already exists.";
      }
    }
    if (fields.has("quantity")) {
      const qty = Number(editQty);
      if (!(qty > 0) || Number.isNaN(qty)) {
        errors.qty = "Quantity must be greater than zero.";
      }
    }
    if (fields.has("unitCost")) {
      const unitCost = Number(editUnitCost);
      if (Number.isNaN(unitCost) || unitCost < 0) {
        errors.unitCost = "Unit cost must be a valid amount.";
      }
    }
    return errors;
  }

  function saveRowEdit(lineId: string) {
    const fields = editingFields;
    const errors = validateEdit(lineId, fields);
    if (Object.keys(errors).length > 0) {
      setEditErrors(errors);
      return;
    }
    setLines((prev) =>
      prev.map((line) => {
        if (line.id !== lineId) return line;
        const next = { ...line };
        if (fields.has("sku")) next.sku = editSku.trim();
        if (fields.has("quantity")) next.qty = Number(editQty);
        if (fields.has("unitCost")) next.unitCost = Number(editUnitCost);
        if (fields.has("quantity") || fields.has("unitCost")) {
          next.lineTotal = Number((next.qty * next.unitCost).toFixed(2));
        }
        return next;
      }),
    );
    cancelRowEdit();
  }

  function startMobileFieldEdit(line: DemoLine, fieldKey: string) {
    const key = fieldKey as DemoEditFieldKey;
    if (!DEMO_EDITABLE_FIELDS.some((field) => field.key === key)) return;
    setEditingId(null);
    setEditingFields(new Set());
    setMobileEditingId(line.id);
    setMobileEditingFields(new Set([key]));
    loadEditDraft(line);
  }

  function startMobileEditAll(line: DemoLine) {
    setEditingId(null);
    setEditingFields(new Set());
    setMobileEditingId(line.id);
    setMobileEditingFields(new Set(DEMO_EDITABLE_FIELDS.map((field) => field.key as DemoEditFieldKey)));
    loadEditDraft(line);
  }

  function saveMobileEdit(lineId: string) {
    const fields = mobileEditingFields;
    const errors = validateEdit(lineId, fields);
    if (Object.keys(errors).length > 0) {
      setEditErrors(errors);
      return;
    }
    setLines((prev) =>
      prev.map((line) => {
        if (line.id !== lineId) return line;
        const next = { ...line };
        if (fields.has("sku")) next.sku = editSku.trim();
        if (fields.has("quantity")) next.qty = Number(editQty);
        if (fields.has("unitCost")) next.unitCost = Number(editUnitCost);
        if (fields.has("quantity") || fields.has("unitCost")) {
          next.lineTotal = Number((next.qty * next.unitCost).toFixed(2));
        }
        return next;
      }),
    );
    cancelMobileEdit();
  }

  function noopOutput() {
    // Reference page — no real file generation.
  }

  return (
    <div className="grid gap-3" data-testid="ui-standards-tables-section">
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("uiStandards.tablesExtensionPilotBadge")}
      </p>

      <UiStandardsSection
        id="tables.demo"
        title={t("uiStandards.tabTables")}
        description={t("uiStandards.tableDemoLede")}
        summary="FULL TABLE · ACTIONS ON · INLINE EDIT ON · APPROVED / LOCKED"
        open={isOpen("tables.demo")}
        onOpenChange={(open) => setOpen("tables.demo", open)}
        testId="ui-standards-table-demo"
      >
        <UiStandardsCopyCommand standard="Table" command="FULL TABLE + ACTIONS ON + INLINE EDIT ON" />
        <UiStandardsCopyCommand standard="Table" command="FULL TABLE + MULTI SELECT OFF" />
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

          <ExitsTable data-testid="ui-standards-table-grid">
            <ExitsTableHeader>
              <ExitsTableRow>
                <ExitsTableHead cellAlign="center" colSize="checkbox">
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
                  colSize="flex"
                  sortable
                  sortDirection={sortKey === "product" ? sortDirection : null}
                  onSort={() => toggleSort("product")}
                >
                  {t("purchasing.colProduct")}
                </ExitsTableHead>
                <ExitsTableHead
                  cellAlign="text"
                  colSize="sku"
                  sortable
                  sortDirection={sortKey === "sku" ? sortDirection : null}
                  onSort={() => toggleSort("sku")}
                  data-testid="ui-standards-table-sku-head"
                >
                  {t("catalog.sku")}
                </ExitsTableHead>
                <ExitsTableHead
                  cellAlign="numeric"
                  colSize="numeric"
                  sortable
                  sortDirection={sortKey === "quantity" ? sortDirection : null}
                  onSort={() => toggleSort("quantity")}
                  data-testid="ui-standards-table-qty-head"
                >
                  {t("purchasing.qty")}
                </ExitsTableHead>
                <ExitsTableHead
                  cellAlign="money"
                  colSize="money"
                  sortable
                  sortDirection={sortKey === "unitCost" ? sortDirection : null}
                  onSort={() => toggleSort("unitCost")}
                >
                  {t("purchasing.unitCost")}
                </ExitsTableHead>
                <ExitsTableHead
                  cellAlign="money"
                  colSize="money"
                  sortable
                  sortDirection={sortKey === "lineTotal" ? sortDirection : null}
                  onSort={() => toggleSort("lineTotal")}
                  data-testid="ui-standards-table-total-head"
                >
                  {t("purchasing.lineTotal")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="actions" colSize="actions">
                  Actions
                </ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {pagedLines.map((line) => {
                const selected = selectedIds.has(line.id);
                const editing = editingId === line.id;
                const editSkuOn = editing && isEditingField("sku");
                const editQtyOn = editing && isEditingField("quantity");
                const editCostOn = editing && isEditingField("unitCost");
                const lineTotalDisplay = editing
                  ? previewLineTotal(line, editingFields)
                  : line.lineTotal;
                const showRowValidation = editing && editErrorMessages.length > 0;
                return (
                  <Fragment key={line.id}>
                    <ExitsTableRow
                      selected={selected}
                      editing={editing}
                      interactive={!editing}
                      data-testid={`ui-standards-main-row-${line.id}`}
                      onClick={() => {
                        // Row click = primary view navigation (demo no-op).
                      }}
                    >
                      <ExitsTableCell cellAlign="center" colSize="checkbox">
                        <ExitsTableCheckbox
                          checked={selected}
                          onChange={() => toggleSelectOne(line.id)}
                          aria-label={t("exitsTable.selectRow")}
                          onClick={(e) => e.stopPropagation()}
                        />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                        {line.name}
                      </ExitsTableCell>
                      <ExitsTableCell
                        cellAlign="text"
                        colSize="sku"
                        truncate={!editSkuOn}
                        title={line.sku}
                        className={editSkuOn ? undefined : "text-muted"}
                      >
                        {editSkuOn ? (
                          <ExitsTableInlineEditor
                            align="start"
                            invalid={Boolean(editErrors.sku)}
                            errorId={`${skuErrorId}-${line.id}`}
                          >
                            <Input
                              label={`SKU for ${line.name}`}
                              value={editSku}
                              onChange={(e) => {
                                setEditSku(e.target.value);
                                setEditErrors((prev) => ({ ...prev, sku: undefined }));
                              }}
                              aria-invalid={Boolean(editErrors.sku)}
                              aria-describedby={
                                editErrors.sku
                                  ? `${rowValidationId}-${line.id}`
                                  : undefined
                              }
                              data-testid={`ui-standards-edit-sku-${line.id}`}
                            />
                          </ExitsTableInlineEditor>
                        ) : (
                          line.sku
                        )}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" colSize="numeric">
                        {editQtyOn ? (
                          <div className="exits-table__qty-edit">
                            <ExitsTableInlineEditor
                              invalid={Boolean(editErrors.qty)}
                              errorId={`${qtyErrorId}-${line.id}`}
                            >
                              <QuantityInput
                                label={`Quantity for ${line.name}`}
                                value={editQty}
                                onChange={(e) => {
                                  setEditQty(e.target.value);
                                  setEditErrors((prev) => ({ ...prev, qty: undefined }));
                                }}
                                aria-invalid={Boolean(editErrors.qty)}
                                aria-describedby={
                                  editErrors.qty
                                    ? `${rowValidationId}-${line.id}`
                                    : undefined
                                }
                                data-testid={`ui-standards-edit-qty-${line.id}`}
                              />
                            </ExitsTableInlineEditor>
                            <span className="exits-table__uom">
                              {formatUnitOfMeasureLabel(line.unitOfMeasureCode)}
                            </span>
                          </div>
                        ) : (
                          qtyLabel(line)
                        )}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money">
                        {editCostOn ? (
                          <div className="exits-table__money-edit">
                            <span className="exits-table__currency-prefix" aria-hidden>
                              ₱
                            </span>
                            <ExitsTableInlineEditor
                              invalid={Boolean(editErrors.unitCost)}
                              errorId={`${unitCostErrorId}-${line.id}`}
                            >
                              <MoneyInput
                                label={`Unit cost for ${line.name}`}
                                value={editUnitCost}
                                onChange={(e) => {
                                  setEditUnitCost(e.target.value);
                                  setEditErrors((prev) => ({ ...prev, unitCost: undefined }));
                                }}
                                aria-invalid={Boolean(editErrors.unitCost)}
                                aria-describedby={
                                  editErrors.unitCost
                                    ? `${rowValidationId}-${line.id}`
                                    : undefined
                                }
                                data-testid={`ui-standards-edit-unit-cost-${line.id}`}
                              />
                            </ExitsTableInlineEditor>
                          </div>
                        ) : (
                          <MoneyDisplay amount={line.unitCost} />
                        )}
                      </ExitsTableCell>
                      <ExitsTableCell
                        cellAlign="money"
                        colSize="money"
                        emphasis="semibold"
                        data-testid={`ui-standards-line-total-${line.id}`}
                      >
                        <MoneyDisplay amount={lineTotalDisplay} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions" colSize="actions">
                        <ExitsTableActions data-testid={`ui-standards-table-row-actions-${line.id}`}>
                          {editing ? (
                            <>
                              <IconAction
                                label={`Save ${line.name} changes`}
                                variant="success"
                                onClick={() => saveRowEdit(line.id)}
                              >
                                <Check className="size-4" aria-hidden />
                              </IconAction>
                              <IconAction
                                label={`Reset ${line.name} to original`}
                                title="Reset"
                                className="exits-table__action-reset"
                                onClick={() => resetRowEdit()}
                              >
                                <RotateCcw className="size-4" aria-hidden />
                              </IconAction>
                            </>
                          ) : DEMO_EDITABLE_FIELDS.length > 0 ? (
                            <>
                              <ExitsTableEditMenu
                                fields={DEMO_EDITABLE_FIELDS}
                                ariaLabel={`Edit ${line.name}`}
                                data-testid={`ui-standards-edit-menu-${line.id}`}
                                onSelectField={(key) => startFieldEdit(line, key)}
                                onEditAll={() => startEditAll(line)}
                              />
                              <MoreActionsMenu productName={line.name} />
                            </>
                          ) : (
                            <MoreActionsMenu productName={line.name} />
                          )}
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                    {showRowValidation ? (
                      <ExitsTableRow error>
                        <ExitsTableCell colSpan={7}>
                          <div
                            id={`${rowValidationId}-${line.id}`}
                            className="exits-table__row-validation"
                            role="alert"
                          >
                            {editErrorMessages.join(" ")}
                          </div>
                        </ExitsTableCell>
                      </ExitsTableRow>
                    ) : null}
                  </Fragment>
                );
              })}
            </ExitsTableBody>
            <ExitsTableFooter>
              <ExitsTableRow>
                {/* checkbox + product + sku + qty + unit cost = 5 */}
                <ExitsTableCell cellAlign="actions" colSpan={5} emphasis="bold">
                  {t("incomingOrders.orderTotal")}
                </ExitsTableCell>
                <ExitsTableCell
                  cellAlign="money"
                  colSize="money"
                  emphasis="bold"
                  data-testid="ui-standards-order-total"
                >
                  <MoneyDisplay amount={orderTotal} />
                </ExitsTableCell>
                <ExitsTableCell cellAlign="actions" colSize="actions" aria-hidden />
              </ExitsTableRow>
            </ExitsTableFooter>
          </ExitsTable>

          <ExitsTableMobile>
            {pagedLines.map((line) => {
              const selected = selectedIds.has(line.id);
              const mobileEditing = mobileEditingId === line.id;
              const mobileSkuOn = mobileEditing && isMobileEditingField("sku");
              const mobileQtyOn = mobileEditing && isMobileEditingField("quantity");
              const mobileCostOn = mobileEditing && isMobileEditingField("unitCost");
              const mobileTotal = mobileEditing
                ? previewLineTotal(line, mobileEditingFields)
                : line.lineTotal;
              return (
                <ExitsTableMobileRow
                  key={line.id}
                  selected={selected}
                  editing={mobileEditing}
                  data-testid={`ui-standards-mobile-row-${line.id}`}
                >
                  <div className="exits-table-mobile__lead">
                    <ExitsTableCheckbox
                      checked={selected}
                      onChange={() => toggleSelectOne(line.id)}
                      aria-label={t("exitsTable.selectRow")}
                    />
                    <div className="exits-table-mobile__lead-body">
                      <div className="exits-table-mobile__title-row">
                        <p className="exits-table-mobile__title">{line.name}</p>
                        {!mobileEditing ? (
                          <p className="exits-table-mobile__total">{formatPeso(line.lineTotal)}</p>
                        ) : null}
                      </div>
                      {!mobileEditing ? (
                        <>
                          <p className="exits-table-mobile__meta">SKU: {line.sku}</p>
                          <p className="exits-table-mobile__math">
                            Quantity: {qtyLabel(line)}
                          </p>
                          <p className="exits-table-mobile__math">
                            Unit cost: {formatPeso(line.unitCost)}
                          </p>
                          <p className="exits-table-mobile__total">
                            Line total: {formatPeso(line.lineTotal)}
                          </p>
                          <div className="exits-table-mobile__actions">
                            <Button type="button" variant="outline" shape="soft">
                              View
                            </Button>
                            {DEMO_EDITABLE_FIELDS.length > 0 ? (
                              <ExitsTableEditMenu
                                fields={DEMO_EDITABLE_FIELDS}
                                ariaLabel={`Edit ${line.name}`}
                                trigger="button"
                                data-testid={`ui-standards-mobile-edit-menu-${line.id}`}
                                onSelectField={(key) => startMobileFieldEdit(line, key)}
                                onEditAll={() => startMobileEditAll(line)}
                              />
                            ) : null}
                            <MoreActionsMenu productName={line.name} />
                          </div>
                        </>
                      ) : (
                        <div className="exits-table-mobile__edit-form">
                          {mobileSkuOn ? (
                            <>
                              <label>
                                SKU
                                <input
                                  value={editSku}
                                  onChange={(e) => setEditSku(e.target.value)}
                                  aria-label={`SKU for ${line.name}`}
                                  aria-invalid={Boolean(editErrors.sku)}
                                />
                              </label>
                              {editErrors.sku ? (
                                <p className="exits-table__editor-error m-0" role="alert">
                                  {editErrors.sku}
                                </p>
                              ) : null}
                            </>
                          ) : (
                            <p className="exits-table-mobile__meta">SKU: {line.sku}</p>
                          )}
                          {mobileQtyOn ? (
                            <>
                              <label>
                                Quantity
                                <input
                                  value={editQty}
                                  onChange={(e) => setEditQty(e.target.value)}
                                  inputMode="decimal"
                                  aria-label={`Quantity for ${line.name}`}
                                  aria-invalid={Boolean(editErrors.qty)}
                                />
                              </label>
                              <span className="exits-table__uom">
                                {formatUnitOfMeasureLabel(line.unitOfMeasureCode)}
                              </span>
                              {editErrors.qty ? (
                                <p className="exits-table__editor-error m-0" role="alert">
                                  {editErrors.qty}
                                </p>
                              ) : null}
                            </>
                          ) : (
                            <p className="exits-table-mobile__math">Quantity: {qtyLabel(line)}</p>
                          )}
                          {mobileCostOn ? (
                            <>
                              <label>
                                Unit cost
                                <span className="exits-table__money-edit">
                                  <span className="exits-table__currency-prefix" aria-hidden>
                                    ₱
                                  </span>
                                  <input
                                    value={editUnitCost}
                                    onChange={(e) => setEditUnitCost(e.target.value)}
                                    inputMode="decimal"
                                    aria-label={`Unit cost for ${line.name}`}
                                    aria-invalid={Boolean(editErrors.unitCost)}
                                  />
                                </span>
                              </label>
                              {editErrors.unitCost ? (
                                <p className="exits-table__editor-error m-0" role="alert">
                                  {editErrors.unitCost}
                                </p>
                              ) : null}
                            </>
                          ) : (
                            <p className="exits-table-mobile__math">
                              Unit cost: {formatPeso(line.unitCost)}
                            </p>
                          )}
                          <p className="exits-table-mobile__total m-0">
                            Line total: {formatPeso(mobileTotal)}
                          </p>
                          <div className="exits-table-mobile__edit-actions">
                            <Button
                              type="button"
                              variant="ghost"
                              shape="soft"
                              className="exits-table__action-reset"
                              onClick={() => resetMobileEdit()}
                            >
                              <RotateCcw className="size-4" aria-hidden />
                              Reset
                            </Button>
                            <Button
                              type="button"
                              variant="success"
                              shape="soft"
                              onClick={() => saveMobileEdit(line.id)}
                            >
                              <Check className="size-4" aria-hidden />
                              Save
                            </Button>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                </ExitsTableMobileRow>
              );
            })}
            <li className="exits-table-mobile__footer">
              <span>{t("incomingOrders.orderTotal")}</span>
              <MoneyDisplay amount={orderTotal} />
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
        <p className="m-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
          Pencil opens the Edit field menu from page-configured editable columns. Choose one field or
          Edit all — the chosen editor focuses automatically. Reset restores the original values; if
          already original, Reset exits edit mode. Escape also exits without saving. Product and Line
          Total stay read-only. Quantity here is sample data — real inventory stock changes use
          audited movements, not free overwrite.
        </p>
      </UiStandardsSection>

      <UiStandardsSection
        id="tables.alignment"
        title={t("uiStandards.tablesAlignmentTitle")}
        description="Header and value alignment must match. Sortable numeric headers keep label + icon at END."
        summary="START · END · CENTER · LOCKED"
        open={isOpen("tables.alignment")}
        onOpenChange={(open) => setOpen("tables.alignment", open)}
        testId="ui-standards-table-alignment"
      >
        <StaticSampleGroup title="ALIGNMENT MATRIX — APPROVED / LOCKED">
          <SampleFrame label="REFERENCE" className="sm:col-span-2" testId="ui-standards-table-align-matrix" command="EXITS TABLE" commandContext="Alignment matrix — header and value alignment must match. APPROVED / LOCKED.">
            <ExitsTableContainer>
              <ExitsTable>
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead cellAlign="text">Column type</ExitsTableHead>
                    <ExitsTableHead cellAlign="text">Header</ExitsTableHead>
                    <ExitsTableHead cellAlign="text">Value</ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {[
                    ["Text / Name", "START", "Apple"],
                    ["SKU / Code", "START", "PH-FRU-APPLE"],
                    ["Status", "START", "Active"],
                    ["Quantity", "END", "2 Kg"],
                    ["Money", "END", "₱180.00"],
                    ["Percentage", "END", "12%"],
                    ["Checkbox", "CENTER", "☐"],
                    ["Actions", "END", "⋯"],
                  ].map(([type, align, value]) => (
                    <ExitsTableRow key={type}>
                      <ExitsTableCell cellAlign="text">{type}</ExitsTableCell>
                      <ExitsTableCell
                        cellAlign={
                          align === "END" ? "numeric" : align === "CENTER" ? "center" : "text"
                        }
                      >
                        {align}
                      </ExitsTableCell>
                      <ExitsTableCell
                        cellAlign={
                          align === "END" ? "numeric" : align === "CENTER" ? "center" : "text"
                        }
                      >
                        {type === "Status" ? <StatusChip tone="success">Active</StatusChip> : value}
                      </ExitsTableCell>
                    </ExitsTableRow>
                  ))}
                </ExitsTableBody>
              </ExitsTable>
            </ExitsTableContainer>
          </SampleFrame>
        </StaticSampleGroup>
      </UiStandardsSection>

      <UiStandardsSection
        id="tables.actions"
        title={t("uiStandards.tablesActionsTitle")}
        description="Reuse locked Button Standard. Prefer 1–2 visible actions + More for secondary."
        summary="ICON · MORE · DANGER · LOCKED"
        open={isOpen("tables.actions")}
        onOpenChange={(open) => setOpen("tables.actions", open)}
        testId="ui-standards-table-actions"
      >
        <div className="grid gap-4">
          <StaticSampleGroup title="A · SINGLE ICON ACTION">
            <SampleFrame label="PENCIL" command="EXITS TABLE + ACTIONS ON">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
                      <ExitsTableHead cellAlign="actions">Actions</ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    <ExitsTableRow>
                      <ExitsTableCell cellAlign="text">Apple</ExitsTableCell>
                      <ExitsTableCell cellAlign="actions">
                        <ExitsTableActions>
                          <IconAction label="Edit Apple">
                            <Pencil className="size-4" aria-hidden />
                          </IconAction>
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="B · MULTIPLE ICON ACTIONS">
            <SampleFrame label="EYE · PENCIL · MORE" command="EXITS TABLE + ACTIONS ON" commandContext="Sample B — Eye · Pencil · More from /ui-standards → Tables.">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
                      <ExitsTableHead cellAlign="actions">Actions</ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    <ExitsTableRow>
                      <ExitsTableCell cellAlign="text">Apple</ExitsTableCell>
                      <ExitsTableCell cellAlign="actions">
                        <ExitsTableActions>
                          <IconAction label="View Apple" variant="info">
                            <Eye className={cn("size-4", buttonIconMotion.view)} aria-hidden />
                          </IconAction>
                          <IconAction label="Edit Apple">
                            <Pencil className="size-4" aria-hidden />
                          </IconAction>
                          <MoreActionsMenu productName="Apple" />
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="C · PRIMARY + MORE">
            <SampleFrame label="EDIT + MORE" command="EXITS TABLE + ACTIONS ON" commandContext="Sample C — Edit + More from /ui-standards → Tables.">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
                      <ExitsTableHead cellAlign="actions">Actions</ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    <ExitsTableRow>
                      <ExitsTableCell cellAlign="text">Apple</ExitsTableCell>
                      <ExitsTableCell cellAlign="actions">
                        <ExitsTableActions>
                          <Button type="button" variant="ghost" shape="soft">
                            Edit
                          </Button>
                          <MoreActionsMenu productName="Apple" />
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="D · DESTRUCTIVE">
            <SampleFrame label="EDIT + DELETE" command="EXITS TABLE + ACTIONS ON" commandContext="Sample D — Edit + Delete from /ui-standards → Tables.">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
                      <ExitsTableHead cellAlign="actions">Actions</ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    <ExitsTableRow>
                      <ExitsTableCell cellAlign="text">Apple</ExitsTableCell>
                      <ExitsTableCell cellAlign="actions">
                        <ExitsTableActions>
                          <IconAction label="Edit Apple">
                            <Pencil className="size-4" aria-hidden />
                          </IconAction>
                          <IconAction label="Delete Apple" variant="destructive">
                            <Trash2 className={cn("size-4", buttonIconMotion.delete)} aria-hidden />
                          </IconAction>
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            ACTION DENSITY GUIDANCE — APPROVED / LOCKED. Prefer icon
            actions in dense tables; avoid listing View Edit Duplicate Archive Delete History Print
            as separate buttons every row.
          </p>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="tables.inline-row-edit"
        title={t("uiStandards.tablesInlineRowEditTitle")}
        description="Pencil → Edit field menu → stealth editors. Page owns editable columns, drafts, and validation."
        summary="FIELD MENU · STEALTH · LOCKED"
        open={isOpen("tables.inline-row-edit")}
        onOpenChange={(open) => setOpen("tables.inline-row-edit", open)}
        testId="ui-standards-table-inline-row-edit"
      >
        <div className="grid gap-4">
          <StaticSampleGroup title="A · NORMAL TABLE">
            <SampleFrame label="READ-ONLY CELLS" testId="ui-standards-inline-sample-normal" command="EXITS TABLE + ACTIONS ON + INLINE EDIT OFF">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
                      <ExitsTableHead cellAlign="text" colSize="sku">
                        SKU
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="numeric" colSize="numeric">
                        Quantity
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="money" colSize="money">
                        Unit cost
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="money" colSize="money">
                        Line total
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="actions" colSize="actions">
                        Actions
                      </ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    <ExitsTableRow data-testid="ui-standards-inline-row-apple">
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        Apple
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" colSize="sku" className="text-muted">
                        PH-FRU-APPLE
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" colSize="numeric">
                        2 Kg
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money">
                        <MoneyDisplay amount={180} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money" emphasis="semibold">
                        <MoneyDisplay amount={360} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions" colSize="actions">
                        <ExitsTableActions>
                          <ExitsTableEditMenu
                            fields={DEMO_EDITABLE_FIELDS}
                            ariaLabel="Edit Apple sample"
                            onSelectField={() => undefined}
                            onEditAll={() => undefined}
                          />
                          <MoreActionsMenu productName="Apple" />
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="B · EDIT FIELD MENU">
            <SampleFrame label="PENCIL DROPDOWN" testId="ui-standards-inline-sample-menu" hint="Open Pencil in the main demo for the live menu." command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="Edit field menu (Pencil dropdown).">
              <ExitsTableActions>
                <ExitsTableEditMenu
                  fields={DEMO_EDITABLE_FIELDS}
                  ariaLabel="Edit field menu sample"
                  data-testid="ui-standards-edit-menu-sample"
                  onSelectField={() => undefined}
                  onEditAll={() => undefined}
                />
              </ExitsTableActions>
            </SampleFrame>
            <SampleFrame label="LABELED TRIGGER (LESS DENSE — OPTIONAL)" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="Labeled Edit trigger (less dense — optional).">
              <ExitsTableEditMenu
                fields={DEMO_EDITABLE_FIELDS}
                ariaLabel="Edit with label"
                trigger="button"
                onSelectField={() => undefined}
                onEditAll={() => undefined}
              />
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="C · EDIT SKU ONLY">
            <SampleFrame label="STEALTH SKU" testId="ui-standards-inline-sample-sku" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="EDITABLE: SKU">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableBody>
                    <ExitsTableRow editing>
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        Apple
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" colSize="sku">
                        <ExitsTableInlineEditor align="start">
                          <Input label="SKU for Apple" defaultValue="PH-FRU-APPLE" readOnly />
                        </ExitsTableInlineEditor>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" colSize="numeric">
                        2 Kg
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money">
                        <MoneyDisplay amount={180} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money" emphasis="semibold">
                        <MoneyDisplay amount={360} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions" colSize="actions">
                        <ExitsTableActions>
                          <IconAction label="Save Apple changes" variant="success">
                            <Check className="size-4" aria-hidden />
                          </IconAction>
                          <IconAction
                            label="Reset Apple to original"
                            title="Reset"
                            className="exits-table__action-reset"
                          >
                            <RotateCcw className="size-4" aria-hidden />
                          </IconAction>
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="D · EDIT QUANTITY ONLY">
            <SampleFrame label="[2] Kg" testId="ui-standards-inline-sample-qty" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="EDITABLE: QUANTITY">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableBody>
                    <ExitsTableRow editing>
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        Apple
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" colSize="sku" className="text-muted">
                        PH-FRU-APPLE
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" colSize="numeric">
                        <div className="exits-table__qty-edit">
                          <ExitsTableInlineEditor>
                            <QuantityInput label="Quantity for Apple" defaultValue="2" readOnly />
                          </ExitsTableInlineEditor>
                          <span className="exits-table__uom">Kg</span>
                        </div>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money">
                        <MoneyDisplay amount={180} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money" emphasis="semibold">
                        <MoneyDisplay amount={360} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions" colSize="actions">
                        <ExitsTableActions>
                          <IconAction label="Save Apple changes" variant="success">
                            <Check className="size-4" aria-hidden />
                          </IconAction>
                          <IconAction
                            label="Reset Apple to original"
                            title="Reset"
                            className="exits-table__action-reset"
                          >
                            <RotateCcw className="size-4" aria-hidden />
                          </IconAction>
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="E · EDIT UNIT COST ONLY">
            <SampleFrame
              label="₱ [180.00]"
              testId="ui-standards-inline-sample-unit-cost" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="EDITABLE: UNIT COST">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableBody>
                    <ExitsTableRow editing>
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        Apple
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" colSize="sku" className="text-muted">
                        PH-FRU-APPLE
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" colSize="numeric">
                        2 Kg
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money">
                        <div className="exits-table__money-edit">
                          <span className="exits-table__currency-prefix" aria-hidden>
                            ₱
                          </span>
                          <ExitsTableInlineEditor>
                            <MoneyInput label="Unit cost for Apple" defaultValue="180.00" readOnly />
                          </ExitsTableInlineEditor>
                        </div>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money" emphasis="semibold">
                        <MoneyDisplay amount={360} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions" colSize="actions">
                        <ExitsTableActions>
                          <IconAction label="Save Apple changes" variant="success">
                            <Check className="size-4" aria-hidden />
                          </IconAction>
                          <IconAction
                            label="Reset Apple to original"
                            title="Reset"
                            className="exits-table__action-reset"
                          >
                            <RotateCcw className="size-4" aria-hidden />
                          </IconAction>
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="F · EDIT ALL">
            <SampleFrame
              label="ALL EDITABLE FIELDS"
              testId="ui-standards-inline-sample-edit-all" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="EDITABLE: SKU, QUANTITY, UNIT COST">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableBody>
                    <ExitsTableRow editing>
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        Apple
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" colSize="sku">
                        <ExitsTableInlineEditor align="start">
                          <Input label="SKU for Apple" defaultValue="PH-FRU-APPLE" readOnly />
                        </ExitsTableInlineEditor>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" colSize="numeric">
                        <div className="exits-table__qty-edit">
                          <ExitsTableInlineEditor>
                            <QuantityInput label="Quantity for Apple" defaultValue="2" readOnly />
                          </ExitsTableInlineEditor>
                          <span className="exits-table__uom">Kg</span>
                        </div>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money">
                        <div className="exits-table__money-edit">
                          <span className="exits-table__currency-prefix" aria-hidden>
                            ₱
                          </span>
                          <ExitsTableInlineEditor>
                            <MoneyInput label="Unit cost for Apple" defaultValue="180.00" readOnly />
                          </ExitsTableInlineEditor>
                        </div>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money" colSize="money" emphasis="semibold">
                        <MoneyDisplay amount={360} />
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions" colSize="actions">
                        <ExitsTableActions>
                          <IconAction label="Save Apple changes" variant="success">
                            <Check className="size-4" aria-hidden />
                          </IconAction>
                          <IconAction
                            label="Reset Apple to original"
                            title="Reset"
                            className="exits-table__action-reset"
                          >
                            <RotateCcw className="size-4" aria-hidden />
                          </IconAction>
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="G · VALIDATION ERROR">
            <SampleFrame label="DANGER BORDER + ROW MESSAGE" testId="ui-standards-inline-sample-validation" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="Validation — danger border + row message.">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableBody>
                    <ExitsTableRow editing>
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        Apple
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric" colSize="numeric">
                        <div className="exits-table__qty-edit">
                          <ExitsTableInlineEditor invalid>
                            <QuantityInput
                              label="Quantity for Apple"
                              defaultValue="-1"
                              readOnly
                              aria-invalid
                              aria-describedby="ui-standards-sample-qty-err"
                            />
                          </ExitsTableInlineEditor>
                          <span className="exits-table__uom">Kg</span>
                        </div>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions" colSize="actions">
                        <ExitsTableActions>
                          <IconAction label="Save Apple changes" variant="success">
                            <Check className="size-4" aria-hidden />
                          </IconAction>
                          <IconAction
                            label="Reset Apple to original"
                            title="Reset"
                            className="exits-table__action-reset"
                          >
                            <RotateCcw className="size-4" aria-hidden />
                          </IconAction>
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                    <ExitsTableRow error>
                      <ExitsTableCell colSpan={3}>
                        <div
                          id="ui-standards-sample-qty-err"
                          className="exits-table__row-validation"
                          role="alert"
                        >
                          Quantity must be greater than zero.
                        </div>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="SINGLE-FIELD SHORTCUT — CANDIDATE">
            <SampleFrame
              label="CANDIDATE (NOT DEFAULT)"
              hint="If exactly one field is editable, Pencil may later open that field directly. Locked default remains the field-menu architecture." explanatory={true}>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                Candidate only — field menu remains the locked default Edit path.
              </p>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="TEXT SAVE / RESET (LESS DENSE)">
            <SampleFrame label="LABELED ACTIONS" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="Labeled Save / Reset (less dense).">
              <ExitsTableActions>
                <Button type="button" variant="success" shape="soft">
                  <Check className="size-4" aria-hidden />
                  Save
                </Button>
                <Button type="button" variant="ghost" shape="soft" className="exits-table__action-reset">
                  <RotateCcw className="size-4" aria-hidden />
                  Reset
                </Button>
              </ExitsTableActions>
            </SampleFrame>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="tables.inline-cell-edit"
        title={t("uiStandards.tablesInlineCellEditTitle")}
        description="CELL EDIT is special dense/data-management use. ROW EDIT remains the preferred general candidate."
        summary="CELL EDIT · SPECIAL · LOCKED"
        open={isOpen("tables.inline-cell-edit")}
        onOpenChange={(open) => setOpen("tables.inline-cell-edit", open)}
        testId="ui-standards-table-inline-cell-edit"
      >
        <SampleFrame
          label="CELL EDIT"
          testId="ui-standards-table-cell-edit-sample"
          command="EXITS TABLE + INLINE EDIT ON"
          commandContext="CELL EDIT — special dense/data-management use. Prefer ROW EDIT for general cases. EDITABLE: UNIT COST"
        >
          <ExitsTableContainer>
            <ExitsTable>
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
                  <ExitsTableHead cellAlign="money">Unit cost</ExitsTableHead>
                </ExitsTableRow>
              </ExitsTableHeader>
              <ExitsTableBody>
                <ExitsTableRow editing={cellEditing}>
                  <ExitsTableCell cellAlign="text">Apple</ExitsTableCell>
                  <ExitsTableCell
                    cellAlign="money"
                    onDoubleClick={() => setCellEditing(true)}
                    data-testid="ui-standards-table-cell-edit"
                  >
                    {cellEditing ? (
                      <ExitsTableInlineEditor>
                        <MoneyInput
                          label="Unit cost for Apple"
                          value={cellEditValue}
                          onChange={(e) => setCellEditValue(e.target.value)}
                          onBlur={() => setCellEditing(false)}
                          onKeyDown={(e) => {
                            if (e.key === "Enter" || e.key === "Escape") setCellEditing(false);
                          }}
                        />
                      </ExitsTableInlineEditor>
                    ) : (
                      <button
                        type="button"
                        className="tabular-nums text-end underline-offset-2 hover:underline"
                        onClick={() => setCellEditing(true)}
                      >
                        {formatPeso(Number(cellEditValue) || 0)}
                      </button>
                    )}
                  </ExitsTableCell>
                </ExitsTableRow>
              </ExitsTableBody>
            </ExitsTable>
          </ExitsTableContainer>
        </SampleFrame>
      </UiStandardsSection>

      <UiStandardsSection
        id="tables.validation"
        title={t("uiStandards.tablesValidationTitle")}
        description="Semantic Danger for errors. Saving disables editors. No real API."
        summary="ERROR · ROW ERROR · SAVING · LOCKED"
        open={isOpen("tables.validation")}
        onOpenChange={(open) => setOpen("tables.validation", open)}
        testId="ui-standards-table-validation"
      >
        <div className="grid gap-4">
          <StaticSampleGroup title="INLINE EDIT — VALIDATION ERROR">
            <SampleFrame label="QUANTITY ERROR" testId="ui-standards-table-validation-qty" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="Validation — quantity error.">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
                      <ExitsTableHead cellAlign="numeric">Quantity</ExitsTableHead>
                      <ExitsTableHead cellAlign="actions">Actions</ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    <ExitsTableRow editing>
                      <ExitsTableCell cellAlign="text">Apple</ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric">
                        <ExitsTableInlineEditor
                          error="Quantity must be greater than zero."
                          errorId={qtyErrorId}
                        >
                          <QuantityInput
                            label="Quantity for Apple"
                            value="-1"
                            aria-invalid
                            aria-describedby={qtyErrorId}
                            readOnly
                          />
                        </ExitsTableInlineEditor>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions">
                        <ExitsTableActions>
                          <IconAction label="Save Apple changes" variant="success">
                            <Check className="size-4" aria-hidden />
                          </IconAction>
                          <IconAction
                            label="Reset Apple to original"
                            title="Reset"
                            className="exits-table__action-reset"
                          >
                            <RotateCcw className="size-4" aria-hidden />
                          </IconAction>
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="ROW-LEVEL ERROR">
            <SampleFrame label="SAVE FAILED" testId="ui-standards-table-row-error" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="Row-level save failed.">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableBody>
                    <ExitsTableRow error>
                      <ExitsTableCell colSpan={2}>
                        <div className="exits-table__row-error">
                          <span>Could not save changes. Try again.</span>
                          <ExitsTableActions>
                            <Button type="button" variant="outline" shape="soft">
                              Retry
                            </Button>
                            <Button type="button" variant="ghost" shape="soft">
                              Cancel
                            </Button>
                          </ExitsTableActions>
                        </div>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="SAVING">
            <SampleFrame label="DISABLED EDITORS" testId="ui-standards-table-saving" command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON" commandContext="Saving — editors disabled.">
              <ExitsTableContainer>
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
                      <ExitsTableHead cellAlign="numeric">Quantity</ExitsTableHead>
                      <ExitsTableHead cellAlign="money">Unit cost</ExitsTableHead>
                      <ExitsTableHead cellAlign="actions">Actions</ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    <ExitsTableRow editing>
                      <ExitsTableCell cellAlign="text">Apple</ExitsTableCell>
                      <ExitsTableCell cellAlign="numeric">
                        <ExitsTableInlineEditor>
                          <QuantityInput label="Quantity for Apple" value="2" disabled />
                        </ExitsTableInlineEditor>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="money">
                        <ExitsTableInlineEditor>
                          <MoneyInput label="Unit cost for Apple" value="180.00" disabled />
                        </ExitsTableInlineEditor>
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions">
                        <ExitsTableActions>
                          <Button type="button" variant="success" shape="soft" disabled>
                            <LoaderCircle className="size-4 animate-spin" aria-hidden />
                            Saving
                          </Button>
                        </ExitsTableActions>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  </ExitsTableBody>
                </ExitsTable>
              </ExitsTableContainer>
            </SampleFrame>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="tables.sticky-actions"
        title={t("uiStandards.tablesStickyActionsTitle")}
        description="SPECIAL USE / CANDIDATE — sticky Actions at inline-end for wide tables. Not default."
        summary="STICKY ACTIONS · CANDIDATE"
        open={isOpen("tables.sticky-actions")}
        onOpenChange={(open) => setOpen("tables.sticky-actions", open)}
        testId="ui-standards-table-sticky-actions"
      >
        <SampleFrame
          label="STICKY ACTIONS"
          command="EXITS TABLE + ACTIONS ON + STICKY ACTIONS"
          commandContext="SPECIAL USE / CANDIDATE — sticky Actions at inline-end for wide tables. Not default."
        >
          <ExitsTableContainer>
            <ExitsTable className="min-w-[56rem]">
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead cellAlign="text">Product</ExitsTableHead>
                  <ExitsTableHead cellAlign="text">SKU</ExitsTableHead>
                  <ExitsTableHead cellAlign="text">Warehouse note</ExitsTableHead>
                  <ExitsTableHead cellAlign="numeric">Quantity</ExitsTableHead>
                  <ExitsTableHead cellAlign="money">Unit cost</ExitsTableHead>
                  <ExitsTableHead cellAlign="money">Line total</ExitsTableHead>
                  <ExitsTableHead cellAlign="actions" stickyEnd>
                    Actions
                  </ExitsTableHead>
                </ExitsTableRow>
              </ExitsTableHeader>
              <ExitsTableBody>
                <ExitsTableRow>
                  <ExitsTableCell cellAlign="text">Apple</ExitsTableCell>
                  <ExitsTableCell cellAlign="text">PH-FRU-APPLE</ExitsTableCell>
                  <ExitsTableCell cellAlign="text">
                    Preferred cold-room bin A12 · supplier lot verified
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="numeric">2 Kg</ExitsTableCell>
                  <ExitsTableCell cellAlign="money">
                    <MoneyDisplay amount={180} />
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="money" emphasis="semibold">
                    <MoneyDisplay amount={360} />
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="actions" stickyEnd>
                    <ExitsTableActions>
                      <IconAction label="Edit Apple">
                        <Pencil className="size-4" aria-hidden />
                      </IconAction>
                    </ExitsTableActions>
                  </ExitsTableCell>
                </ExitsTableRow>
              </ExitsTableBody>
            </ExitsTable>
          </ExitsTableContainer>
        </SampleFrame>
      </UiStandardsSection>

      <UiStandardsSection
        id="tables.mobile-edit"
        title={t("uiStandards.tablesMobileEditTitle")}
        description="Mobile uses the same Edit field menu. Chosen fields become a compact stacked form with labeled Reset/Save."
        summary="MOBILE · FIELD MENU · LOCKED"
        open={isOpen("tables.mobile-edit")}
        onOpenChange={(open) => setOpen("tables.mobile-edit", open)}
        testId="ui-standards-table-mobile-edit"
      >
        <SampleFrame
          label="MOBILE EDIT"
          command="EXITS TABLE + ACTIONS ON + INLINE EDIT ON"
          commandContext="Mobile stacked edit form — same Edit field menu; EDITABLE: SKU, QUANTITY, UNIT COST"
        >
        <ExitsTableContainer>
          <ExitsTableMobile className="!flex md:!flex">
            {(() => {
              const apple = lines.find((line) => line.id === "apple") ?? DEMO_LINES_SEED[0];
              const mobileEditing = mobileEditingId === apple.id;
              const mobileTotal = mobileEditing
                ? previewLineTotal(apple, mobileEditingFields)
                : apple.lineTotal;
              return (
                <ExitsTableMobileRow editing={mobileEditing}>
                  <p className="exits-table-mobile__title">{apple.name}</p>
                  {!mobileEditing ? (
                    <>
                      <p className="exits-table-mobile__meta">SKU: {apple.sku}</p>
                      <p className="exits-table-mobile__math">Quantity: {qtyLabel(apple)}</p>
                      <p className="exits-table-mobile__math">
                        Unit cost: {formatPeso(apple.unitCost)}
                      </p>
                      <p className="exits-table-mobile__total">
                        Line total: {formatPeso(apple.lineTotal)}
                      </p>
                      <div className="exits-table-mobile__actions">
                        <Button type="button" variant="outline" shape="soft">
                          View
                        </Button>
                        <ExitsTableEditMenu
                          fields={DEMO_EDITABLE_FIELDS}
                          ariaLabel={`Edit ${apple.name}`}
                          trigger="button"
                          onSelectField={(key) => startMobileFieldEdit(apple, key)}
                          onEditAll={() => startMobileEditAll(apple)}
                        />
                        <MoreActionsMenu productName={apple.name} />
                      </div>
                    </>
                  ) : (
                    <div className="exits-table-mobile__edit-form">
                      {isMobileEditingField("sku") ? (
                        <label>
                          SKU
                          <input
                            value={editSku}
                            onChange={(e) => setEditSku(e.target.value)}
                            aria-label={`SKU for ${apple.name}`}
                          />
                        </label>
                      ) : (
                        <p className="exits-table-mobile__meta">SKU: {apple.sku}</p>
                      )}
                      {isMobileEditingField("quantity") ? (
                        <>
                          <label>
                            Quantity
                            <input
                              value={editQty}
                              onChange={(e) => setEditQty(e.target.value)}
                              inputMode="decimal"
                              aria-label={`Quantity for ${apple.name}`}
                            />
                          </label>
                          <span className="exits-table__uom">
                            {formatUnitOfMeasureLabel(apple.unitOfMeasureCode)}
                          </span>
                        </>
                      ) : (
                        <p className="exits-table-mobile__math">Quantity: {qtyLabel(apple)}</p>
                      )}
                      {isMobileEditingField("unitCost") ? (
                        <label>
                          Unit cost
                          <span className="exits-table__money-edit">
                            <span className="exits-table__currency-prefix" aria-hidden>
                              ₱
                            </span>
                            <input
                              value={editUnitCost}
                              onChange={(e) => setEditUnitCost(e.target.value)}
                              inputMode="decimal"
                              aria-label={`Unit cost for ${apple.name}`}
                            />
                          </span>
                        </label>
                      ) : (
                        <p className="exits-table-mobile__math">
                          Unit cost: {formatPeso(apple.unitCost)}
                        </p>
                      )}
                      <p className="exits-table-mobile__total m-0">
                        Line total: {formatPeso(mobileTotal)}
                      </p>
                      <div className="exits-table-mobile__edit-actions">
                        <Button
                          type="button"
                          variant="ghost"
                          shape="soft"
                          className="exits-table__action-reset"
                          onClick={() => resetMobileEdit()}
                        >
                          <RotateCcw className="size-4" aria-hidden />
                          Reset
                        </Button>
                        <Button
                          type="button"
                          variant="success"
                          shape="soft"
                          onClick={() => saveMobileEdit(apple.id)}
                        >
                          <Check className="size-4" aria-hidden />
                          Save
                        </Button>
                      </div>
                    </div>
                  )}
                </ExitsTableMobileRow>
              );
            })()}
          </ExitsTableMobile>
        </ExitsTableContainer>
        </SampleFrame>
      </UiStandardsSection>

      <UiStandardsSection
        id="tables.cheatsheet"
        title={t("uiStandards.tableCheatTitle")}
        description={t("uiStandards.tableCheatLede")}
        summary="Cursor shorthand · APPROVED / LOCKED"
        open={isOpen("tables.cheatsheet")}
        onOpenChange={(open) => setOpen("tables.cheatsheet", open)}
        testId="ui-standards-table-cheatsheet"
      >
        <SampleFrame label="CHEATSHEET" explanatory>
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

ACTIONS ON / OFF
INLINE EDIT ON / OFF
EDIT MODE:
  FIELD MENU
EDIT ALL
RESET
STICKY ACTIONS (SPECIAL)

EDITABLE:
  SKU
  QUANTITY
  UNIT COST

OUTPUT ICONS = CSV + XLSX + PDF + Print
PAGE SIZE = 10 / 25 / 50 / 100 (default 25)

Examples:
EXITS TABLE
ACTIONS ON
INLINE EDIT ON
EDIT MODE: FIELD MENU
EDITABLE: SKU, QUANTITY, UNIT COST

EXITS TABLE
INLINE EDIT ON
EDITABLE: PRICE

EXITS TABLE
ACTIONS ON
INLINE EDIT OFF

SINGLE-FIELD SHORTCUT — CANDIDATE
(if exactly one editable field, Pencil may open it directly — not default)

Inventory note:
Quantity in UI Standards is DEMO DATA.
Real stock uses audited movements — not free overwrite.

TABLE STANDARD
APPROVED / LOCKED
Docs/UI/exits-table-standard.md`}
        </pre>
        </SampleFrame>
      </UiStandardsSection>
    </div>
  );
}
