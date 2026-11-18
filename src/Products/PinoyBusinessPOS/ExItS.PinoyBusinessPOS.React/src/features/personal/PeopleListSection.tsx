import { Check, Download, HandCoins, Pencil, SlidersHorizontal, Users, Wallet } from "lucide-react";
import { useEffect, useMemo, useState, type CSSProperties, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { CountBadge } from "@/components/exits/CountChip";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { ExitsOrgEhgInput } from "@/components/exits/ExitsOrgEhgInput";
import {
  ExitsFileFormatIcon,
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableOutputActions,
  ExitsTablePagination,
  ExitsTableRow,
  ExitsTableToolbar,
} from "@/components/exits/ExitsTable";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { PeopleAddGlassSpeedDial } from "@/features/personal/PeopleAddGlassSpeedDial";
import { PeopleMobileRow } from "@/features/personal/PeopleMobileRow";
import {
  type PeopleConnectionStatus,
  type PeopleRowModel,
} from "@/features/personal/people-status";
import { EditLocalContactDialog } from "@/features/personal/EditLocalContactDialog";
import { useMediaMin } from "@/hooks/useMediaQuery";
import {
  buildPeopleTableExportModel,
  downloadPeopleTableCsv,
  downloadPeopleTablePdf,
  downloadPeopleTableXlsx,
  printPeopleTable,
  type PeopleTableExportLabels,
} from "@/features/personal/people-table-output";
import {
  TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_FOOTER_INTENT,
  TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_HEADER_INTENT,
} from "@/features/ui-standards/table-global-default";
import { useI18n } from "@/i18n/I18nProvider";

export type PeopleListFilter = "all" | "connected" | "pending" | "local" | "not_connected";
export type PeopleListSort = "name" | "amount";

const FILTERS: ReadonlyArray<{
  key: PeopleListFilter;
  labelKey:
    | "people.filter.all"
    | "people.filter.connected"
    | "people.filter.pending"
    | "people.filter.local"
    | "people.filter.notConnected";
}> = [
  { key: "all", labelKey: "people.filter.all" },
  { key: "connected", labelKey: "people.filter.connected" },
  { key: "pending", labelKey: "people.filter.pending" },
  { key: "local", labelKey: "people.filter.local" },
  { key: "not_connected", labelKey: "people.filter.notConnected" },
];

function statusTone(
  status: PeopleConnectionStatus,
): "neutral" | "success" | "warning" | "info" | "danger" {
  if (status === "connected") {
    return "success";
  }
  if (status === "blocked") {
    return "danger";
  }
  if (status === "request_sent" || status === "request_received") {
    return "warning";
  }
  if (status === "local") {
    return "info";
  }
  return "neutral";
}

function matchesFilter(row: PeopleRowModel, filter: PeopleListFilter): boolean {
  switch (filter) {
    case "connected":
      return row.connectionStatus === "connected";
    case "pending":
      return (
        row.connectionStatus === "request_sent" || row.connectionStatus === "request_received"
      );
    case "local":
      return row.connectionStatus === "local";
    case "not_connected":
      return row.connectionStatus === "not_connected";
    default:
      return true;
  }
}

function peopleRowAmount(row: PeopleRowModel): number {
  return row.utang?.balance ?? 0;
}

export function filterPeopleRows(
  rows: PeopleRowModel[],
  filter: PeopleListFilter,
  search: string,
  sort: PeopleListSort = "name",
): PeopleRowModel[] {
  const needle = search.trim().toLowerCase();
  return rows
    .filter((row) => matchesFilter(row, filter))
    .filter((row) => {
      if (!needle) {
        return true;
      }
      const hay = `${row.contact.displayName} ${row.publicUserId ?? ""}`.toLowerCase();
      return hay.includes(needle);
    })
    .sort((a, b) => {
      if (sort === "amount") {
        const amountDelta = peopleRowAmount(b) - peopleRowAmount(a);
        if (amountDelta !== 0) {
          return amountDelta;
        }
      }
      return a.contact.displayName.localeCompare(b.contact.displayName);
    });
}

function PeopleUtangTotal({
  total,
  count,
  testId,
  href,
  owe = false,
}: {
  total: number;
  count: number;
  testId: string;
  href: string;
  owe?: boolean;
}) {
  if (count === 0) {
    return (
      <Link to={href} className="people-table-amount people-table-amount--empty text-muted no-underline" data-testid={testId}>
        —
      </Link>
    );
  }
  return (
    <Link to={href} className="people-table-amount inline-flex items-center gap-2 no-underline" data-testid={`${testId}-link`}>
      <MoneyDisplay
        amount={total}
        testId={testId}
        className={owe && total > 0 ? "text-[var(--exits-warning)]" : undefined}
      />
      <CountBadge count={count} tone={owe ? "warning" : "neutral"} />
    </Link>
  );
}

const PEOPLE_PAGE_SIZES = [10, 25, 50, 100, 200, 500] as const;

function PeopleTableOutput({
  rows,
  statusLabel,
  appearance = "auto",
  className,
}: {
  rows: PeopleRowModel[];
  statusLabel: (status: PeopleConnectionStatus) => string;
  appearance?: "auto" | "icons";
  className?: string;
}) {
  const { t } = useI18n();
  const exportLabels: PeopleTableExportLabels = {
    name: t("people.displayName"),
    contact: t("people.column.contact"),
    email: t("people.column.email"),
    status: t("people.column.status"),
    lent: t("people.detail.iLentAction"),
    owe: t("people.detail.iOweAction"),
  };

  function exportModel() {
    return buildPeopleTableExportModel(rows, statusLabel);
  }

  return (
    <ExitsTableOutputActions
      appearance={appearance}
      className={className}
      csvLabel={t("exitsTable.exportCsv")}
      xlsxLabel={t("exitsTable.exportExcel")}
      pdfLabel={t("exitsTable.exportPdf")}
      printLabel={t("exitsTable.print")}
      menuLabel={t("exitsTable.exportPrintMenu")}
      disabled={rows.length === 0}
      onCsv={() => downloadPeopleTableCsv(exportModel(), exportLabels)}
      onXlsx={() => downloadPeopleTableXlsx(exportModel(), exportLabels)}
      onPdf={() => downloadPeopleTablePdf(exportModel(), exportLabels)}
      onPrint={printPeopleTable}
    />
  );
}

function PeopleSearchSort({
  sort,
  onSort,
  open,
  onOpenChange,
  testId = "people-sort",
}: {
  sort: PeopleListSort;
  onSort: (next: PeopleListSort) => void;
  open: boolean;
  onOpenChange: (next: boolean) => void;
  testId?: string;
}) {
  const { t } = useI18n();

  return (
    <DropdownMenu
      open={open}
      onOpenChange={onOpenChange}
      align="end"
      menuLabel={t("people.sort.label")}
      className="people-search-sort-wrap"
      menuClassName="people-search-sort-menu"
      trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
        <ExitsTooltip content={t("people.sort.label")}>
          <button
            type="button"
            id={id}
            className="people-search-sort"
            aria-label={t("people.sort.label")}
            aria-haspopup="menu"
            aria-expanded={expanded}
            aria-controls={controls}
            data-testid={testId}
            onClick={onClick}
            onKeyDown={onKeyDown}
          >
            <SlidersHorizontal className="size-[0.95rem] shrink-0" strokeWidth={2.25} aria-hidden />
          </button>
        </ExitsTooltip>
      )}
    >
      <MenuItem
        data-testid={`${testId}-name`}
        onSelect={() => {
          onSort("name");
          onOpenChange(false);
        }}
      >
        <span className="people-search-sort-menu__check" aria-hidden>
          {sort === "name" ? <Check className="size-3.5" strokeWidth={2.5} /> : null}
        </span>
        {t("people.sort.name")}
      </MenuItem>
      <MenuItem
        data-testid={`${testId}-amount`}
        onSelect={() => {
          onSort("amount");
          onOpenChange(false);
        }}
      >
        <span className="people-search-sort-menu__check" aria-hidden>
          {sort === "amount" ? <Check className="size-3.5" strokeWidth={2.5} /> : null}
        </span>
        {t("people.sort.amount")}
      </MenuItem>
    </DropdownMenu>
  );
}

function PeopleListSummaryBar({
  total,
  connected,
  local,
  downloadDisabled,
  onCsv,
  onXlsx,
  onPdf,
}: {
  total: number;
  connected: number;
  local: number;
  downloadDisabled: boolean;
  onCsv: () => void;
  onXlsx: () => void;
  onPdf: () => void;
}) {
  const { t } = useI18n();
  const downloadMenu = useDismissibleOpen(false);

  return (
    <div className="people-list-summary" data-testid="people-list-summary">
      <div className="people-list-summary__info">
        <span className="people-list-summary__icon" aria-hidden>
          <Users className="size-[1.05rem] shrink-0" strokeWidth={2.25} />
        </span>
        <div className="people-list-summary__text">
          <p className="people-list-summary__count">
            {t("people.summaryBar.count").replace("{count}", String(total))}
          </p>
          <p className="people-list-summary__meta">
            {t("people.summaryBar.meta")
              .replace("{connected}", String(connected))
              .replace("{local}", String(local))}
          </p>
        </div>
      </div>
      <DropdownMenu
        open={downloadMenu.open}
        onOpenChange={downloadMenu.setOpen}
        align="end"
        menuLabel={t("people.summaryBar.download")}
        className="people-list-summary__download-wrap"
        menuClassName="people-list-summary__download-menu"
        trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
          <ExitsTooltip content={t("people.summaryBar.download")}>
            <button
              type="button"
              id={id}
              className="people-list-summary__download"
              aria-label={t("people.summaryBar.download")}
              aria-haspopup="menu"
              aria-expanded={expanded}
              aria-controls={controls}
              data-testid="people-list-summary-download"
              disabled={downloadDisabled}
              onClick={onClick}
              onKeyDown={onKeyDown}
            >
              <Download className="size-[1.05rem] shrink-0" strokeWidth={2.25} aria-hidden />
            </button>
          </ExitsTooltip>
        )}
      >
        <MenuItem
          data-testid="people-list-summary-csv"
          disabled={downloadDisabled}
          onSelect={() => {
            onCsv();
            downloadMenu.setOpen(false);
          }}
        >
          <ExitsFileFormatIcon format="csv" />
          {t("exitsTable.exportCsv")}
        </MenuItem>
        <MenuItem
          data-testid="people-list-summary-xlsx"
          disabled={downloadDisabled}
          onSelect={() => {
            onXlsx();
            downloadMenu.setOpen(false);
          }}
        >
          <ExitsFileFormatIcon format="xlsx" />
          {t("exitsTable.exportExcel")}
        </MenuItem>
        <MenuItem
          data-testid="people-list-summary-pdf"
          disabled={downloadDisabled}
          onSelect={() => {
            onPdf();
            downloadMenu.setOpen(false);
          }}
        >
          <ExitsFileFormatIcon format="pdf" />
          {t("exitsTable.exportPdf")}
        </MenuItem>
      </DropdownMenu>
    </div>
  );
}

function PeopleTable({
  rows,
  statusLabel,
  search,
  onSearch,
  sort,
  onSort,
  sortMenuOpen,
  onSortMenuOpenChange,
  empty,
  showOutput = true,
}: {
  rows: PeopleRowModel[];
  statusLabel: (status: PeopleConnectionStatus) => string;
  search: string;
  onSearch: (value: string) => void;
  sort: PeopleListSort;
  onSort: (next: PeopleListSort) => void;
  sortMenuOpen: boolean;
  onSortMenuOpenChange: (next: boolean) => void;
  empty: { title: string; detail: string; variant: "default" | "filtered" } | null;
  showOutput?: boolean;
}) {
  const { t } = useI18n();
  const [editingContact, setEditingContact] = useState<PeopleRowModel["contact"] | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<(typeof PEOPLE_PAGE_SIZES)[number]>(10);

  useEffect(() => {
    setPage(1);
  }, [rows]);

  const pageCount = Math.max(1, Math.ceil(rows.length / pageSize) || 1);
  const safePage = Math.min(page, pageCount);
  const pageRows = rows.slice((safePage - 1) * pageSize, safePage * pageSize);

  const tableChromeStyle = {
    "--qheader-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
    "--qheader-ink": "var(--exits-text, #000)",
    "--qfooter-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
    "--qfooter-ink": "var(--exits-text, #000)",
  } as CSSProperties;

  return (
    <ExitsTableContainer
      className="people-list-table my-orders-table"
      data-shell="soft-glass"
      data-testid="people-table"
      style={tableChromeStyle}
    >
      <ExitsTableToolbar
        search={
          <div className="people-search-with-sort">
            <ExitsOrgEhgInput
              variant="search"
              label={t("people.search")}
              value={search}
              onChange={onSearch}
              clearable
              className="people-org-ehg-search"
              testId="people-table-search"
            />
            <PeopleSearchSort
              sort={sort}
              onSort={onSort}
              open={sortMenuOpen}
              onOpenChange={onSortMenuOpenChange}
              testId="people-table-sort"
            />
          </div>
        }
        output={
          showOutput ? (
            <PeopleTableOutput rows={rows} statusLabel={statusLabel} />
          ) : undefined
        }
      />
      <ExitsTable>
        <ExitsTableHeader
          data-appearance={TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE}
          data-intent={TABLE_GLOBAL_DEFAULT_HEADER_INTENT}
          style={
            {
              "--qheader-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
              "--qheader-ink": "var(--exits-text, #000)",
            } as CSSProperties
          }
        >
          <ExitsTableRow>
            <ExitsTableHead colSize="flex">{t("people.displayName")}</ExitsTableHead>
            <ExitsTableHead>{t("people.column.contact")}</ExitsTableHead>
            <ExitsTableHead>{t("people.column.email")}</ExitsTableHead>
            <ExitsTableHead>{t("people.column.status")}</ExitsTableHead>
            <ExitsTableHead>{t("people.detail.iLentAction")}</ExitsTableHead>
            <ExitsTableHead>{t("people.detail.iOweAction")}</ExitsTableHead>
            <ExitsTableHead cellAlign="center" colSize="actions" className="people-table-actions">
              {t("people.column.actions")}
            </ExitsTableHead>
          </ExitsTableRow>
        </ExitsTableHeader>
        <ExitsTableBody>
          {pageRows.length === 0 && empty ? (
            <ExitsTableRow>
              <ExitsTableCell colSpan={7}>
                <EmptyState
                  align="center"
                  size="compact"
                  variant={empty.variant}
                  icon={<Users className="size-5" strokeWidth={1.75} />}
                  title={empty.title}
                  detail={empty.detail}
                  className="people-table-empty border-0 bg-transparent shadow-none"
                  testId="people-table-empty"
                />
              </ExitsTableCell>
            </ExitsTableRow>
          ) : (
            pageRows.map((row) => {
            const href = `/personal/people/${row.contact.id}`;
            const phone = row.contact.phone?.trim() || "—";
            const email = row.contact.email?.trim() || "—";
            return (
              <ExitsTableRow key={row.contact.id} data-testid={`people-table-row-${row.contact.id}`}>
                <ExitsTableCell emphasis="semibold" truncate title={row.contact.displayName}>
                  <Link to={href} className="people-table-name truncate no-underline">
                    {row.contact.displayName}
                  </Link>
                </ExitsTableCell>
                <ExitsTableCell truncate title={phone === "—" ? undefined : phone}>
                  {phone}
                </ExitsTableCell>
                <ExitsTableCell truncate title={email === "—" ? undefined : email}>
                  {email}
                </ExitsTableCell>
                <ExitsTableCell>
                  <StatusChip
                    tone={statusTone(row.connectionStatus)}
                    appearance="emphasis"
                    shape="square"
                  >
                    {statusLabel(row.connectionStatus)}
                  </StatusChip>
                </ExitsTableCell>
                <ExitsTableCell>
                  <PeopleUtangTotal
                    total={row.lentTotal}
                    count={row.lentCount}
                    href={`/personal/utang/lent?contact=${row.contact.id}`}
                    testId={`people-table-lent-${row.contact.id}`}
                  />
                </ExitsTableCell>
                <ExitsTableCell>
                  <PeopleUtangTotal
                    total={row.oweTotal}
                    count={row.oweCount}
                    href={`/personal/utang/owe?contact=${row.contact.id}`}
                    testId={`people-table-owe-${row.contact.id}`}
                    owe
                  />
                </ExitsTableCell>
                <ExitsTableCell cellAlign="center" colSize="actions" className="people-table-actions">
                  <span className="people-table-actions__cluster inline-flex flex-nowrap items-center justify-center gap-1.5">
                    {row.connectionStatus === "local" ? (
                      <ExitsTooltip
                        content={`${t("people.action.edit")} ${row.contact.displayName}`}
                      >
                        <Button
                          type="button"
                          intent="primary"
                          appearance="outline"
                          shape="round"
                          size="icon"
                          className="people-table-action people-table-action--edit people-table-edit"
                          data-testid={`people-edit-${row.contact.id}`}
                          aria-label={`${t("people.action.edit")} ${row.contact.displayName}`}
                          onClick={() => setEditingContact(row.contact)}
                        >
                          <Pencil className="size-4" aria-hidden />
                        </Button>
                      </ExitsTooltip>
                    ) : null}
                    <ExitsTooltip content={t("personal.utang.filterOwedToMe")}>
                      <Button
                        asChild
                        intent="success"
                        appearance="outline"
                        shape="round"
                        size="icon"
                        className="people-table-action people-table-action--lent"
                      >
                        <Link
                          to={`/personal/utang/lent?contact=${row.contact.id}`}
                          data-testid={`people-lent-${row.contact.id}`}
                          aria-label={t("personal.utang.filterOwedToMe")}
                        >
                          <Wallet className="size-4" aria-hidden />
                        </Link>
                      </Button>
                    </ExitsTooltip>
                    <ExitsTooltip content={t("personal.utang.filterIOwe")}>
                      <Button
                        asChild
                        intent="info"
                        appearance="outline"
                        shape="round"
                        size="icon"
                        className="people-table-action people-table-action--owe"
                      >
                        <Link
                          to={`/personal/utang/owe?contact=${row.contact.id}`}
                          data-testid={`people-owe-${row.contact.id}`}
                          aria-label={t("personal.utang.filterIOwe")}
                        >
                          <HandCoins className="size-4" aria-hidden />
                        </Link>
                      </Button>
                    </ExitsTooltip>
                  </span>
                </ExitsTableCell>
              </ExitsTableRow>
            );
            })
          )}
        </ExitsTableBody>
      </ExitsTable>
      <div
        className="people-table-footer my-orders-table-footer"
        data-testid="people-table-footer"
        data-appearance={TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE}
        data-intent={TABLE_GLOBAL_DEFAULT_FOOTER_INTENT}
        style={
          {
            "--qfooter-bg": "var(--exits-surface-elevated, #fff)",
            "--qfooter-ink": "var(--exits-text, #000)",
          } as CSSProperties
        }
      >
        <ExitsTablePagination
          page={safePage}
          pageSize={pageSize}
          total={rows.length}
          pageSizeOptions={[...PEOPLE_PAGE_SIZES]}
          onPageChange={setPage}
          onPageSizeChange={(next) => {
            const size = PEOPLE_PAGE_SIZES.find((item) => item === next) ?? 10;
            setPageSize(size);
            setPage(1);
          }}
          rowsPerPageLabel={t("exitsTable.rowsPerPage")}
          previousLabel={t("exitsTable.previous")}
          nextLabel={t("exitsTable.next")}
          rangeLabel={t("exitsTable.range")}
          rowCountLabel={t("exitsTable.rowCount")}
        />
      </div>
      {editingContact ? (
        <EditLocalContactDialog
          open
          contact={editingContact}
          onOpenChange={(open) => {
            if (!open) {
              setEditingContact(null);
            }
          }}
        />
      ) : null}
    </ExitsTableContainer>
  );
}

export function PeopleListSection({
  rows,
  toolbarStart,
  onAddLocal,
  onAddExits,
}: {
  rows: PeopleRowModel[];
  toolbarStart?: ReactNode;
  onAddLocal: () => void;
  onAddExits: () => void;
}) {
  const { t } = useI18n();
  const tableLayout = useMediaMin(1024);
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<PeopleListFilter>("all");
  const [sort, setSort] = useState<PeopleListSort>("name");
  const sortMenu = useDismissibleOpen(false);
  const [expandedContactId, setExpandedContactId] = useState<string | null>(null);

  const filteredRows = useMemo(
    () => filterPeopleRows(rows, filter, search, sort),
    [filter, rows, search, sort],
  );

  useEffect(() => {
    if (!expandedContactId) {
      return;
    }
    if (!filteredRows.some((row) => row.contact.id === expandedContactId)) {
      setExpandedContactId(null);
    }
  }, [expandedContactId, filteredRows]);

  function statusLabel(status: PeopleConnectionStatus): string {
    switch (status) {
      case "connected":
        return t("people.status.connected");
      case "request_sent":
        return t("people.status.requestSent");
      case "request_received":
        return t("people.status.requestReceived");
      case "blocked":
        return t("people.status.blocked");
      case "local":
        return t("people.status.local");
      default:
        return t("people.status.notConnected");
    }
  }

  const exportLabels: PeopleTableExportLabels = {
    name: t("people.displayName"),
    contact: t("people.column.contact"),
    email: t("people.column.email"),
    status: t("people.column.status"),
    lent: t("people.detail.iLentAction"),
    owe: t("people.detail.iOweAction"),
  };

  function exportModel() {
    return buildPeopleTableExportModel(filteredRows, statusLabel);
  }

  const filterCounts = useMemo(() => {
    const counts: Record<PeopleListFilter, number> = {
      all: rows.length,
      connected: 0,
      pending: 0,
      local: 0,
      not_connected: 0,
    };
    for (const row of rows) {
      if (row.connectionStatus === "connected") {
        counts.connected += 1;
      }
      if (
        row.connectionStatus === "request_sent" ||
        row.connectionStatus === "request_received"
      ) {
        counts.pending += 1;
      }
      if (row.connectionStatus === "local") {
        counts.local += 1;
      }
      if (row.connectionStatus === "not_connected") {
        counts.not_connected += 1;
      }
    }
    return counts;
  }, [rows]);

  return (
    <section
      className="people-list-section flex flex-col gap-2"
      data-testid="people-list-section"
      aria-label={t("people.listTitle")}
    >
      <Card
        className="people-list-card people-list-main-glass flex flex-col gap-2"
        data-shell="soft-glass"
        data-testid="people-list-card"
        padding="none"
        radius="soft"
      >
        <div
          className="personal-tracker-card__header people-list-card__header"
          data-testid="people-list-header-actions"
        >
          <h2 className="personal-tracker-card__title" data-testid="people-list-ribbon">
            <span className="personal-tracker-card__pennant" aria-hidden>
              <Users className="personal-tracker-card__pennant-icon size-[1.05rem] shrink-0" />
            </span>
            {t("people.listTitle")}
          </h2>
          <PeopleAddGlassSpeedDial onAddLocal={onAddLocal} onAddExits={onAddExits} />
        </div>
        <div className="people-search-row flex min-w-0 flex-nowrap items-end gap-2" data-testid="people-search-row">
          {toolbarStart}
          <div className="people-search-field people-search-with-sort min-w-0 flex-1">
            <ExitsOrgEhgInput
              variant="search"
              label={t("people.search")}
              value={search}
              onChange={setSearch}
              clearable
              className="people-org-ehg-search"
              testId="people-search"
            />
            <PeopleSearchSort
              sort={sort}
              onSort={setSort}
              open={sortMenu.open}
              onOpenChange={sortMenu.setOpen}
            />
          </div>
        </div>

        <ExitsChipBar
          variant="filter"
          ariaLabel={t("people.filter.label")}
          testId="people-status-filters"
          className="exits-animate-toolbar exits-chip-bar--scroll people-page__filters"
          items={FILTERS.map((item) => ({
            key: item.key,
            label: t(item.labelKey),
            count: filterCounts[item.key],
            state: filter === item.key ? "active" : "idle",
            testId: `people-filter-${item.key}`,
            onSelect: () => setFilter(item.key),
          }))}
        />

        {rows.length === 0 ? (
          <EmptyState
            align="center"
            shell="soft-glass"
            icon={<Users className="size-5" strokeWidth={1.75} />}
            title={t("people.emptyTitle")}
            detail={t("people.emptyBody")}
            className="people-list-empty"
          />
        ) : filteredRows.length === 0 ? (
          <EmptyState
            variant="filtered"
            align="center"
            shell="soft-glass"
            icon={<Users className="size-5" strokeWidth={1.75} />}
            title={t("people.noResultsTitle")}
            detail={t("people.noResultsBody")}
            className="people-list-empty"
          />
        ) : (
          <ul
            className="exits-list people-list-cards people-mobile-list m-0 flex list-none flex-col gap-0 p-0"
            data-testid="people-list"
          >
            {filteredRows.map((row) => (
              <PeopleMobileRow
                key={row.contact.id}
                row={row}
                statusLabel={statusLabel(row.connectionStatus)}
                expanded={expandedContactId === row.contact.id}
                onExpandedChange={(next) =>
                  setExpandedContactId(next ? row.contact.id : null)
                }
              />
            ))}
          </ul>
        )}
        {tableLayout ? (
          <PeopleTable
            rows={filteredRows}
            statusLabel={statusLabel}
            search={search}
            onSearch={setSearch}
            sort={sort}
            onSort={setSort}
            sortMenuOpen={sortMenu.open}
            onSortMenuOpenChange={sortMenu.setOpen}
            showOutput={false}
            empty={
              rows.length === 0
                ? { title: t("people.emptyTitle"), detail: t("people.emptyBody"), variant: "default" }
                : filteredRows.length === 0
                  ? {
                      title: t("people.noResultsTitle"),
                      detail: t("people.noResultsBody"),
                      variant: "filtered",
                    }
                  : null
            }
          />
        ) : null}
        <PeopleListSummaryBar
          total={rows.length}
          connected={filterCounts.connected}
          local={filterCounts.local}
          downloadDisabled={filteredRows.length === 0}
          onCsv={() => downloadPeopleTableCsv(exportModel(), exportLabels)}
          onXlsx={() => downloadPeopleTableXlsx(exportModel(), exportLabels)}
          onPdf={() => downloadPeopleTablePdf(exportModel(), exportLabels)}
        />
      </Card>
    </section>
  );
}
