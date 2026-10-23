import { useMemo, useState, type CSSProperties } from "react";
import { Search, Users } from "lucide-react";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsOutlineInput } from "@/components/exits/ExitsOutlineField";
import {
  cycleExitsTableSort,
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTablePagination,
  ExitsTableRow,
  ExitsTableToolbar,
  type ExitsTableSortDirection,
} from "@/components/exits/ExitsTable";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip, type StatusChipTone } from "@/components/exits/StatusChip";
import { UtangDueCaption } from "@/features/personal/utang/UtangListMeta";
import { formatDueLabel } from "@/api/platform/personal-utang-client";
import {
  TABLE_FOOTER_GLOBAL_HIDE_CONTROLS_AT_OR_BELOW,
} from "@/features/ui-standards/table-footer-global-default";
import {
  TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_HEADER_INTENT,
} from "@/features/ui-standards/table-global-default";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

function activityStatusTone(status: string): StatusChipTone {
  switch (status) {
    case "Confirmed":
      return "success";
    case "Pending":
      return "warning";
    case "Disputed":
      return "danger";
    case "Cancelled":
      return "neutral";
    default:
      return "info";
  }
}

const tableChromeStyle = {
  "--qheader-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
  "--qheader-ink": "var(--exits-text, #000)",
} as CSSProperties;

export type UtangActivityTableRow = {
  id: string;
  title: string;
  amount: number;
  balanceAfter: number;
  affectsBalance: boolean;
  isOpening: boolean;
  status: string;
  statusText: string;
  isShared: boolean;
  notes: string | null;
  createdAtUtc: string;
  dueDateUtc: string | null;
  perspectiveLent: boolean;
};

type ActivitySortKey = "action" | "amount" | "status" | "note" | "due" | "created";

type UtangActivityTableProps = {
  rows: UtangActivityTableRow[];
  search: string;
  onSearch: (value: string) => void;
};

function compareText(a: string, b: string): number {
  return a.localeCompare(b, undefined, { sensitivity: "base" });
}

/** Soft Glass locked table for relationship Activity (lg+). */
export function UtangActivityTable({ rows, search, onSearch }: UtangActivityTableProps) {
  const { t } = useI18n();
  const [sortKey, setSortKey] = useState<ActivitySortKey | null>(null);
  const [sortDirection, setSortDirection] = useState<ExitsTableSortDirection>(null);

  const sortedRows = useMemo(() => {
    if (!sortKey || !sortDirection) {
      return rows;
    }
    const next = [...rows];
    next.sort((a, b) => {
      let cmp = 0;
      switch (sortKey) {
        case "action":
          cmp = compareText(a.title, b.title);
          break;
        case "amount":
          cmp = a.amount - b.amount;
          break;
        case "status":
          cmp = compareText(a.statusText, b.statusText);
          break;
        case "note":
          cmp = compareText(a.notes ?? "", b.notes ?? "");
          break;
        case "due":
          cmp = compareText(a.dueDateUtc ?? "", b.dueDateUtc ?? "");
          break;
        case "created":
          cmp = compareText(a.createdAtUtc, b.createdAtUtc);
          break;
      }
      return sortDirection === "asc" ? cmp : -cmp;
    });
    return next;
  }, [rows, sortDirection, sortKey]);

  function toggleSort(key: ActivitySortKey) {
    const next = cycleExitsTableSort(sortKey, sortDirection, key);
    setSortKey(next.key as ActivitySortKey | null);
    setSortDirection(next.direction);
  }

  return (
    <ExitsTableContainer
      className="my-orders-table utang-activity-table"
      data-shell="soft-glass"
      data-testid="utang-activity-table"
      style={tableChromeStyle}
    >
      <ExitsTableToolbar
        search={
          <ExitsOutlineInput
            label={t("personal.utang.activitySearchLabel")}
            placeholder={t("personal.utang.activitySearchPlaceholder")}
            placeholderOnly
            icon={Search}
            type="search"
            value={search}
            onChange={onSearch}
            clearable
            testId="utang-activity-table-search"
          />
        }
      />
      {rows.length === 0 ? (
        <EmptyState
          shell="soft-glass"
          variant="filtered"
          align="center"
          icon={<Users className="size-5" strokeWidth={1.75} />}
          title={t("personal.utang.activityFilterEmptyTitle")}
          detail={t("personal.utang.activityFilterEmptyDetail")}
          testId="utang-activity-table-empty"
        />
      ) : (
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
              <ExitsTableHead
                className="utang-activity-col-entry"
                colSize="flex"
                sortDirection={sortKey === "action" ? sortDirection : null}
                onSort={() => toggleSort("action")}
                data-testid="utang-activity-head-action"
              >
                {t("personal.utang.table.action")}
              </ExitsTableHead>
              <ExitsTableHead
                cellAlign="numeric"
                colSize="numeric"
                sortDirection={sortKey === "amount" ? sortDirection : null}
                onSort={() => toggleSort("amount")}
                data-testid="utang-activity-head-amount"
              >
                {t("personal.utang.table.amount")}
              </ExitsTableHead>
              <ExitsTableHead
                className="utang-activity-col-status"
                sortDirection={sortKey === "status" ? sortDirection : null}
                onSort={() => toggleSort("status")}
                data-testid="utang-activity-head-status"
              >
                {t("personal.utang.table.status")}
              </ExitsTableHead>
              <ExitsTableHead
                sortDirection={sortKey === "note" ? sortDirection : null}
                onSort={() => toggleSort("note")}
                data-testid="utang-activity-head-note"
              >
                {t("personal.utang.table.note")}
              </ExitsTableHead>
              <ExitsTableHead
                className="utang-activity-col-due"
                sortDirection={sortKey === "due" ? sortDirection : null}
                onSort={() => toggleSort("due")}
                data-testid="utang-activity-head-due"
              >
                {t("personal.utang.table.due")}
              </ExitsTableHead>
              <ExitsTableHead
                className="utang-activity-col-created"
                sortDirection={sortKey === "created" ? sortDirection : null}
                onSort={() => toggleSort("created")}
                data-testid="utang-activity-head-created"
              >
                {t("personal.utang.table.created")}
              </ExitsTableHead>
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            {sortedRows.map((row) => {
              const due = formatDueLabel(row.dueDateUtc);
              return (
                <ExitsTableRow
                  key={row.id}
                  data-testid={`utang-activity-table-row-${row.id}`}
                >
                  <ExitsTableCell
                    className="utang-activity-col-entry"
                    emphasis="semibold"
                    truncate
                    title={row.title}
                    data-testid={`utang-activity-table-action-${row.id}`}
                  >
                    {row.title}
                  </ExitsTableCell>
                  <ExitsTableCell
                    className="utang-activity-col-amount"
                    cellAlign="numeric"
                    colSize="numeric"
                  >
                    <span className="utang-activity-amount inline-flex flex-col items-end gap-0.5">
                      <MoneyDisplay amount={row.amount} />
                      {!row.isOpening && row.affectsBalance ? (
                        <span className="utang-activity-balance-after text-[length:var(--exits-text-xs)]">
                          {t("personal.utang.balanceAfter")}:{" "}
                          <MoneyDisplay amount={row.balanceAfter} />
                        </span>
                      ) : null}
                      {!row.isOpening && !row.affectsBalance ? (
                        <span
                          className="text-[length:var(--exits-text-xs)] text-muted"
                          data-testid={`utang-activity-table-no-balance-${row.id}`}
                        >
                          {t("personal.utang.noBalanceChange")}
                        </span>
                      ) : null}
                    </span>
                  </ExitsTableCell>
                  <ExitsTableCell className="utang-activity-col-status">
                    <span
                      className="utang-activity-status-tags inline-flex flex-nowrap items-center gap-1.5"
                      data-testid={`utang-activity-table-status-${row.id}`}
                    >
                      <StatusChip
                        tone={row.perspectiveLent ? "success" : "info"}
                        appearance="emphasis"
                        shape="square"
                      >
                        {row.perspectiveLent
                          ? t("personal.utang.owesYou")
                          : t("personal.utang.youOwe")}
                      </StatusChip>
                      <StatusChip
                        tone={row.isShared ? "success" : "info"}
                        appearance="emphasis"
                        shape="square"
                      >
                        {row.isShared
                          ? t("people.status.connected")
                          : t("people.localContact")}
                      </StatusChip>
                      <StatusChip
                        tone={activityStatusTone(row.status)}
                        appearance="emphasis"
                        shape="square"
                      >
                        {row.statusText}
                      </StatusChip>
                    </span>
                  </ExitsTableCell>
                  <ExitsTableCell truncate title={row.notes ?? undefined}>
                    {row.notes ? (
                      <span data-testid={`utang-activity-table-purpose-${row.id}`}>
                        {row.notes}
                      </span>
                    ) : (
                      <span className="text-muted">—</span>
                    )}
                  </ExitsTableCell>
                  <ExitsTableCell className="utang-activity-col-due">
                    {due.kind !== "none" && row.dueDateUtc ? (
                      <UtangDueCaption
                        dueDateUtc={row.dueDateUtc}
                        dueKind={due.kind}
                        className={cn(
                          due.kind === "overdue" && "utang-settings-card__tab-text--danger",
                        )}
                      />
                    ) : (
                      <span className="text-muted">—</span>
                    )}
                  </ExitsTableCell>
                  <ExitsTableCell className="utang-activity-col-created">
                    <span className="utang-activity-created-date whitespace-nowrap">
                      {new Date(row.createdAtUtc).toLocaleString()}
                    </span>
                  </ExitsTableCell>
                </ExitsTableRow>
              );
            })}
          </ExitsTableBody>
        </ExitsTable>
      )}
      {rows.length > 0 &&
      rows.length <= TABLE_FOOTER_GLOBAL_HIDE_CONTROLS_AT_OR_BELOW ? (
        <ExitsTablePagination
          page={1}
          pageSize={rows.length}
          total={rows.length}
          onPageChange={() => undefined}
          onPageSizeChange={() => undefined}
          rowsPerPageLabel={t("exitsTable.rowsPerPage")}
          previousLabel={t("exitsTable.previous")}
          nextLabel={t("exitsTable.next")}
          rangeLabel={t("exitsTable.range")}
          rowCountLabel={t("exitsTable.rowCount")}
        />
      ) : null}
    </ExitsTableContainer>
  );
}
