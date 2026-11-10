import { useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { addDays, dayAriaLabel, formatRangeLabel, todayCalendarDate, type DateRange } from "./calendar";
import { DateRangePicker } from "./DateRangePicker";

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

const initial: DateRange = { start: "2024-06-10", end: "2024-06-12" };

describe("DateRangePicker", () => {
  beforeEach(() => {
    installMatchMedia(false);
    document.documentElement.setAttribute("data-theme", "light");
    document.documentElement.setAttribute("data-palette", "teal");
  });

  it("previews a range, swaps reversed clicks, and commits on Apply", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<DateRangePicker label="Range" defaultValue={initial} onChange={onChange} locale="en-US" weekStartsOn={0} />);
    await user.click(screen.getByRole("button", { name: /Range, Jun 10, 2024/ }));
    await user.click(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-18", "en-US", todayCalendarDate()) }));
    await user.hover(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-15", "en-US", todayCalendarDate()) }));
    expect(document.querySelector("[data-date='2024-06-16']")).toHaveAttribute("data-in-range", "true");
    expect(document.querySelector("[data-date='2024-06-16']")).toHaveAttribute("data-preview", "true");
    await user.click(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-15", "en-US", todayCalendarDate()) }));
    expect(screen.getByRole("status")).toHaveTextContent(formatRangeLabel({ start: "2024-06-15", end: "2024-06-18" }, "en-US"));
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith({ start: "2024-06-15", end: "2024-06-18" });
    expect(screen.getByRole("button", { name: /Jun 15, 2024 – Jun 18, 2024/ })).toBeInTheDocument();
  });

  it("discards a draft on Cancel and commits a cleared draft on Apply", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<DateRangePicker label="Range" defaultValue={initial} onChange={onChange} locale="en-US" />);
    await user.click(screen.getByRole("button", { name: /Range, Jun 10, 2024/ }));
    await user.click(screen.getByRole("button", { name: "Today" }));
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: /Jun 10, 2024/ })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /Range, Jun 10, 2024/ }));
    await user.click(screen.getByRole("button", { name: "Clear" }));
    expect(screen.getByRole("status")).toHaveTextContent("No dates selected");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith(null);
    expect(screen.getByRole("button", { name: "Range" })).toBeInTheDocument();
  });

  it("applies presets that honor constraints and refuses disabled dates inside a range", async () => {
    const user = userEvent.setup();
    const today = todayCalendarDate();
    const onChange = vi.fn();
    render(
      <DateRangePicker
        label="Range"
        locale="en-US"
        weekStartsOn={0}
        disabledDates={[addDays(today, -1)]}
        onChange={onChange}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Range" }));
    expect(screen.getByRole("button", { name: "Yesterday" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Last 7 Days" })).toBeDisabled();
    await user.click(screen.getByRole("button", { name: "Today" }));
    expect(screen.getByRole("status")).toHaveTextContent(formatRangeLabel({ start: today, end: today }, "en-US"));
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith({ start: today, end: today });
  });

  it("does not commit a range that contains a disabled date", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<DateRangePicker label="Range" defaultValue={initial} locale="en-US" disabledDates={["2024-06-16"]} onChange={onChange} />);
    await user.click(screen.getByRole("button", { name: /Range, Jun 10, 2024/ }));
    await user.click(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-15", "en-US", todayCalendarDate()) }));
    await user.click(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-18", "en-US", todayCalendarDate()) }));
    expect(screen.getByRole("status")).toHaveTextContent("Select an end date.");
    expect(screen.getByRole("button", { name: "Apply" })).toBeDisabled();
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onChange).not.toHaveBeenCalled();
  });

  it("supports controlled values and keyboard selection", async () => {
    const user = userEvent.setup();
    function Harness() {
      const [value, setValue] = useState<DateRange | null>(initial);
      return <DateRangePicker label="Range" value={value} onChange={setValue} locale="en-US" weekStartsOn={0} />;
    }
    render(<Harness />);
    await user.click(screen.getByRole("button", { name: /Range, Jun 10, 2024/ }));
    expect(screen.getByRole("gridcell", { name: dayAriaLabel("2024-06-10", "en-US", todayCalendarDate()) })).toHaveFocus();
    await user.keyboard("{Enter}");
    await user.keyboard("{ArrowRight}");
    await user.keyboard("{Enter}");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(screen.getByRole("button", { name: /Jun 10, 2024 – Jun 11, 2024/ })).toBeInTheDocument();
  });

  it("uses the bottom sheet and sticky actions on a phone", async () => {
    installMatchMedia(true);
    const user = userEvent.setup();
    render(<DateRangePicker label="Range" locale="en-US" />);
    expect(screen.getByRole("button", { name: "Range" }).closest("[data-viewport]")).toHaveAttribute("data-viewport", "compact");
    await user.click(screen.getByRole("button", { name: "Range" }));
    const dialog = screen.getByRole("dialog", { name: "Choose dates" });
    expect(screen.getByRole("button", { name: "Dismiss" })).toBeInTheDocument();
    expect(dialog.querySelector("[data-surface='sheet']")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Apply" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    document.documentElement.setAttribute("data-theme", "dark");
    expect(document.documentElement).toHaveAttribute("data-theme", "dark");
    expect(dialog.getAttribute("style") ?? "").not.toMatch(/#|rgb|hsl/);
  });
});
