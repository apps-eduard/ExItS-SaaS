import { useState } from "react";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DateTimePicker, type DateTimePickerProps } from "./DateTimePicker";
import type { DateSelectionAdapterProps } from "./DateSelectionAdapter";
import type { DateTimeValue } from "./datetime";

function FakeDate({ value, onChange }: DateSelectionAdapterProps) {
  return (
    <button type="button" onClick={() => onChange("2024-05-02")}>
      {`Date ${value ?? "empty"}`}
    </button>
  );
}

function InvalidDate({ onChange }: DateSelectionAdapterProps) {
  return (
    <button type="button" onClick={() => onChange("2023-02-29")}>
      Set leap day
    </button>
  );
}

function Harness(props: Partial<DateTimePickerProps>) {
  const [value, setValue] = useState<DateTimeValue | null>(props.value ?? null);
  return (
    <>
      <DateTimePicker label="Appointment" locale="en-US" hourCycle="h24" viewport="desktop" dateAdapter={FakeDate} {...props} value={value} onChange={setValue} />
      <output>{value ? `${value.date} ${value.time}` : "empty"}</output>
    </>
  );
}

describe("DateTimePicker", () => {
  it("combines the date adapter with a time and commits on Apply", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    await user.click(screen.getByRole("button", { name: "Date empty" }));
    await user.type(screen.getByLabelText("Hour"), "14");
    await user.type(screen.getByLabelText("Minute"), "30");
    await user.tab();
    expect(screen.getByText(/May 2, 2024, 14:30/)).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(screen.getByText("2024-05-02 14:30")).toBeInTheDocument();
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
        dateAdapter={FakeDate}
        now={now}
        onChange={onChange}
      />,
    );
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    await user.click(screen.getByRole("button", { name: "Date empty" }));
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

  it("rejects an invalid adapter date and a min datetime", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <DateTimePicker
        label="Appointment"
        locale="en-US"
        hourCycle="h24"
        viewport="desktop"
        dateAdapter={InvalidDate}
        min={{ date: "2024-05-02", time: "09:00" }}
        onChange={onChange}
      />,
    );
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    await user.click(screen.getByRole("button", { name: "Set leap day" }));
    await user.type(screen.getByLabelText("Hour"), "10");
    await user.type(screen.getByLabelText("Minute"), "00");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent(/not a leap year/i);
  });

  it("does not rewrite a daylight-saving gap or choose an ambiguous instant", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    function GapDate({ onChange: change }: DateSelectionAdapterProps) {
      return (
        <button type="button" onClick={() => change("2024-03-10")}>
          Set gap date
        </button>
      );
    }
    render(
      <DateTimePicker
        label="Appointment"
        locale="en-US"
        hourCycle="h24"
        viewport="desktop"
        timeZone="America/New_York"
        dateAdapter={GapDate}
        onChange={onChange}
      />,
    );
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    await user.click(screen.getByRole("button", { name: "Set gap date" }));
    await user.type(screen.getByLabelText("Hour"), "02");
    await user.type(screen.getByLabelText("Minute"), "30");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent(/does not exist/i);
  });

  it("uses the native date adapter and the mobile sheet", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<DateTimePicker label="Appointment" locale="en-US" hourCycle="h24" viewport="mobile" minuteStep={15} onChange={onChange} />);
    await user.click(screen.getByRole("button", { name: /Choose a date and time/ }));
    expect(screen.getByRole("dialog", { name: "Appointment" })).toBeInTheDocument();
    expect(screen.getByLabelText("Date", { selector: 'input[type="date"]' })).toHaveAttribute("data-adapter", "native-date");
    await user.click(screen.getByRole("radio", { name: "Time" }));
    expect(screen.getByRole("group", { name: "Type a time" })).toBeInTheDocument();
    await user.click(within(screen.getByRole("listbox", { name: "Hour" })).getByRole("option", { name: "09" }));
    await user.click(within(screen.getByRole("listbox", { name: "Minute" })).getByRole("option", { name: "15" }));
    await user.click(screen.getByRole("radio", { name: "Date" }));
    fireEvent.change(screen.getByLabelText("Date", { selector: 'input[type="date"]' }), { target: { value: "2024-05-02" } });
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith({ date: "2024-05-02", time: "09:15" });
  });
});
