import { useMemo, useState, type CSSProperties } from "react";
import { Link } from "react-router-dom";
import { Eye, Search, Users } from "lucide-react";
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
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { formatDueLabel } from "@/api/platform/personal-utang-client";
import { UtangDueCaption } from "@/features/personal/utang/UtangListMeta";
import { UTANG_READ_ONLY_CHIP } from "@/features/personal/utang/utang-ownership-ui";
import {
  TABLE_FOOTER_GLOBAL_HIDE_CONTROLS_AT_OR_BELOW,
} from "@/features/ui-standards/table-footer-global-default";
import {
  TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_HEADER_INTENT,
} from "@/features/ui-standards/table-global-default";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

const tableChromeStyle = {
  "--qheader-bg": "color-mix(in srgb, var(--exits-primary) 8%, transparent)",
  "--qheader-ink": "var(--exits-text, #000)",
} as CSSProperties;

export type UtangRelationshipListTableRow = {
  id: string;
  displayName: string;
  balance: number;
  dueDateUtc: string | null;
  isShared: boolean;
  isOwner: boolean;
};

type MoneySortKey = "person" | "balance" | "ledger" | "record" | "due";

type UtangRelationshipListTableProps = {
  mode: "lent" | "owe";
  rows: UtangRelationshipListTableRow[];
  search: string;
  onSearch: (value: string) => void;
};

function compareText(a: string, b: string): number {
  return a.localeCompare(b, undefined, { sensitivity: "base" });
}

/** Soft Glass locked table for Money I lent / I owe (lg+). */
export function UtangRelationshipListTable({
  mode,
  rows,
  search,
  onSearch,
}: UtangRelationshipListTableProps) {
  const { t } = useI18n();
  const lent = mode === "lent";
  const [sortKey, setSortKey] = useState<MoneySortKey | null>(null);
  const [sortDirection, setSortDirection] = useState<ExitsTableSortDirection>(null);

  const sortedRows = useMemo(() => {
    if (!sortKey || !sortDirection) {
      return rows;
    }
    const next = [...rows];
    next.sort((a, b) => {
      let cmp = 0;
      switch (sortKey) {
        case "person":
          cmp = compareText(a.displayName, b.displayName);
          break;
        case "balance":
          cmp = a.balance - b.balance;
          break;
        case "ledger":
          cmp = Number(a.isShared) - Number(b.isShared);
          break;
        case "record":
          cmp = Number(a.isOwner) - Number(b.isOwner);
          break;
        case "due":
          cmp = compareText(a.dueDateUtc ?? "", b.dueDateUtc ?? "");
          break;
      }
      return sortDirection === "asc" ? cmp : -cmp;
    });
    return next;
  }, [rows, sortDirection, sortKey]);

  function toggleSort(key: MoneySortKey) {
    const next = cycleExitsTableSort(sortKey, sortDirection, key);
    setSortKey(next.key as MoneySortKey | null);
    setSortDirection(next.direction);
  }

  return (
    <ExitsTableContainer
      className={cn(
        "my-orders-table utang-money-table",
        lent ? "utang-money-table--lent" : "utang-money-table--owe",
      )}
      data-shell="soft-glass"
      data-testid={lent ? "utang-lent-table" : "utang-owe-table"}
      style={tableChromeStyle}
    >
      <ExitsTableToolbar
        search={
          <ExitsOutlineInput
            label={t("personal.utang.searchLabel")}
            placeholder={t("personal.utang.searchPlaceholder")}
            placeholderOnly
            icon={Search}
            type="search"
            value={search}
            onChange={onSearch}
            clearable
            testId={lent ? "utang-lent-table-search" : "utang-owe-table-search"}
          />
        }
      />
      {rows.length === 0 ? (
        <EmptyState
          shell="soft-glass"
          variant="filtered"
          align="center"
          icon={<Users className="size-5" strokeWidth={1.75} />}
          title={t("personal.utang.accountsFilterEmptyTitle")}
          detail={t("personal.utang.accountsFilterEmptyDetail")}
          testId={lent ? "utang-lent-table-empty" : "utang-owe-table-empty"}
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
                colSize="flex"
                sortDirection={sortKey === "person" ? sortDirection : null}
                onSort={() => toggleSort("person")}
              >
                {t("personal.utang.table.person")}
              </ExitsTableHead>
              <ExitsTableHead
                cellAlign="numeric"
                sortDirection={sortKey === "balance" ? sortDirection : null}
                onSort={() => toggleSort("balance")}
              >
                {t("personal.utang.table.balance")}
              </ExitsTableHead>
              <ExitsTableHead
                sortDirection={sortKey === "ledger" ? sortDirection : null}
                onSort={() => toggleSort("ledger")}
              >
                {t("personal.utang.table.ledger")}
              </ExitsTableHead>
              <ExitsTableHead
                sortDirection={sortKey === "record" ? sortDirection : null}
                onSort={() => toggleSort("record")}
              >
                {t("personal.utang.table.record")}
              </ExitsTableHead>
              <ExitsTableHead
                sortDirection={sortKey === "due" ? sortDirection : null}
                onSort={() => toggleSort("due")}
              >
                {t("personal.utang.table.due")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="center" colSize="actions" sortable={false}>
                {t("personal.utang.table.actions")}
              </ExitsTableHead>
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            {sortedRows.map((row) => {
              const href = `/personal/utang/relationships/${row.id}`;
              const viewLabel = `${t("personal.utang.action.view")} ${row.displayName}`;
              const due = formatDueLabel(row.dueDateUtc);
              return (
                <ExitsTableRow
                  key={row.id}
                  data-testid={`utang-rel-table-row-${row.id}`}
                >
                  <ExitsTableCell emphasis="semibold" truncate title={row.displayName}>
                    <Link className="text-foreground no-underline hover:underline" to={href}>
                      {row.displayName}
                    </Link>
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="numeric">
                    <Link
                      to={href}
                      className={cn(
                        "people-table-amount inline-flex no-underline",
                        !lent && row.balance > 0 && "text-[var(--exits-warning)]",
                      )}
                      data-testid={`utang-rel-table-balance-${row.id}`}
                    >
                      <MoneyDisplay amount={row.balance} />
                    </Link>
                  </ExitsTableCell>
                  <ExitsTableCell>
                    <StatusChip
                      tone={row.isShared ? "success" : "info"}
                      appearance="emphasis"
                      shape="square"
                      data-testid={`utang-rel-table-ledger-${row.id}`}
                    >
                      {row.isShared ? t("people.status.connected") : t("people.localContact")}
                    </StatusChip>
                  </ExitsTableCell>
                  <ExitsTableCell>
                    <span className="inline-flex flex-wrap items-center gap-1.5">
                      <StatusChip
                        tone={row.isOwner ? "success" : "info"}
                        appearance="emphasis"
                        shape="square"
                        data-testid={`utang-rel-table-ownership-${row.id}`}
                      >
                        {row.isOwner
                          ? t("personal.utang.ownershipMine")
                          : t("personal.utang.ownershipSharedWithMe")}
                      </StatusChip>
                      {!row.isOwner ? (
                        <StatusChip
                          tone={UTANG_READ_ONLY_CHIP.tone}
                          appearance={UTANG_READ_ONLY_CHIP.appearance}
                          shape={UTANG_READ_ONLY_CHIP.shape}
                          data-testid={`utang-rel-table-readonly-${row.id}`}
                        >
                          {t("personal.utang.readOnly")}
                        </StatusChip>
                      ) : null}
                    </span>
                  </ExitsTableCell>
                  <ExitsTableCell truncate>
                    {due.kind !== "none" && row.dueDateUtc ? (
                      <UtangDueCaption
                        dueDateUtc={row.dueDateUtc}
                        dueKind={due.kind}
                        testId={`utang-rel-table-due-${row.id}`}
                        className={cn(
                          due.kind === "overdue" && "utang-settings-card__tab-text--danger",
                        )}
                      />
                    ) : (
                      <span className="text-muted">—</span>
                    )}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="center" colSize="actions">
                    <ExitsTooltip content={viewLabel}>
                      <Button
                        asChild
                        intent="primary"
                        appearance="outline"
                        shape="round"
                        size="icon"
                        className="people-table-action people-table-action--view"
                      >
                        <Link
                          to={href}
                          data-testid={`utang-rel-view-${row.id}`}
                          aria-label={viewLabel}
                        >
                          <Eye className="size-4" aria-hidden />
                        </Link>
                      </Button>
                    </ExitsTooltip>
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
