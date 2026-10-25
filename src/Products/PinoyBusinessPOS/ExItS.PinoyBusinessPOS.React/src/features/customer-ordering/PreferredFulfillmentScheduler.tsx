import { useMemo } from "react";
import { SoftGlassDateField } from "@/components/exits/SoftGlassDateField";
import { Button } from "@/components/ui/button";
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

  return (
    <div className="flex flex-col gap-3" data-testid={testIdPrefix}>
      <div className="flex flex-col gap-2">
        <span className="pc-field__label" data-testid={`${testIdPrefix}-day-label`}>
          {mode === "delivery" ? t("orders.preferredDelivery") : t("orders.preferredPickup")}
        </span>
        <div className="flex flex-wrap gap-2" data-testid={`${testIdPrefix}-day-choices`}>
          <DayChip
            testId={`${testIdPrefix}-day-today`}
            label={t("orders.preferredDayToday")}
            selected={dayChoice === "today"}
            disabled={!todaySelectable}
            onClick={() => {
              onDayChoiceChange("today");
              onPeriodChange(null);
            }}
          />
          <DayChip
            testId={`${testIdPrefix}-day-tomorrow`}
            label={t("orders.preferredDayTomorrow")}
            selected={dayChoice === "tomorrow"}
            disabled={!tomorrowSelectable}
            onClick={() => {
              onDayChoiceChange("tomorrow");
              onPeriodChange(null);
            }}
          />
          <DayChip
            testId={`${testIdPrefix}-day-another`}
            label={t("orders.preferredDayAnother")}
            selected={dayChoice === "another"}
            onClick={() => {
              onDayChoiceChange("another");
              onPeriodChange(null);
            }}
          />
        </div>
      </div>

      {dayChoice === "another" ? (
        <SoftGlassDateField
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
            onPeriodChange(null);
          }}
          testId={`${testIdPrefix}-date-picker`}
        />
      ) : null}

      <div className="flex flex-col gap-2">
        <span className="pc-field__label" data-testid={`${testIdPrefix}-period-label`}>
          {t("orders.preferredFulfillmentPeriod")}
        </span>
        <div className="flex flex-wrap gap-2" data-testid={`${testIdPrefix}-period-choices`}>
          {(["Morning", "Afternoon", "Evening"] as const).map((option) => {
            const enabled = periods.includes(option);
            return (
              <button
                key={option}
                type="button"
                className={
                  "personal-preferred-period" +
                  (resolvedPeriod === option ? " personal-preferred-period--selected" : "")
                }
                disabled={!enabled || !selectedDate}
                aria-pressed={resolvedPeriod === option}
                data-testid={`${testIdPrefix}-period-${option.toLowerCase()}`}
                onClick={() => onPeriodChange(option)}
              >
                <span className="personal-preferred-period__title">
                  {t(`orders.preferredPeriod${option}` as "orders.preferredPeriodMorning")}
                </span>
                <span className="personal-preferred-period__caption">{periodCaption(option)}</span>
              </button>
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

function DayChip({
  label,
  selected,
  disabled,
  onClick,
  testId,
}: {
  label: string;
  selected: boolean;
  disabled?: boolean;
  onClick: () => void;
  testId: string;
}) {
  return (
    <Button
      type="button"
      intent={selected ? "primary" : "neutral"}
      appearance={selected ? "glossy" : "outline"}
      disabled={disabled}
      aria-pressed={selected}
      data-testid={testId}
      onClick={onClick}
      className="min-w-[6.5rem]"
    >
      {label}
    </Button>
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
