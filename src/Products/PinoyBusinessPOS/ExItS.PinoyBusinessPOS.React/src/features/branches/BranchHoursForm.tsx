import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  applyHoursToAllDays,
  hasConfiguredHours,
  ORDERED_WEEKDAYS,
  templateFromDay,
  type HoursDayDraft,
  type HoursScheduleTemplate,
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
  onReplaceHours: (next: HoursDayDraft[]) => void;
};

export function BranchHoursForm({
  hours,
  t,
  onUpdateHour,
  onReplaceHours,
}: BranchHoursFormProps) {
  const [bulkOpen, setBulkOpen] = useState("08:00");
  const [bulkClose, setBulkClose] = useState("21:00");

  function applyBulkOpenClose() {
    const template: HoursScheduleTemplate = {
      isClosed: false,
      isOpen24Hours: false,
      openTime: bulkOpen,
      closeTime: bulkClose,
    };
    onReplaceHours(applyHoursToAllDays(hours, template));
  }

  function applyAllDayEveryDay() {
    onReplaceHours(
      applyHoursToAllDays(hours, {
        isClosed: false,
        isOpen24Hours: true,
        openTime: bulkOpen,
        closeTime: bulkClose,
      }),
    );
  }

  function applyClosedEveryDay() {
    onReplaceHours(
      applyHoursToAllDays(hours, {
        isClosed: true,
        isOpen24Hours: false,
        openTime: bulkOpen,
        closeTime: bulkClose,
      }),
    );
  }

  function applyDayToAll(dayName: string) {
    const day = hours.find((h) => h.dayOfWeek === dayName);
    if (!day) return;
    onReplaceHours(applyHoursToAllDays(hours, templateFromDay(day)));
  }

  return (
    <section
      className="catalog-form-section exits-animate-panel gap-3"
      data-testid="branch-hours-section"
    >
      <h2 className="catalog-form-section__title">{t("branches.hoursTitle")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {hasConfiguredHours(hours)
          ? t("branches.hoursConfigured")
          : t("branches.hoursNotConfigured")}
      </p>

      <div
        className="branch-hours-bulk flex flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border p-3"
        data-testid="branch-hours-bulk"
      >
        <div className="min-w-0">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.hoursSameAllDaysTitle")}
          </p>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("branches.hoursSameAllDaysLede")}
          </p>
        </div>

        <div className="grid gap-2 sm:grid-cols-2">
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.hoursStart")}
            <input
              type="time"
              className="catalog-form-select font-normal"
              value={bulkOpen}
              onChange={(e) => setBulkOpen(e.target.value)}
              data-testid="hours-bulk-start"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.hoursEnd")}
            <input
              type="time"
              className="catalog-form-select font-normal"
              value={bulkClose}
              onChange={(e) => setBulkClose(e.target.value)}
              data-testid="hours-bulk-end"
            />
          </label>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            data-testid="hours-apply-all-open"
            onClick={applyBulkOpenClose}
          >
            {t("branches.hoursApplyAll")}
          </Button>
          <Button
            type="button"
            variant="outline"
            data-testid="hours-apply-all-24h"
            onClick={applyAllDayEveryDay}
          >
            {t("branches.hoursAllDayEveryDay")}
          </Button>
          <Button
            type="button"
            variant="outline"
            data-testid="hours-apply-all-closed"
            onClick={applyClosedEveryDay}
          >
            {t("branches.hoursClosedEveryDay")}
          </Button>
        </div>
      </div>

      <ul
        className="branch-hours-days m-0 grid list-none grid-cols-1 gap-2 p-0 lg:grid-cols-2"
        data-testid="branch-hours-days"
      >
        {ORDERED_WEEKDAYS.map((dayName) => {
          const day = hours.find((h) => h.dayOfWeek === dayName)!;
          return (
            <li key={dayName} className="branch-hours-day" data-testid={`hours-day-${dayName}`}>
              <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t(dayLabelKey(dayName))}
                </p>
                <Button
                  type="button"
                  variant="outline"
                  data-testid={`hours-apply-from-${dayName}`}
                  onClick={() => applyDayToAll(dayName)}
                >
                  {t("branches.hoursApplyFromDay")}
                </Button>
              </div>
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
