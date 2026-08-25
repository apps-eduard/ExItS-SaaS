import { Link } from "react-router-dom";
import { ChevronRight } from "lucide-react";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import type { UtangAccountRow } from "@/features/personal/utang/utang-workspace";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type UtangAccountCardProps = {
  row: UtangAccountRow;
};

export function UtangAccountCard({ row }: UtangAccountCardProps) {
  const { t } = useI18n();
  const direction =
    row.perspective === "lent" ? t("personal.utang.owesYou") : t("personal.utang.youOwe");

  const dueLabel =
    row.dueKind === "overdue"
      ? t("personal.utang.dueOverdue")
      : row.dueKind === "dueSoon"
        ? t("personal.utang.dueSoon")
        : row.dueKind === "upcoming"
          ? t("personal.utang.dueUpcoming")
          : null;

  const dueDateText = row.dueDateUtc
    ? new Date(row.dueDateUtc).toLocaleDateString()
    : null;

  return (
    <Link
      to={`/personal/utang/relationships/${row.relationshipId}`}
      className="utang-account-card exits-list__card flex min-h-11 items-center justify-between gap-3 text-foreground no-underline"
      data-testid={`utang-account-${row.relationshipId}`}
    >
      <div className="min-w-0 flex-1">
        <p className="exits-list__name m-0 truncate font-semibold">{row.displayName}</p>
        <p className="m-0 truncate text-[length:var(--exits-text-sm)] text-muted">
          {direction}
          {row.isSharedLedger ? ` · ${t("personal.utang.linked")}` : null}
        </p>
        <div className="mt-1 flex flex-wrap items-center gap-1.5">
          {dueLabel ? (
            <StatusChip tone={row.dueKind === "overdue" || row.dueKind === "dueSoon" ? "warning" : "info"}>
              {dueDateText ? `${dueLabel} · ${dueDateText}` : dueLabel}
            </StatusChip>
          ) : null}
        </div>
      </div>
      <div className="flex shrink-0 items-center gap-1">
        <MoneyDisplay
          amount={row.currentBalance}
          className={cn(
            "text-[length:var(--exits-text-base)]",
            row.perspective === "owe" && row.currentBalance > 0 && "text-[var(--exits-warning)]",
          )}
          testId={`utang-account-balance-${row.relationshipId}`}
        />
        <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
      </div>
    </Link>
  );
}
