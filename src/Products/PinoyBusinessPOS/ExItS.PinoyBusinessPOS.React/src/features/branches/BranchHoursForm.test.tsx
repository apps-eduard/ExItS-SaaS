import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BranchHoursForm } from "@/features/branches/BranchHoursForm";
import {
  applyHoursToAllDays,
  defaultHoursSchedule,
  hasConfiguredHours,
} from "@/features/branches/branch-hours";
import { catalogs } from "@/i18n/messages";

const t = (key: keyof typeof catalogs.en) => catalogs.en[key];

describe("branch-hours helpers", () => {
  it("applies open/close template to every day", () => {
    const next = applyHoursToAllDays(defaultHoursSchedule(), {
      isClosed: false,
      isOpen24Hours: false,
      openTime: "09:00",
      closeTime: "18:00",
    });
    expect(hasConfiguredHours(next)).toBe(true);
    expect(next.every((d) => !d.isClosed && !d.isOpen24Hours)).toBe(true);
    expect(next.every((d) => d.openTime === "09:00" && d.closeTime === "18:00")).toBe(true);
  });

  it("applies 24-hour template to every day", () => {
    const next = applyHoursToAllDays(defaultHoursSchedule(), {
      isClosed: false,
      isOpen24Hours: true,
      openTime: "08:00",
      closeTime: "21:00",
    });
    expect(next.every((d) => d.isOpen24Hours && !d.isClosed)).toBe(true);
  });
});

describe("BranchHoursForm", () => {
  it("applies bulk open/close to all days", async () => {
    const user = userEvent.setup();
    const onReplaceHours = vi.fn();
    render(
      <BranchHoursForm
        hours={defaultHoursSchedule()}
        t={t}
        onUpdateHour={vi.fn()}
        onReplaceHours={onReplaceHours}
      />,
    );

    await user.clear(screen.getByTestId("hours-bulk-start"));
    await user.type(screen.getByTestId("hours-bulk-start"), "10:00");
    await user.clear(screen.getByTestId("hours-bulk-end"));
    await user.type(screen.getByTestId("hours-bulk-end"), "20:00");
    await user.click(screen.getByTestId("hours-apply-all-open"));

    expect(onReplaceHours).toHaveBeenCalled();
    const next = onReplaceHours.mock.calls[0]?.[0] as ReturnType<typeof defaultHoursSchedule>;
    expect(next.every((d) => d.openTime === "10:00" && d.closeTime === "20:00" && !d.isClosed)).toBe(
      true,
    );
  });

  it("applies one day schedule to all days", async () => {
    const user = userEvent.setup();
    const onReplaceHours = vi.fn();
    const hours = defaultHoursSchedule().map((d) =>
      d.dayOfWeek === "Monday"
        ? { ...d, isClosed: false, isOpen24Hours: false, openTime: "07:30", closeTime: "19:30" }
        : d,
    );
    render(
      <BranchHoursForm
        hours={hours}
        t={t}
        onUpdateHour={vi.fn()}
        onReplaceHours={onReplaceHours}
      />,
    );

    await user.click(screen.getByTestId("hours-apply-from-Monday"));
    const next = onReplaceHours.mock.calls[0]?.[0] as ReturnType<typeof defaultHoursSchedule>;
    expect(next.every((d) => d.openTime === "07:30" && d.closeTime === "19:30" && !d.isClosed)).toBe(
      true,
    );
  });

  it("marks every day open 24 hours", async () => {
    const user = userEvent.setup();
    const onReplaceHours = vi.fn();
    render(
      <BranchHoursForm
        hours={defaultHoursSchedule()}
        t={t}
        onUpdateHour={vi.fn()}
        onReplaceHours={onReplaceHours}
      />,
    );
    await user.click(screen.getByTestId("hours-apply-all-24h"));
    const next = onReplaceHours.mock.calls[0]?.[0] as ReturnType<typeof defaultHoursSchedule>;
    expect(next.every((d) => d.isOpen24Hours && !d.isClosed)).toBe(true);
  });
});
