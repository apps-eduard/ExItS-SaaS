import { formatDueLabel } from "@/api/platform/personal-utang-client";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type DueKind = "none" | "overdue" | "dueSoon" | "upcoming";

function dueLabelForKind(kind: Exclude<DueKind, "none">, t: ReturnType<typeof useI18n>["t"]) {
  if (kind === "overdue") return t("personal.utang.dueOverdue");
  if (kind === "dueSoon") return t("personal.utang.dueSoon");
  return t("personal.utang.dueUpcoming");
}

export function UtangDueCaption({
  dueDateUtc,
  dueKind,
  className,
  testId,
}: {
  dueDateUtc: string | null | undefined;
  dueKind?: DueKind;
  className?: string;
  testId?: string;
}) {
  const { t } = useI18n();
  const due = dueKind
    ? { kind: dueKind, iso: dueDateUtc ?? null }
    : formatDueLabel(dueDateUtc);
  if (due.kind === "none" || !due.iso) return null;

  const label = dueLabelForKind(due.kind, t);
  const dueDateText = new Date(due.iso).toLocaleDateString();

  return (
    <span
      className={cn(
        "whitespace-nowrap text-[length:var(--exits-text-xs)] leading-none",
        due.kind === "overdue" || due.kind === "dueSoon"
          ? "font-medium text-[var(--exits-warning)]"
          : "text-muted",
        className,
      )}
      data-testid={testId}
    >
      {`${label} · ${dueDateText}`}
    </span>
  );
}

export function UtangDirectionTags({
  direction,
  shared,
  linkTestId,
}: {
  direction: "lent" | "owe";
  shared: boolean;
  linkTestId?: string;
}) {
  const { t } = useI18n();
  const owesYou = direction === "lent";

  return (
    <span className="flex min-w-0 flex-wrap items-center gap-1">
      <StatusChip
        tone={owesYou ? "success" : "warning"}
        appearance="emphasis"
        shape="square"
      >
        {owesYou ? t("personal.utang.owesYou") : t("personal.utang.youOwe")}
      </StatusChip>
      <span className="text-muted" aria-hidden="true">·</span>
      <StatusChip
        tone={shared ? "success" : "info"}
        appearance="emphasis"
        shape="square"
        data-testid={linkTestId}
      >
        {shared ? t("people.status.connected") : t("people.localContact")}
      </StatusChip>
    </span>
  );
}
