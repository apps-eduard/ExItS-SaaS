import { useState } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { dayAriaLabel, todayCalendarDate } from "../date/calendar";
import { DateTimePicker, type DateTimePickerProps } from "./DateTimePicker";
import type { DateTimeValue } from "./datetime";

function Harness(props: Partial<DateTimePickerProps>) {
  const [value, setValue] = useState<DateTimeValue | null>(props.value ?? null);
  return (
    <>
      <DateTimePicker label="Appointment" locale="en-US" hourCycle="h24" viewport="desktop" {...props} value={value} onChange={setValue} />
      <output>{value ? `${value.date} ${value.time}` : "empty"}</output>
    </>
  );
}

async function chooseDate(user: ReturnType<typeof userEvent.setup>, year: string, month: string, day: string) {
  await user.click(screen.getByRole("button", { name: /Choose month/ }));
  await user.click(screen.getByRole("button", { name: /Choose year/ }));
  await user.click(screen.getByRole("gridcell", { name: year }));
  await user.click(screen.getByRole("gridcell", { name: month }));
  const cell = document.querySelector<HTMLElement>(`[data-date="${day}"]`);
  if (!cell) throw new Error(`Missing calendar day ${day}`);
  await user.click(cell);
}

describe("DateTimePicker", () => {
  it("combines the shared calendar with a time and commits on Apply", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    await chooseDate(user, "2024", "May", "2024-05-02");
    await user.type(screen.getByLabelText("Hour"), "14");
    await user.type(screen.getByLabelText("Minute"), "30");
    await user.tab();
    expect(screen.getByText(/May 2, 2024, 14:30/)).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(screen.getByText("2024-05-02 14:30")).toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: "Date" })).not.toBeInTheDocument();
  });

  it("rolls back draft edits on Cancel and commits Today and Now", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const now = () => new Date(2024, 4, 2, 14, 7, 40);
    render(
      <DateTimePicker
        label="Appointment"
        locale="en-US"
        hourCycle="h24"
        viewport="desktop"
        minuteStep={5}
        now={now}
        onChange={onChange}
      />,
    );
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    await chooseDate(user, "2024", "May", "2024-05-02");
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onChange).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    await user.click(screen.getByRole("button", { name: "Now" }));
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith({ date: "2024-05-02", time: "14:05" });

    onChange.mockClear();
    await user.click(screen.getByRole("button", { name: /May 2, 2024/ }));
    await user.click(screen.getByRole("button", { name: "Clear" }));
    await user.click(screen.getByRole("button", { name: "Today" }));
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent(/choose a time/i);
  });

  it("rejects a time before the minimum on the boundary day", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <DateTimePicker
        label="Appointment"
        locale="en-US"
        hourCycle="h24"
        viewport="desktop"
        min={{ date: "2024-05-02", time: "09:00" }}
        onChange={onChange}
      />,
    );
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    await chooseDate(user, "2024", "May", "2024-05-02");
    expect(screen.getByRole("gridcell", { name: dayAriaLabel("2024-05-01", "en-US", todayCalendarDate()) })).toHaveAttribute("aria-disabled", "true");
    await user.type(screen.getByLabelText("Hour"), "08");
    await user.type(screen.getByLabelText("Minute"), "00");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent(/09:00|or later/i);
  });

  it("does not rewrite a daylight-saving gap or choose an ambiguous instant", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <DateTimePicker label="Appointment" locale="en-US" hourCycle="h24" viewport="desktop" timeZone="America/New_York" onChange={onChange} />,
    );
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    await chooseDate(user, "2024", "March", "2024-03-10");
    await user.type(screen.getByLabelText("Hour"), "02");
    await user.type(screen.getByLabelText("Minute"), "30");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent(/does not exist/i);
  });

  it("uses the shared calendar inside the mobile sheet", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<DateTimePicker label="Appointment" locale="en-US" hourCycle="h24" viewport="mobile" minuteStep={15} onChange={onChange} />);
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    expect(screen.getByRole("dialog", { name: "Appointment" })).toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: "Date" })).not.toBeInTheDocument();
    expect(screen.getByRole("grid", { name: /November|2026|Choose month/i })).toBeInTheDocument();
    await user.click(screen.getByRole("radio", { name: "Time" }));
    expect(screen.getByRole("group", { name: "Type a time" })).toBeInTheDocument();
    await user.click(within(screen.getByRole("listbox", { name: "Hour" })).getByRole("option", { name: "09" }));
    await user.click(within(screen.getByRole("listbox", { name: "Minute" })).getByRole("option", { name: "15" }));
    await user.click(screen.getByRole("radio", { name: "Date" }));
    await chooseDate(user, "2024", "May", "2024-05-02");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith({ date: "2024-05-02", time: "09:15" });
  });
});
