import { formatDueLabel } from "@/api/platform/personal-utang-client";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import type { PeopleRowModel } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

export function contactSheetStatusLabel(
  row: PeopleRowModel,
  fallbackStatus: string,
  t: (key: "people.contactSheet.localContact" | "people.contactSheet.sharedContact") => string,
): string {
  if (row.connectionStatus === "local") {
    return t("people.contactSheet.localContact");
  }
  if (row.connectionStatus === "connected") {
    return t("people.contactSheet.sharedContact");
  }
  return fallbackStatus;
}

export function contactSheetKindShort(
  row: PeopleRowModel,
  fallbackStatus: string,
  t: (
    key:
      | "people.contactSheet.contactShort"
      | "people.contactSheet.sharedShort"
      | "people.contactSheet.localContact"
      | "people.contactSheet.sharedContact",
  ) => string,
): string {
  if (row.connectionStatus === "local") {
    return t("people.contactSheet.contactShort");
  }
  if (row.connectionStatus === "connected") {
    return t("people.contactSheet.sharedShort");
  }
  return fallbackStatus;
}

export function formatContactSheetDueLabel(
  dueDateUtc: string | null | undefined,
  t: (key: MessageKey) => string,
): { text: string; overdue: boolean } | null {
  const due = formatDueLabel(dueDateUtc);
  if (due.kind === "none" || !due.iso) {
    return null;
  }
  const dateText = new Date(due.iso).toLocaleDateString();
  if (due.kind === "overdue") {
    return { text: `${t("personal.utang.dueOverdue")}: ${dateText}`, overdue: true };
  }
  if (due.kind === "dueSoon") {
    return { text: `${t("personal.utang.dueSoon")}: ${dateText}`, overdue: false };
  }
  return { text: `${t("personal.utang.dueUpcoming")}: ${dateText}`, overdue: false };
}

/**
 * Lent/owe identity header (one row):
 * [ I Lent / I Owe ]  [ Amount ]  [ Contact / Shared ]
 *              [ due / overdue if present ]
 */
export function ContactBalanceSummary({
  row,
  amount,
  statusFallback,
  testId,
  appearance = "card",
  kindVariant = "short",
  id,
}: {
  row: PeopleRowModel;
  amount: number;
  statusFallback: string;
  testId: string;
  /** `card` = bordered wash; `inline` = list/identity stack without border wash */
  appearance?: "card" | "inline";
  /** `short` = Contact/Shared; `full` = Local Contact/Shared Contact (sheet identity) */
  kindVariant?: "short" | "full";
  id?: string;
}) {
  const { t } = useI18n();
  const utang = row.utang;
  if (!utang) {
    return null;
  }
  const directionLabel =
    utang.direction === "lent" ? t("personal.home.kpiPrimaryLent") : t("personal.home.kpiPrimaryOwe");
  const kindFull = contactSheetStatusLabel(row, statusFallback, t);
  const kindLabel =
    kindVariant === "full" ? kindFull : contactSheetKindShort(row, statusFallback, t);
  const dueLabel = formatContactSheetDueLabel(utang.dueDateUtc, t);

  return (
    <div
      id={id}
      className={cn(
        "personal-contact-sheet__balance-summary",
        appearance === "inline" && "personal-contact-sheet__balance-summary--inline",
      )}
      data-testid={testId}
    >
      <div className="personal-contact-sheet__balance-summary-top">
        <span
          className={cn(
            "personal-contact-sheet__balance-direction",
            utang.direction === "lent"
              ? "personal-contact-sheet__balance-direction--lent"
              : "personal-contact-sheet__balance-direction--owe",
          )}
          data-testid={`${testId}-direction`}
        >
          {directionLabel}
        </span>
        <div className="personal-contact-sheet__balance-summary-amount">
          <MoneyDisplay
            amount={amount}
            className="personal-contact-sheet__balance-amount"
            testId={`${testId}-amount`}
          />
        </div>
        <span
          className="personal-contact-sheet__balance-kind"
          title={kindFull}
          aria-label={kindFull}
          data-testid={`${testId}-kind`}
        >
          {kindLabel}
        </span>
      </div>
      {dueLabel ? (
        <p
          className={cn(
            "personal-contact-sheet__balance-due",
            dueLabel.overdue && "personal-contact-sheet__balance-due--overdue",
          )}
          data-testid={`${testId}-due`}
        >
          {dueLabel.text}
        </p>
      ) : null}
    </div>
  );
}
