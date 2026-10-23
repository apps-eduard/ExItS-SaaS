import { type CSSProperties } from "react";
import { Link } from "react-router-dom";
import { Eye, Search, Users } from "lucide-react";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsOutlineInput } from "@/components/exits/ExitsOutlineField";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
  ExitsTableToolbar,
} from "@/components/exits/ExitsTable";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { UtangDueCaption } from "@/features/personal/utang/UtangListMeta";
import { UTANG_READ_ONLY_CHIP } from "@/features/personal/utang/utang-ownership-ui";
import type { UtangAccountRow } from "@/features/personal/utang/utang-workspace";
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

type UtangActiveAccountsTableProps = {
  rows: UtangAccountRow[];
  search: string;
  onSearch: (value: string) => void;
  showSearch: boolean;
};

/** Soft Glass locked table for Utang Active accounts (lg+). */
export function UtangActiveAccountsTable({
  rows,
  search,
  onSearch,
  showSearch,
}: UtangActiveAccountsTableProps) {
  const { t } = useI18n();

  return (
    <ExitsTableContainer
      className="my-orders-table utang-hub-accounts-table"
      data-shell="soft-glass"
      data-testid="utang-hub-accounts-table"
      style={tableChromeStyle}
    >
      {showSearch ? (
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
              testId="utang-hub-table-search"
            />
          }
        />
      ) : null}
      {rows.length === 0 ? (
        <EmptyState
          shell="soft-glass"
          variant="filtered"
          align="center"
          icon={<Users className="size-5" strokeWidth={1.75} />}
          title={t("personal.utang.accountsFilterEmptyTitle")}
          detail={t("personal.utang.accountsFilterEmptyDetail")}
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
              <ExitsTableHead colSize="flex">{t("personal.utang.table.person")}</ExitsTableHead>
              <ExitsTableHead cellAlign="numeric">{t("personal.utang.table.balance")}</ExitsTableHead>
              <ExitsTableHead>{t("personal.utang.table.direction")}</ExitsTableHead>
              <ExitsTableHead>{t("personal.utang.table.ledger")}</ExitsTableHead>
              <ExitsTableHead>{t("personal.utang.table.record")}</ExitsTableHead>
              <ExitsTableHead>{t("personal.utang.table.due")}</ExitsTableHead>
              <ExitsTableHead cellAlign="center" colSize="actions">
                {t("personal.utang.table.actions")}
              </ExitsTableHead>
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            {rows.map((row) => {
              const lent = row.perspective === "lent";
              const href = `/personal/utang/relationships/${row.relationshipId}`;
              const viewLabel = `${t("personal.utang.action.view")} ${row.displayName}`;
              return (
                <ExitsTableRow
                  key={row.relationshipId}
                  data-testid={`utang-account-table-row-${row.relationshipId}`}
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
                        !lent && row.currentBalance > 0 && "text-[var(--exits-warning)]",
                      )}
                      data-testid={`utang-account-table-balance-${row.relationshipId}`}
                    >
                      <MoneyDisplay amount={row.currentBalance} />
                    </Link>
                  </ExitsTableCell>
                  <ExitsTableCell>
                    <StatusChip tone={lent ? "success" : "info"} appearance="emphasis" shape="square">
                      {lent ? t("personal.utang.owesYou") : t("personal.utang.youOwe")}
                    </StatusChip>
                  </ExitsTableCell>
                  <ExitsTableCell>
                    <StatusChip
                      tone={row.isSharedLedger ? "success" : "info"}
                      appearance="emphasis"
                      shape="square"
                      data-testid={`utang-account-table-linked-${row.relationshipId}`}
                    >
                      {row.isSharedLedger ? t("people.status.connected") : t("people.localContact")}
                    </StatusChip>
                  </ExitsTableCell>
                  <ExitsTableCell>
                    <span className="inline-flex flex-wrap items-center gap-1.5">
                      <StatusChip
                        tone={row.isLedgerOwner ? "success" : "info"}
                        appearance="emphasis"
                        shape="square"
                        data-testid={`utang-account-table-ownership-${row.relationshipId}`}
                      >
                        {row.isLedgerOwner
                          ? t("personal.utang.ownershipMine")
                          : t("personal.utang.ownershipSharedWithMe")}
                      </StatusChip>
                      {!row.isLedgerOwner ? (
                        <StatusChip
                          tone={UTANG_READ_ONLY_CHIP.tone}
                          appearance={UTANG_READ_ONLY_CHIP.appearance}
                          shape={UTANG_READ_ONLY_CHIP.shape}
                          data-testid={`utang-account-table-readonly-${row.relationshipId}`}
                        >
                          {t("personal.utang.readOnly")}
                        </StatusChip>
                      ) : null}
                    </span>
                  </ExitsTableCell>
                  <ExitsTableCell truncate>
                    {row.dueKind !== "none" && row.dueDateUtc ? (
                      <UtangDueCaption
                        dueDateUtc={row.dueDateUtc}
                        dueKind={row.dueKind}
                        testId={`utang-account-table-due-${row.relationshipId}`}
                        className={cn(
                          row.dueKind === "overdue" && "utang-settings-card__tab-text--danger",
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
                          data-testid={`utang-account-view-${row.relationshipId}`}
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
    </ExitsTableContainer>
  );
}
