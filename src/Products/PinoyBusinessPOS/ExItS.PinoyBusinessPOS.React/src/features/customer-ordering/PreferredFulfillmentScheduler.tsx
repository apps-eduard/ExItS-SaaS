import { useMemo, useRef, useState } from "react";
import { SoftGlassDatePopover } from "@/components/exits/SoftGlassDatePopover";
import { PreferredChoiceCard } from "@/features/customer-ordering/PreferredChoiceCard";
import { formatPreferredFulfillmentLongDate } from "@/features/customer-ordering/preferred-fulfillment-display";
import {
  addCalendarDays,
  availablePreferredPeriods,
  isDayClosed,
  isPreferredDateSelectable,
  periodCaption,
  type PreferredFulfillmentPeriod,
} from "@/features/customer-ordering/preferred-fulfillment-periods";
import {
  branchLocalClock,
  pickupMaxScheduleDate,
  type PickupHoursDay,
} from "@/features/customer-ordering/pickup-time-bounds";
import { useI18n } from "@/i18n/I18nProvider";

export type PreferredDayChoice = "today" | "tomorrow" | "another";

export type PreferredFulfillmentSchedulerProps = {
  /** "pickup" | "delivery" — drives Soft Glass day heading copy. */
  mode: "pickup" | "delivery";
  dayChoice: PreferredDayChoice;
  onDayChoiceChange: (next: PreferredDayChoice) => void;
  anotherDate: string;
  onAnotherDateChange: (next: string) => void;
  period: PreferredFulfillmentPeriod | null;
  onPeriodChange: (next: PreferredFulfillmentPeriod | null) => void;
  timeZoneId?: string | null;
  operatingHours?: PickupHoursDay[] | null;
  minPreparationLeadMinutes?: number;
  maxAdvanceScheduleDays?: number;
  scheduleRequired: boolean;
  error?: string | null;
  testIdPrefix?: string;
};

export function PreferredFulfillmentScheduler({
  mode,
  dayChoice,
  onDayChoiceChange,
  anotherDate,
  onAnotherDateChange,
  period,
  onPeriodChange,
  timeZoneId,
  operatingHours,
  minPreparationLeadMinutes = 0,
  maxAdvanceScheduleDays = 0,
  scheduleRequired,
  error,
  testIdPrefix = "preferred-fulfillment",
}: PreferredFulfillmentSchedulerProps) {
  const { t } = useI18n();
  const anotherCardRef = useRef<HTMLDivElement | null>(null);
  const [anotherCalendarOpen, setAnotherCalendarOpen] = useState(false);
  const now = useMemo(() => new Date(), []);
  const storeToday = branchLocalClock(now, timeZoneId).date;
  const storeTomorrow = addCalendarDays(storeToday, 1);
  const storeMaxDate =
    pickupMaxScheduleDate({
      now,
      timeZoneId,
      maxAdvanceScheduleDays,
    }) ?? undefined;

  const selectedDate =
    dayChoice === "today"
      ? storeToday
      : dayChoice === "tomorrow"
        ? storeTomorrow
        : anotherDate;

  const todaySelectable = isPreferredDateSelectable({
    date: storeToday,
    now,
    timeZoneId,
    operatingHours,
    maxAdvanceScheduleDays,
  });
  const tomorrowSelectable = isPreferredDateSelectable({
    date: storeTomorrow,
    now,
    timeZoneId,
    operatingHours,
    maxAdvanceScheduleDays,
  });

  const periods = selectedDate
    ? availablePreferredPeriods({
        date: selectedDate,
        now,
        timeZoneId,
        operatingHours,
        minPreparationLeadMinutes,
      })
    : [];

  const resolvedPeriod =
    period && periods.includes(period) ? period : null;

  function openAnotherCalendar() {
    onDayChoiceChange("another");
    onPeriodChange(null);
    setAnotherCalendarOpen(true);
  }

  return (
    <div className="flex flex-col gap-3" data-testid={testIdPrefix}>
      <div className="flex flex-col gap-2">
        <span className="pc-field__label" data-testid={`${testIdPrefix}-day-label`}>
          {mode === "delivery" ? t("orders.preferredDelivery") : t("orders.preferredPickup")}
        </span>
        <div className="flex flex-wrap gap-2" data-testid={`${testIdPrefix}-day-choices`}>
          <PreferredChoiceCard
            testId={`${testIdPrefix}-day-today`}
            title={t("orders.preferredDayToday")}
            caption={formatPreferredFulfillmentLongDate(storeToday)}
            selected={dayChoice === "today"}
            disabled={!todaySelectable}
            onClick={() => {
              setAnotherCalendarOpen(false);
              onDayChoiceChange("today");
              onPeriodChange(null);
            }}
          />
          <PreferredChoiceCard
            testId={`${testIdPrefix}-day-tomorrow`}
            title={t("orders.preferredDayTomorrow")}
            caption={formatPreferredFulfillmentLongDate(storeTomorrow)}
            selected={dayChoice === "tomorrow"}
            disabled={!tomorrowSelectable}
            onClick={() => {
              setAnotherCalendarOpen(false);
              onDayChoiceChange("tomorrow");
              onPeriodChange(null);
            }}
          />
          <div ref={anotherCardRef} className="personal-preferred-period-anchor">
            <PreferredChoiceCard
              testId={`${testIdPrefix}-day-another`}
              title={t("orders.preferredDayAnother")}
              caption={
                anotherDate
                  ? formatPreferredFulfillmentLongDate(anotherDate)
                  : t("orders.preferredDayAnotherCaption")
              }
              selected={dayChoice === "another"}
              onClick={() => {
                if (dayChoice === "another" && anotherCalendarOpen) {
                  setAnotherCalendarOpen(false);
                  return;
                }
                openAnotherCalendar();
              }}
            />
          </div>
        </div>
      </div>

      <SoftGlassDatePopover
        open={anotherCalendarOpen}
        onOpenChange={setAnotherCalendarOpen}
        anchorRef={anotherCardRef}
        label={t("orders.preferredChooseDate")}
        value={anotherDate}
        minDate={storeToday}
        maxDate={storeMaxDate}
        isDateDisabled={(iso) =>
          !isPreferredDateSelectable({
            date: iso,
            now,
            timeZoneId,
            operatingHours,
            maxAdvanceScheduleDays,
          }) || isDayClosed(iso, operatingHours)
        }
        onChange={(next) => {
          onAnotherDateChange(next);
          onDayChoiceChange("another");
          onPeriodChange(null);
        }}
        testId={`${testIdPrefix}-date-picker`}
      />

      <div className="flex flex-col gap-2">
        <span className="pc-field__label" data-testid={`${testIdPrefix}-period-label`}>
          {t("orders.preferredFulfillmentPeriod")}
        </span>
        <div className="flex flex-wrap gap-2" data-testid={`${testIdPrefix}-period-choices`}>
          {(["Morning", "Afternoon", "Evening"] as const).map((option) => {
            const enabled = periods.includes(option);
            return (
              <PreferredChoiceCard
                key={option}
                testId={`${testIdPrefix}-period-${option.toLowerCase()}`}
                title={t(`orders.preferredPeriod${option}` as "orders.preferredPeriodMorning")}
                caption={periodCaption(option)}
                selected={resolvedPeriod === option}
                disabled={!enabled || !selectedDate}
                onClick={() => onPeriodChange(option)}
              />
            );
          })}
        </div>
        {selectedDate && periods.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid={`${testIdPrefix}-no-periods`}>
            {t("orders.preferredNoPeriods")}
          </p>
        ) : null}
      </div>

      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted" data-testid={`${testIdPrefix}-hint`}>
        {scheduleRequired ? t("orders.preferredScheduleRequiredHint") : t("orders.preferredFulfillmentHint")}
      </p>
      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid={`${testIdPrefix}-error`}>
          {error}
        </p>
      ) : null}
    </div>
  );
}

export function resolvePreferredScheduleDate(input: {
  dayChoice: PreferredDayChoice;
  anotherDate: string;
  timeZoneId?: string | null;
  now?: Date;
}): string {
  const clock = branchLocalClock(input.now ?? new Date(), input.timeZoneId);
  if (input.dayChoice === "today") return clock.date;
  if (input.dayChoice === "tomorrow") return addCalendarDays(clock.date, 1);
  return input.anotherDate.trim();
}
