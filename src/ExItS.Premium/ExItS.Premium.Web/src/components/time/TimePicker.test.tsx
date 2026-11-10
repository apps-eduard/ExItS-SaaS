import { useState } from "react";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { TimePicker } from "./TimePicker";
import { TimeWheel } from "./TimeWheel";

describe("TimeWheel", () => {
  it("selects the value shown in the hour and minute columns", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<TimeWheel value="14:30" onChange={onChange} hourCycle="h24" minuteStep={5} locale="en-US" />);
    expect(screen.getByRole("option", { name: "14" })).toHaveAttribute("aria-selected", "true");
    expect(screen.getByRole("option", { name: "30" })).toHaveAttribute("aria-selected", "true");
    await user.click(screen.getByRole("option", { name: "45" }));
    expect(onChange).toHaveBeenCalledWith("14:45");
  });

  it("moves the minute column from the keyboard and skips a disabled minute", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <TimeWheel
        value="14:30"
        onChange={onChange}
        hourCycle="h24"
        minuteStep={5}
        locale="en-US"
        isTimeDisabled={(time) => time === "14:35"}
      />,
    );
    await user.click(screen.getByRole("listbox", { name: "Minute" }));
    await user.keyboard("{ArrowDown}");
    expect(onChange).toHaveBeenCalledWith("14:40");
  });

  it("keeps 12 AM and 12 PM on the correct period", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(<TimeWheel value="00:00" onChange={onChange} hourCycle="h12" locale="en-US" />);
    const hour = screen.getByRole("listbox", { name: "Hour" });
    const period = screen.getByRole("listbox", { name: "AM or PM" });
    expect(within(hour).getByRole("option", { name: "12" })).toHaveAttribute("aria-selected", "true");
    expect(within(period).getByRole("option", { name: "AM" })).toHaveAttribute("aria-selected", "true");
    await user.click(within(period).getByRole("option", { name: "PM" }));
    expect(onChange).toHaveBeenCalledWith("12:00");
    rerender(<TimeWheel value="12:00" onChange={onChange} hourCycle="h12" locale="en-US" />);
    expect(within(screen.getByRole("listbox", { name: "AM or PM" })).getByRole("option", { name: "PM" })).toHaveAttribute("aria-selected", "true");
  });
});

function Controlled({ initial = "09:00" }: { initial?: string | null }) {
  const [value, setValue] = useState<string | null>(initial);
  return (
    <>
      <TimePicker label="Start" locale="en-US" hourCycle="h24" viewport="desktop" value={value} onChange={setValue} />
      <output>{value ?? "empty"}</output>
    </>
  );
}

describe("TimePicker", () => {
  it("commits on Apply and restores the previous value on Cancel", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<TimePicker label="Start" locale="en-US" hourCycle="h24" viewport="desktop" defaultValue="09:00" onChange={onChange} />);
    await user.click(screen.getByRole("button", { name: /09:00/ }));
    await user.clear(screen.getByLabelText("Hour"));
    await user.type(screen.getByLabelText("Hour"), "10");
    await user.clear(screen.getByLabelText("Minute"));
    await user.type(screen.getByLabelText("Minute"), "15");
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: /Start, 09:00/ })).toHaveFocus();

    await user.click(screen.getByRole("button", { name: /Start, 09:00/ }));
    await user.clear(screen.getByLabelText("Hour"));
    await user.type(screen.getByLabelText("Hour"), "10");
    await user.clear(screen.getByLabelText("Minute"));
    await user.type(screen.getByLabelText("Minute"), "15");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith("10:15");
    expect(screen.getByRole("button", { name: /Start, 10:15/ })).toBeInTheDocument();
  });

  it("commits a typed time on Apply before the field blurs", () => {
    const onChange = vi.fn();
    render(<TimePicker label="Close" locale="en-US" hourCycle="h24" viewport="desktop" minuteStep={15} onChange={onChange} />);
    fireEvent.click(screen.getByRole("button", { name: /Choose a time/ }));
    fireEvent.change(screen.getByLabelText("Hour"), { target: { value: "18" } });
    fireEvent.change(screen.getByLabelText("Minute"), { target: { value: "45" } });
    fireEvent.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith("18:45");
    expect(screen.getByRole("button", { name: /Close, 18:45/ })).toBeInTheDocument();
  });

  it("blocks an invalid, out-of-range, or disabled time", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <TimePicker
        label="Start"
        locale="en-US"
        hourCycle="h24"
        viewport="desktop"
        minuteStep={15}
        min="09:00"
        max="17:00"
        disabledTimes={["10:00"]}
        onChange={onChange}
      />,
    );
    await user.click(screen.getByRole("button", { name: /Choose a time/ }));
    await user.type(screen.getByLabelText("Hour"), "08");
    await user.type(screen.getByLabelText("Minute"), "00");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent(/or later/i);

    await user.clear(screen.getByLabelText("Hour"));
    await user.type(screen.getByLabelText("Hour"), "10");
    await user.clear(screen.getByLabelText("Minute"));
    await user.type(screen.getByLabelText("Minute"), "00");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(screen.getByRole("alert")).toHaveTextContent(/unavailable/i);
    expect(onChange).not.toHaveBeenCalled();

    await user.clear(screen.getByLabelText("Minute"));
    await user.type(screen.getByLabelText("Minute"), "10");
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(screen.getByText(/steps of 15/i)).toBeInTheDocument();
  });

  it("supports controlled updates, clearing, read-only, and 12-hour entry", async () => {
    const user = userEvent.setup();
    const { rerender } = render(<Controlled />);
    expect(screen.getByRole("button", { name: /Start, 09:00/ })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Clear Start" }));
    expect(screen.getByText("empty")).toBeInTheDocument();

    rerender(
      <TimePicker label="Visit" locale="en-US" hourCycle="h12" viewport="desktop" defaultValue={null} onChange={() => undefined} />,
    );
    await user.click(screen.getByRole("button", { name: /Choose a time/ }));
    await user.type(screen.getByLabelText("Hour"), "12");
    await user.type(screen.getByLabelText("Minute"), "00");
    await user.click(screen.getByRole("radio", { name: "AM" }));
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(screen.getByRole("button", { name: /12:00\sAM/ })).toBeInTheDocument();

    rerender(<TimePicker label="Visit" locale="en-US" hourCycle="h24" viewport="desktop" readOnly value="07:45" />);
    expect(screen.getByRole("button", { name: /07:45/ })).toHaveAttribute("aria-readonly", "true");
    await user.click(screen.getByRole("button", { name: /07:45/ }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("closes from Escape without committing", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<TimePicker label="Start" locale="en-US" hourCycle="h24" viewport="desktop" defaultValue="09:00" onChange={onChange} />);
    await user.click(screen.getByRole("button", { name: /09:00/ }));
    await user.keyboard("{Escape}");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(onChange).not.toHaveBeenCalled();
  });

  it("uses the bottom sheet and wheel on a mobile viewport", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<TimePicker label="Start" locale="en-US" hourCycle="h24" viewport="mobile" minuteStep={5} onChange={onChange} />);
    await user.click(screen.getByRole("button", { name: /Choose a time/ }));
    expect(screen.getByRole("dialog", { name: "Start" })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Type a time" })).toBeInTheDocument();
    await user.click(within(screen.getByRole("listbox", { name: "Hour" })).getByRole("option", { name: "08" }));
    await user.click(within(screen.getByRole("listbox", { name: "Minute" })).getByRole("option", { name: "15" }));
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith("08:15");
  });
});
