import { useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { dayAriaLabel, formatDisplayDate, todayCalendarDate } from "./calendar";
import { DatePicker } from "./DatePicker";
import surface from "./surface.module.scss";

function installMatchMedia(compact: boolean) {
  Object.defineProperty(window, "matchMedia", {
    configurable: true,
    writable: true,
    value: (query: string) => ({
      matches: query.includes("max-width: 767px") ? compact : false,
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    }),
  });
}

async function openDate(user: ReturnType<typeof userEvent.setup>, name: string | RegExp) {
  await user.click(screen.getByRole("button", { name }));
}

describe("DatePicker", () => {
  beforeEach(() => {
    installMatchMedia(false);
    document.documentElement.setAttribute("data-theme", "light");
    document.documentElement.setAttribute("data-palette", "teal");
  });

  afterEach(() => {
    document.documentElement.removeAttribute("data-theme");
    document.documentElement.removeAttribute("data-palette");
  });

  it("selects a date in uncontrolled mode and clears it", async () => {
    const user = userEvent.setup();
    render(<DatePicker label="Date" defaultValue="2024-06-15" locale="en-US" name="due" />);
    expect(document.querySelector("input[name='due']")).toHaveValue("2024-06-15");
    await openDate(user, `Date, ${formatDisplayDate("2024-06-15", "en-US")}`);
    await user.click(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-16", "en-US", todayCalendarDate()) }));
    expect(screen.getByRole("button", { name: `Date, ${formatDisplayDate("2024-06-16", "en-US")}` })).toBeInTheDocument();
    expect(document.querySelector("input[name='due']")).toHaveValue("2024-06-16");
    await user.click(screen.getByRole("button", { name: "Clear Date" }));
    expect(screen.getByRole("button", { name: "Date" })).toBeInTheDocument();
    expect(document.querySelector("input[name='due']")).toHaveValue("");
  });

  it("reports changes without moving a controlled value", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<DatePicker label="Date" value="2024-06-15" onChange={onChange} locale="en-US" />);
    await openDate(user, `Date, ${formatDisplayDate("2024-06-15", "en-US")}`);
    await user.click(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-18", "en-US", todayCalendarDate()) }));
    expect(onChange).toHaveBeenCalledWith("2024-06-18");
    expect(screen.getByRole("button", { name: `Date, ${formatDisplayDate("2024-06-15", "en-US")}` })).toBeInTheDocument();
  });

  it("updates when the controlled value changes", async () => {
    const user = userEvent.setup();
    function Harness() {
      const [value, setValue] = useState<string | null>("2024-06-15");
      return <DatePicker label="Date" value={value} onChange={setValue} locale="en-US" />;
    }
    render(<Harness />);
    await openDate(user, `Date, ${formatDisplayDate("2024-06-15", "en-US")}`);
    await user.click(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-20", "en-US", todayCalendarDate()) }));
    expect(screen.getByRole("button", { name: `Date, ${formatDisplayDate("2024-06-20", "en-US")}` })).toBeInTheDocument();
  });

  it("navigates months and years and keeps February 29 only on leap years", async () => {
    const user = userEvent.setup();
    const { rerender } = render(<DatePicker label="Date" defaultValue="2024-02-10" locale="en-US" />);
    await openDate(user, `Date, ${formatDisplayDate("2024-02-10", "en-US")}`);
    expect(screen.getByRole("gridcell", { name: dayAriaLabel("2024-02-29", "en-US", todayCalendarDate()) })).toBeEnabled();
    await user.click(screen.getByRole("button", { name: "Next month" }));
    expect(screen.getByRole("button", { name: "Choose month, March 2024" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Previous month" }));
    await user.click(screen.getByRole("button", { name: "Previous month" }));
    expect(screen.getByRole("button", { name: "Choose month, January 2024" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Choose month, January 2024" }));
    await user.click(screen.getByRole("button", { name: "Choose year, 2024" }));
    await user.click(screen.getByRole("button", { name: "Next years" }));
    await user.click(screen.getByRole("gridcell", { name: "2028" }));
    await user.click(screen.getByRole("gridcell", { name: "January" }));
    expect(screen.getByRole("button", { name: "Choose month, January 2028" })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    rerender(<DatePicker key="leap-off" label="Date" defaultValue="2023-02-10" locale="en-US" />);
    await openDate(user, `Date, ${formatDisplayDate("2023-02-10", "en-US")}`);
    expect(screen.queryByRole("gridcell", { name: "Feb 29, 2023" })).not.toBeInTheDocument();
  });

  it("blocks dates outside min, max, and disabled dates", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <DatePicker
        label="Date"
        defaultValue="2024-06-15"
        locale="en-US"
        min="2024-06-10"
        max="2024-06-20"
        disabledDates={["2024-06-18"]}
        onChange={onChange}
      />,
    );
    await openDate(user, `Date, ${formatDisplayDate("2024-06-15", "en-US")}`);
    const early = screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-09", "en-US", todayCalendarDate()) });
    const blocked = screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-18", "en-US", todayCalendarDate()) });
    expect(early).toHaveAttribute("aria-disabled", "true");
    expect(blocked).toHaveAttribute("aria-disabled", "true");
    await user.click(early);
    await user.click(blocked);
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("dialog", { name: "Choose date" })).toBeInTheDocument();
    await user.click(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-10", "en-US", todayCalendarDate()) }));
    expect(onChange).toHaveBeenCalledWith("2024-06-10");
  });

  it("moves by day, month, and year from the keyboard", async () => {
    const user = userEvent.setup();
    render(<DatePicker label="Date" defaultValue="2024-06-15" locale="en-US" weekStartsOn={0} />);
    await openDate(user, `Date, ${formatDisplayDate("2024-06-15", "en-US")}`);
    expect(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-15", "en-US", todayCalendarDate()) })).toHaveFocus();
    await user.keyboard("{Home}");
    expect(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-09", "en-US", todayCalendarDate()) })).toHaveFocus();
    await user.keyboard("{End}");
    expect(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-15", "en-US", todayCalendarDate()) })).toHaveFocus();
    await user.keyboard("{ArrowRight}");
    expect(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-16", "en-US", todayCalendarDate()) })).toHaveFocus();
    await user.keyboard("{PageDown}");
    expect(screen.getByRole("button", { name: "Choose month, July 2024" })).toBeInTheDocument();
    await user.keyboard("{Shift>}{PageDown}{/Shift}");
    expect(screen.getByRole("button", { name: "Choose month, July 2025" })).toBeInTheDocument();
    await user.keyboard("{Enter}");
    expect(screen.getByRole("button", { name: `Date, ${formatDisplayDate("2025-07-16", "en-US")}` })).toBeInTheDocument();
  });

  it("marks today and can jump to it", async () => {
    const user = userEvent.setup();
    const today = todayCalendarDate();
    render(<DatePicker label="Date" locale="en-US" />);
    await openDate(user, "Date");
    expect(screen.getByRole("gridcell", { name: dayAriaLabel(today, "en-US", today) })).toHaveAttribute("aria-current", "date");
    await user.click(screen.getByRole("button", { name: "Today" }));
    expect(screen.getByRole("button", { name: `Date, ${formatDisplayDate(today, "en-US")}` })).toBeInTheDocument();
  });

  it("keeps the same picker usable in light and dark", async () => {
    const user = userEvent.setup();
    document.documentElement.setAttribute("data-theme", "dark");
    document.documentElement.setAttribute("data-palette", "violet");
    render(<DatePicker label="Date" defaultValue="2024-06-15" locale="en-US" />);
    await openDate(user, `Date, ${formatDisplayDate("2024-06-15", "en-US")}`);
    const dialog = screen.getByRole("dialog", { name: "Choose date" });
    expect(dialog).toHaveClass(surface.popover);
    expect(dialog.getAttribute("style") ?? "").not.toMatch(/#|rgb|hsl/);
    expect(document.documentElement).toHaveAttribute("data-theme", "dark");
    document.documentElement.setAttribute("data-theme", "light");
    document.documentElement.setAttribute("data-palette", "rose");
    await user.click(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-17", "en-US", todayCalendarDate()) }));
    expect(screen.getByRole("button", { name: `Date, ${formatDisplayDate("2024-06-17", "en-US")}` })).toBeInTheDocument();
  });

  it("anchors a desktop popover and uses the bottom sheet on a phone", async () => {
    const user = userEvent.setup();
    const { unmount } = render(<DatePicker label="Date" locale="en-US" />);
    const trigger = screen.getByRole("button", { name: "Date" });
    expect(trigger.closest("[data-viewport]")).toHaveAttribute("data-viewport", "regular");
    await user.click(trigger);
    const dialog = screen.getByRole("dialog", { name: "Choose date" });
    expect(dialog).toHaveAttribute("data-surface", "popover");
    expect(dialog.style.top).not.toBe("");
    expect(dialog.style.left).not.toBe("");
    expect(screen.queryByRole("button", { name: "Dismiss" })).not.toBeInTheDocument();
    unmount();

    installMatchMedia(true);
    render(<DatePicker label="Date" locale="en-US" />);
    expect(screen.getByRole("button", { name: "Date" }).closest("[data-viewport]")).toHaveAttribute("data-viewport", "compact");
    await user.click(screen.getByRole("button", { name: "Date" }));
    expect(screen.getByRole("button", { name: "Dismiss" })).toBeInTheDocument();
    expect(screen.getByRole("dialog", { name: "Choose date" })).toBeInTheDocument();
    expect(document.querySelector("[data-surface='sheet']")).toBeTruthy();
  });
});
