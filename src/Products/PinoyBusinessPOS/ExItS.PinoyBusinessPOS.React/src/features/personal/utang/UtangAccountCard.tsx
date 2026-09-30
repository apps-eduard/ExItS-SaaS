import { Link } from "react-router-dom";
import { ChevronRight } from "lucide-react";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { StatusChip } from "@/components/exits/StatusChip";
import { UtangDueCaption, UtangLinkedIcon } from "@/features/personal/utang/UtangListMeta";
import type { UtangAccountRow } from "@/features/personal/utang/utang-workspace";
import { UTANG_READ_ONLY_CHIP } from "@/features/personal/utang/utang-ownership-ui";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type UtangAccountCardProps = {
  row: UtangAccountRow;
};

export function UtangAccountCard({ row }: UtangAccountCardProps) {
  const { t } = useI18n();
  const direction =
    row.perspective === "lent" ? t("personal.utang.owesYou") : t("personal.utang.youOwe");
  const ownershipLabel = row.isLedgerOwner
    ? t("personal.utang.ownershipMine")
    : t("personal.utang.ownershipSharedWithMe");

  return (
    <Link
      to={`/personal/utang/relationships/${row.relationshipId}`}
      className={cn(
        "utang-account-card exits-list__card flex items-center justify-between gap-3 text-foreground no-underline",
        !row.isLedgerOwner && "utang-account-card--shared",
      )}
      data-testid={`utang-account-${row.relationshipId}`}
    >
      <PersonAvatar name={row.displayName} size="sm" />
      <div className="min-w-0 flex-1">
        <p
          className="m-0 flex min-w-0 items-center justify-between gap-2 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted"
          data-utang-ownership=""
          data-testid={`utang-account-ownership-${row.relationshipId}`}
        >
          <span className="min-w-0 truncate">{ownershipLabel}</span>
          {!row.isLedgerOwner ? (
            <StatusChip
              tone={UTANG_READ_ONLY_CHIP.tone}
              appearance={UTANG_READ_ONLY_CHIP.appearance}
              shape={UTANG_READ_ONLY_CHIP.shape}
              data-testid={`utang-account-readonly-${row.relationshipId}`}
            >
              {t("personal.utang.readOnly")}
            </StatusChip>
          ) : null}
        </p>
        <p className="exits-list__name m-0 truncate font-semibold">{row.displayName}</p>
        <p className="m-0 flex min-w-0 items-center gap-1 truncate text-[length:var(--exits-text-sm)] text-muted">
          <span className="truncate">{direction}</span>
          {row.isSharedLedger ? (
            <>
              <span aria-hidden="true">·</span>
              <UtangLinkedIcon testId={`utang-account-linked-${row.relationshipId}`} />
            </>
          ) : null}
        </p>
      </div>
      <div className="flex shrink-0 items-center gap-1">
        <div className="flex flex-col items-end gap-0.5">
          <UtangDueCaption
            dueDateUtc={row.dueDateUtc}
            dueKind={row.dueKind}
            testId={`utang-account-due-${row.relationshipId}`}
          />
          <MoneyDisplay
            amount={row.currentBalance}
            className={cn(
              "text-[length:var(--exits-text-base)] leading-tight",
              row.perspective === "owe" && row.currentBalance > 0 && "text-[var(--exits-warning)]",
            )}
            testId={`utang-account-balance-${row.relationshipId}`}
          />
        </div>
        <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
      </div>
    </Link>
  );
}
