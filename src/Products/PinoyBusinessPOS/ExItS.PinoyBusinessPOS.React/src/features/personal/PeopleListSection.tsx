import { ChevronRight, Pencil, Search, UserPlus, Users } from "lucide-react";
import { useEffect, useMemo, useState, type CSSProperties, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { CountBadge } from "@/components/exits/CountChip";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import {
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
import { ExitsOutlineInput } from "@/components/exits/ExitsOutlineField";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import {
  type PeopleConnectionStatus,
  type PeopleRowModel,
} from "@/features/personal/people-status";
import { UtangDueCaption } from "@/features/personal/utang/UtangListMeta";
import { UTANG_READ_ONLY_CHIP } from "@/features/personal/utang/utang-ownership-ui";
import { EditLocalContactDialog } from "@/features/personal/EditLocalContactDialog";
import { formatDueLabel } from "@/api/platform/personal-utang-client";
import { useMediaMin } from "@/hooks/useMediaQuery";
import {
  buildPeopleTableExportModel,
  downloadPeopleTableCsv,
  downloadPeopleTablePdf,
  downloadPeopleTableXlsx,
  printPeopleTable,
  type PeopleTableExportLabels,
} from "@/features/personal/people-table-output";
import { INTENT_GLOBAL_STANDARD } from "@/features/ui-standards/intent-color-global-default";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

/** Locked `ext btn glossy`: standard fill, glossy sheen, control height. */
const EXT_BTN_GLOSSY_CLASS =
  "shrink-0 rounded-[6px] border shadow-[0_1px_5px_rgba(0,0,0,0.2),0_2px_2px_rgba(0,0,0,0.14),0_3px_1px_-2px_rgba(0,0,0,0.12)]";
const EXT_BTN_GLOSSY_STYLE: CSSProperties = {
  backgroundColor: INTENT_GLOBAL_STANDARD,
  color: "#000000",
  borderColor: "rgba(0, 0, 0, 0.2)",
};

export type PeopleListFilter = "all" | "connected" | "pending" | "local" | "not_connected";

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

export function filterPeopleRows(
  rows: PeopleRowModel[],
  filter: PeopleListFilter,
  search: string,
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
    .sort((a, b) => a.contact.displayName.localeCompare(b.contact.displayName));
}

function PeopleListRow({
  row,
  statusLabel,
}: {
  row: PeopleRowModel;
  statusLabel: string;
}) {
  const { t } = useI18n();

  const utang = row.utang;
  const owned = utang?.isLedgerOwner !== false;

  return (
    <li>
      <Link
        to={`/personal/people/${row.contact.id}`}
        className={cn(
          "ui-standards-qcard ui-standards-qcard--settings-card utang-account-card utang-account-card--standard utang-settings-card text-foreground no-underline",
          utang && (utang.direction === "lent" ? "utang-settings-card--lent" : "utang-settings-card--owe"),
          utang && (owned ? "utang-account-card--mine" : "utang-account-card--shared"),
        )}
        data-testid={`people-row-${row.contact.id}`}
      >
        <div className="ui-standards-qcard__settings-ribbon">
          {utang ? (
            <div className="ui-standards-qcard__settings-tab utang-settings-card__tab" data-utang-ownership="">
              <span className="truncate">
                {owned
                  ? t("personal.utang.ownershipMine")
                  : t("personal.utang.ownershipSharedWithMe")}
              </span>
              {formatDueLabel(utang.dueDateUtc).kind !== "none" ? (
                <>
                  <span className="utang-settings-card__tab-divider" aria-hidden />
                  <UtangDueCaption
                    dueDateUtc={utang.dueDateUtc}
                    className={cn(
                      "utang-settings-card__tab-text",
                      formatDueLabel(utang.dueDateUtc).kind === "overdue" &&
                        "utang-settings-card__tab-text--danger",
                    )}
                    testId={`people-row-due-${row.contact.id}`}
                  />
                </>
              ) : null}
              <span className="ui-standards-qcard__settings-fold" aria-hidden />
            </div>
          ) : null}
          <div className="utang-settings-card__content">
            <div className="utang-settings-card__meta min-w-0 flex-1">
              <div className="utang-settings-card__headline">
                <p className="utang-settings-card__name m-0 truncate">{row.contact.displayName}</p>
                {utang ? (
                  <span className="utang-settings-card__amount">
                    <MoneyDisplay
                      amount={utang.balance}
                      testId={`people-row-balance-${row.contact.id}`}
                    />
                  </span>
                ) : null}
              </div>
              <span className="utang-settings-card__tags">
                {utang ? (
                  <>
                    <StatusChip
                      tone={utang.direction === "lent" ? "success" : "warning"}
                      appearance="emphasis"
                      shape="square"
                    >
                      {utang.direction === "lent"
                        ? t("personal.utang.owesYou")
                        : t("personal.utang.youOwe")}
                    </StatusChip>
                    <StatusChip
                      tone={utang.isSharedLedger ? "success" : "info"}
                      appearance="emphasis"
                      shape="square"
                      data-testid={`people-row-linked-${row.contact.id}`}
                    >
                      {utang.isSharedLedger ? t("people.status.connected") : t("people.localContact")}
                    </StatusChip>
                  </>
                ) : (
                  <StatusChip
                    tone={statusTone(row.connectionStatus)}
                    appearance="emphasis"
                    shape="square"
                  >
                    {statusLabel}
                  </StatusChip>
                )}
                {utang && !owned ? (
                  <StatusChip
                    tone={UTANG_READ_ONLY_CHIP.tone}
                    appearance={UTANG_READ_ONLY_CHIP.appearance}
                    shape={UTANG_READ_ONLY_CHIP.shape}
                    className="max-md:hidden"
                    data-testid={`people-row-readonly-${row.contact.id}`}
                  >
                    {t("personal.utang.readOnly")}
                  </StatusChip>
                ) : null}
              </span>
            </div>
          </div>
          <span className="utang-account-card__caret" aria-hidden>
            <ChevronRight className="size-5" />
          </span>
        </div>
      </Link>
    </li>
  );
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

function PeopleTable({
  rows,
  statusLabel,
  search,
  onSearch,
  empty,
}: {
  rows: PeopleRowModel[];
  statusLabel: (status: PeopleConnectionStatus) => string;
  search: string;
  onSearch: (value: string) => void;
  empty: { title: string; detail: string; variant: "default" | "filtered" } | null;
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
    <ExitsTableContainer
      className="people-list-table"
      data-testid="people-table"
      style={
        {
          "--qheader-accent": "var(--people-table-chrome-bg)",
          "--exits-table-page-size-border": "var(--people-table-chrome-border)",
        } as CSSProperties
      }
    >
      <ExitsTableToolbar
        search={
          <ExitsOutlineInput
            label={t("people.search")}
            placeholder={t("people.search")}
            placeholderOnly
            icon={Search}
            type="search"
            value={search}
            onChange={onSearch}
            clearable
            testId="people-table-search"
          />
        }
        output={
          <ExitsTableOutputActions
            csvLabel={t("exitsTable.exportCsv")}
            xlsxLabel={t("exitsTable.exportExcel")}
            pdfLabel={t("exitsTable.exportPdf")}
            printLabel={t("exitsTable.print")}
            menuLabel={t("exitsTable.exportPrintMenu")}
            onCsv={() => downloadPeopleTableCsv(exportModel(), exportLabels)}
            onXlsx={() => downloadPeopleTableXlsx(exportModel(), exportLabels)}
            onPdf={() => downloadPeopleTablePdf(exportModel(), exportLabels)}
            onPrint={printPeopleTable}
          />
        }
      />
      <ExitsTable>
        <ExitsTableHeader
          data-appearance="glossy"
          data-intent="standard"
          style={
            {
              "--qheader-bg": "var(--people-table-chrome-bg)",
              "--qheader-ink": "var(--people-table-chrome-ink)",
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
            <ExitsTableHead align="center" className="people-table-actions">
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
                <ExitsTableCell className="people-table-actions">
                  <span className="inline-flex flex-wrap items-center justify-end gap-2">
                    {row.connectionStatus === "local" ? (
                      <Button
                        type="button"
                        intent="primary"
                        appearance="glossy"
                        shape="round"
                        size="icon"
                        className="people-table-edit"
                        data-testid={`people-edit-${row.contact.id}`}
                        aria-label={`${t("people.action.edit")} ${row.contact.displayName}`}
                        onClick={() => setEditingContact(row.contact)}
                      >
                        <Pencil className="size-4" aria-hidden />
                      </Button>
                    ) : null}
                    <Button asChild intent="success" appearance="glossy" className="w-auto">
                      <Link
                        to={`/personal/utang/lent?contact=${row.contact.id}`}
                        data-testid={`people-lent-${row.contact.id}`}
                      >
                        {t("personal.utang.filterOwedToMe")}
                      </Link>
                    </Button>
                    <Button asChild intent="info" appearance="glossy" className="w-auto">
                      <Link
                        to={`/personal/utang/owe?contact=${row.contact.id}`}
                        data-testid={`people-owe-${row.contact.id}`}
                      >
                        {t("personal.utang.filterIOwe")}
                      </Link>
                    </Button>
                  </span>
                </ExitsTableCell>
              </ExitsTableRow>
            );
            })
          )}
        </ExitsTableBody>
      </ExitsTable>
      <div
        className="people-table-footer"
        data-testid="people-table-footer"
        data-appearance="glossy"
        data-intent="standard"
        style={
          {
            "--qfooter-bg": "var(--people-table-chrome-bg)",
            "--qfooter-ink": "var(--people-table-chrome-ink)",
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
}: {
  rows: PeopleRowModel[];
  toolbarStart?: ReactNode;
}) {
  const { t } = useI18n();
  const compactSearch = !useMediaMin(640);
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<PeopleListFilter>("all");

  const filteredRows = useMemo(
    () => filterPeopleRows(rows, filter, search),
    [filter, rows, search],
  );

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
      <div className="people-search-row flex min-w-0 flex-wrap items-end gap-2" data-testid="people-search-row">
        {toolbarStart}
        <Button
          asChild
          intent="neutral"
          appearance="glossy"
          className={EXT_BTN_GLOSSY_CLASS}
          style={EXT_BTN_GLOSSY_STYLE}
        >
          <Link to="/personal/invitations" data-testid="people-connection-requests">
            <UserPlus className="size-4 shrink-0" aria-hidden="true" />
            {t("people.connectionInbox")}
          </Link>
        </Button>
        <div className="ui-standards-playground-search min-w-[12rem] flex-1">
          <ExitsOutlineInput
            label={t("people.search")}
            placeholder={t("people.searchPlaceholder")}
            placeholderOnly={compactSearch}
            value={search}
            onChange={setSearch}
            clearable
            icon={Search}
            type="search"
            testId="people-search"
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
          icon={<Users className="size-5" strokeWidth={1.75} />}
          title={t("people.emptyTitle")}
          detail={t("people.emptyBody")}
          className="people-list-empty"
        />
      ) : filteredRows.length === 0 ? (
        <EmptyState
          variant="filtered"
          align="center"
          icon={<Users className="size-5" strokeWidth={1.75} />}
          title={t("people.noResultsTitle")}
          detail={t("people.noResultsBody")}
          className="people-list-empty"
        />
      ) : (
        <ul className="exits-list people-list-cards utang-relationship-list m-0 grid list-none gap-4 p-0" data-testid="people-list">
          {filteredRows.map((row) => (
            <PeopleListRow
              key={row.contact.id}
              row={row}
              statusLabel={statusLabel(row.connectionStatus)}
            />
          ))}
        </ul>
      )}
      <PeopleTable
        rows={filteredRows}
        statusLabel={statusLabel}
        search={search}
        onSearch={setSearch}
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
    </section>
  );
}
