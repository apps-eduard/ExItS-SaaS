import { addCalendarDays } from "@/features/customer-ordering/preferred-fulfillment-periods";
import { branchLocalClock } from "@/features/customer-ordering/pickup-time-bounds";

/** Human label for preferred day + period without inventing an exact appointment time. */
export function formatPreferredFulfillmentLabel(input: {
  date?: string | null;
  period?: string | null;
  timeZoneId?: string | null;
  now?: Date;
  todayLabel?: string;
  tomorrowLabel?: string;
}): string | null {
  const date = input.date?.trim() ?? "";
  const period = input.period?.trim() ?? "";
  if (!date || !period) return null;

  const clock = branchLocalClock(input.now ?? new Date(), input.timeZoneId);
  const tomorrow = addCalendarDays(clock.date, 1);
  let dayLabel = date;
  if (date === clock.date) {
    dayLabel = input.todayLabel ?? "Today";
  } else if (date === tomorrow) {
    dayLabel = input.tomorrowLabel ?? "Tomorrow";
  }

  return `${dayLabel} — ${period}`;
}
