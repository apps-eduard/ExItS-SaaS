import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import {
  contactSheetStatusLabel,
  formatContactSheetDueLabel,
} from "@/features/personal/ContactBalanceSummary";
import type { PeopleRowModel } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

/**
 * Sheet upper profile stack (centered):
 * badge → I Owe/I Lent → amount → overdue badge (when applicable)
 */
export function ContactIdentityProfile({
  row,
  statusFallback,
  statusId,
  testId,
}: {
  row: PeopleRowModel;
  statusFallback: string;
  statusId: string;
  testId: string;
}) {
  const { t } = useI18n();
  const kindLabel = contactSheetStatusLabel(row, statusFallback, t);
  const utang = row.utang;
  const directionLabel = utang
    ? utang.direction === "lent"
      ? t("personal.home.kpiPrimaryLent")
      : t("personal.home.kpiPrimaryOwe")
    : null;
  const dueLabel = utang ? formatContactSheetDueLabel(utang.dueDateUtc, t) : null;
  const overdueBadge = dueLabel?.overdue ? dueLabel : null;

  return (
    <div
      id={statusId}
      className="personal-contact-sheet__profile"
      data-testid={testId}
    >
      <span
        className="personal-contact-sheet__kind-badge"
        data-testid={`${testId}-kind`}
      >
        {kindLabel}
      </span>

      {utang && directionLabel ? (
        <>
          <p
            className={cn(
              "personal-contact-sheet__relation",
              utang.direction === "lent"
                ? "personal-contact-sheet__relation--lent"
                : "personal-contact-sheet__relation--owe",
            )}
            data-testid={`${testId}-direction`}
          >
            {directionLabel}
          </p>
          <div
            className="personal-contact-sheet__profile-amount"
            data-testid={`${testId}-amount`}
          >
            <MoneyDisplay
              amount={utang.balance}
              className="personal-contact-sheet__profile-balance"
              testId={`${testId}-balance`}
            />
          </div>
          {overdueBadge ? (
            <span
              className="personal-contact-sheet__overdue-badge"
              data-testid={`${testId}-due`}
            >
              {overdueBadge.text}
            </span>
          ) : null}
        </>
      ) : null}
    </div>
  );
}
