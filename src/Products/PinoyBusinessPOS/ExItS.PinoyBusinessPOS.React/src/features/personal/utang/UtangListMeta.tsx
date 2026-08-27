import { Link2 } from "lucide-react";
import { formatDueLabel } from "@/api/platform/personal-utang-client";
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

export function UtangLinkedIcon({ testId }: { testId?: string }) {
  const { t } = useI18n();
  const label = t("people.status.connected");

  return (
    <span
      className="inline-flex shrink-0 items-center gap-0.5 text-[length:var(--exits-text-xs)] font-bold text-[var(--exits-success)]"
      data-testid={testId}
    >
      <Link2 className="size-3.5 shrink-0" aria-hidden="true" />
      <span>{label}</span>
    </span>
  );
}
