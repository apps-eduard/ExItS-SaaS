import {
  hasConfiguredHours,
  ORDERED_WEEKDAYS,
  type HoursDayDraft,
} from "@/features/branches/branch-hours";
import type { MessageKey } from "@/i18n/messages";

function dayLabelKey(day: string): MessageKey {
  const map: Record<string, MessageKey> = {
    Monday: "branches.day.monday",
    Tuesday: "branches.day.tuesday",
    Wednesday: "branches.day.wednesday",
    Thursday: "branches.day.thursday",
    Friday: "branches.day.friday",
    Saturday: "branches.day.saturday",
    Sunday: "branches.day.sunday",
  };
  return map[day] ?? "branches.day.monday";
}

type BranchHoursFormProps = {
  hours: HoursDayDraft[];
  t: (key: MessageKey) => string;
  onUpdateHour: (dayOfWeek: string, patch: Partial<HoursDayDraft>) => void;
};

export function BranchHoursForm({ hours, t, onUpdateHour }: BranchHoursFormProps) {
  return (
    <section
      className="catalog-form-section exits-animate-panel gap-3"
      data-testid="branch-hours-section"
    >
      <h2 className="catalog-form-section__title">{t("branches.hoursTitle")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {hasConfiguredHours(hours) ? t("branches.hoursConfigured") : t("branches.hoursNotConfigured")}
      </p>
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {ORDERED_WEEKDAYS.map((dayName) => {
          const day = hours.find((h) => h.dayOfWeek === dayName)!;
          return (
            <li key={dayName} className="branch-hours-day">
              <p className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
                {t(dayLabelKey(dayName))}
              </p>
              <div className="flex flex-wrap gap-3">
                <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                  <input
                    type="radio"
                    name={`hours-mode-${dayName}`}
                    checked={!day.isClosed && !day.isOpen24Hours}
                    onChange={() =>
                      onUpdateHour(dayName, { isClosed: false, isOpen24Hours: false })
                    }
                    data-testid={`hours-open-${dayName}`}
                  />
                  {t("branches.hoursOpen")}
                </label>
                <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                  <input
                    type="radio"
                    name={`hours-mode-${dayName}`}
                    checked={day.isOpen24Hours && !day.isClosed}
                    onChange={() =>
                      onUpdateHour(dayName, { isClosed: false, isOpen24Hours: true })
                    }
                    data-testid={`hours-24h-${dayName}`}
                  />
                  {t("branches.hours24")}
                </label>
                <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                  <input
                    type="radio"
                    name={`hours-mode-${dayName}`}
                    checked={day.isClosed}
                    onChange={() =>
                      onUpdateHour(dayName, { isClosed: true, isOpen24Hours: false })
                    }
                    data-testid={`hours-closed-${dayName}`}
                  />
                  {t("branches.hoursClosed")}
                </label>
              </div>
              {!day.isClosed && !day.isOpen24Hours ? (
                <div className="mt-2 grid gap-2 sm:grid-cols-2">
                  <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
                    {t("branches.hoursStart")}
                    <input
                      type="time"
                      className="catalog-form-select font-normal"
                      value={day.openTime}
                      onChange={(e) => onUpdateHour(dayName, { openTime: e.target.value })}
                      data-testid={`hours-start-${dayName}`}
                    />
                  </label>
                  <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
                    {t("branches.hoursEnd")}
                    <input
                      type="time"
                      className="catalog-form-select font-normal"
                      value={day.closeTime}
                      onChange={(e) => onUpdateHour(dayName, { closeTime: e.target.value })}
                      data-testid={`hours-end-${dayName}`}
                    />
                  </label>
                </div>
              ) : null}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
