import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { symbol } from "@/components/icons/MaterialSymbol";
import { type PeopleRowModel } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

const ChevronRightIcon = symbol("chevron_right");

export function PeopleMobileRow({
  row,
  statusLabel,
  onOpenContact,
}: {
  row: PeopleRowModel;
  statusLabel: string;
  onOpenContact?: () => void;
}) {
  const { t } = useI18n();
  const statusKey = row.connectionStatus;

  return (
    <li className="people-mobile-row-item">
      <button
        type="button"
        className={cn("people-mobile-row", `people-mobile-row--${statusKey}`)}
        data-testid={`people-row-${row.contact.id}`}
        data-status={statusKey}
        aria-label={`${t("people.contactSheet.open")} ${row.contact.displayName}`}
        onClick={() => onOpenContact?.()}
      >
        <PersonAvatar
          name={row.contact.displayName}
          size="sm"
          className="people-mobile-row__avatar"
        />
        <span className="people-mobile-row__main">
          <span
            className="people-mobile-row__name truncate"
            data-testid={`people-row-open-${row.contact.id}`}
          >
            {row.contact.displayName}
          </span>
          <span
            className="people-mobile-row__status"
            data-testid={`people-row-status-${row.contact.id}`}
          >
            {statusLabel}
          </span>
        </span>
        <span className="people-mobile-row__chevron" aria-hidden>
          <ChevronRightIcon className="people-mobile-row__chevron-icon" />
        </span>
      </button>
    </li>
  );
}
