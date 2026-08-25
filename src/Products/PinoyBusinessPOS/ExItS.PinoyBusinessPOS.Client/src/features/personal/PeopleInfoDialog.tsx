import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";

export function PeopleInfoDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useI18n();

  if (!open) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-[var(--exits-z-notice)] flex items-end justify-center bg-black/40 p-4 sm:items-center"
      role="presentation"
      onClick={onClose}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          onClose();
        }
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="people-info-title"
        className="w-full max-w-sm rounded-[var(--exits-radius-lg)] border border-border bg-surface p-[var(--exits-density-card-padding)] shadow-lg"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="flex flex-col gap-3">
          <h2 id="people-info-title" className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {t("people.info.title")}
          </h2>
          <p className="m-0 text-muted">{t("people.info.body1")}</p>
          <p className="m-0 text-muted">{t("people.info.body2")}</p>
          <p className="m-0 text-muted">{t("people.info.body3")}</p>
          <Button type="button" className="self-end" onClick={onClose}>
            {t("people.info.close")}
          </Button>
        </div>
      </div>
    </div>
  );
}
