import { describe, expect, it } from "vitest";
import {
  formatStoreClock,
  formatStoreHoursSummary,
} from "@/features/customer-ordering/store-hours-summary";

describe("formatStoreHoursSummary", () => {
  it("formats Mon–Fri shared hours", () => {
    const weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"].map((dayOfWeek) => ({
      dayOfWeek,
      isClosed: false,
      isOpen24Hours: false,
      openTime: "08:00",
      closeTime: "22:00",
    }));
    const weekend = ["Saturday", "Sunday"].map((dayOfWeek) => ({
      dayOfWeek,
      isClosed: true,
      isOpen24Hours: false,
      openTime: null,
      closeTime: null,
    }));
    expect(formatStoreHoursSummary([...weekdays, ...weekend])).toBe("Mon–Fri 8 AM – 10 PM");
  });

  it("keeps separate windows when hours differ", () => {
    expect(
      formatStoreHoursSummary([
        {
          dayOfWeek: "Monday",
          isClosed: false,
          isOpen24Hours: false,
          openTime: "08:00",
          closeTime: "17:00",
        },
        {
          dayOfWeek: "Saturday",
          isClosed: false,
          isOpen24Hours: false,
          openTime: "09:00",
          closeTime: "13:00",
        },
      ]),
    ).toBe("Mon 8 AM – 5 PM; Sat 9 AM – 1 PM");
  });

  it("formats clocks compactly", () => {
    expect(formatStoreClock("08:00")).toBe("8 AM");
    expect(formatStoreClock("22:30")).toBe("10:30 PM");
  });
});
