import { Link } from "react-router-dom";
import { ChevronRight } from "lucide-react";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { UtangDueCaption } from "@/features/personal/utang/UtangListMeta";
import type { UtangAccountRow } from "@/features/personal/utang/utang-workspace";
import { UTANG_READ_ONLY_CHIP } from "@/features/personal/utang/utang-ownership-ui";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type UtangAccountCardProps = {
  row: UtangAccountRow;
};

/** Utang account row — Card settings layout shared with Money I owe / lent. */
export function UtangAccountCard({ row }: UtangAccountCardProps) {
  const { t } = useI18n();
  const ownershipLabel = row.isLedgerOwner
    ? t("personal.utang.ownershipMine")
    : t("personal.utang.ownershipSharedWithMe");
  const lent = row.perspective === "lent";

  return (
    <Link
      to={`/personal/utang/relationships/${row.relationshipId}`}
      className={cn(
        "ui-standards-qcard ui-standards-qcard--settings-card utang-account-card utang-account-card--standard utang-settings-card text-foreground no-underline",
        lent ? "utang-settings-card--lent" : "utang-settings-card--owe",
        row.isLedgerOwner ? "utang-account-card--mine" : "utang-account-card--shared",
      )}
      data-testid={`utang-account-${row.relationshipId}`}
    >
      <div className="ui-standards-qcard__settings-ribbon">
        <div
          className="ui-standards-qcard__settings-tab utang-settings-card__tab"
          data-utang-ownership=""
          data-testid={`utang-account-ownership-${row.relationshipId}`}
        >
          <span className="truncate">{ownershipLabel}</span>
          {row.dueKind !== "none" && row.dueDateUtc ? (
            <>
              <span className="utang-settings-card__tab-divider" aria-hidden />
              <UtangDueCaption
                dueDateUtc={row.dueDateUtc}
                dueKind={row.dueKind}
                testId={`utang-account-due-${row.relationshipId}`}
                className={cn(
                  "utang-settings-card__tab-text",
                  row.dueKind === "overdue" && "utang-settings-card__tab-text--danger",
                )}
              />
            </>
          ) : null}
          <span className="ui-standards-qcard__settings-fold" aria-hidden />
        </div>
        <div className="utang-settings-card__content">
          <div className="utang-settings-card__meta min-w-0 flex-1">
            <div className="utang-settings-card__headline">
              <p className="utang-settings-card__name m-0 truncate">{row.displayName}</p>
              <span className="utang-settings-card__amount">
                <MoneyDisplay amount={row.currentBalance} testId={`utang-account-balance-${row.relationshipId}`} />
              </span>
            </div>
            <span className="utang-settings-card__tags">
              <StatusChip tone={lent ? "success" : "warning"} appearance="emphasis" shape="square">
                {lent ? t("personal.utang.owesYou") : t("personal.utang.youOwe")}
              </StatusChip>
              <StatusChip
                tone={row.isSharedLedger ? "success" : "info"}
                appearance="emphasis"
                shape="square"
                data-testid={`utang-account-linked-${row.relationshipId}`}
              >
                {row.isSharedLedger ? t("people.status.connected") : t("people.localContact")}
              </StatusChip>
              {!row.isLedgerOwner ? (
                <StatusChip
                  tone={UTANG_READ_ONLY_CHIP.tone}
                  appearance={UTANG_READ_ONLY_CHIP.appearance}
                  shape={UTANG_READ_ONLY_CHIP.shape}
                  className="max-md:hidden"
                  data-testid={`utang-account-readonly-${row.relationshipId}`}
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
  );
}
