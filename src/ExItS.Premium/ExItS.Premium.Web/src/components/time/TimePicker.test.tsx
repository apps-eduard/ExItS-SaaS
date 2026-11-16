import { useState } from "react";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { TimePicker } from "./TimePicker";
import timeCss from "./TimePicker.module.scss?raw";
import { TimeWheel, WHEEL_ITEM_PX } from "./TimeWheel";
import { formatTimeDisplay } from "./time";

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
    await user.click(within(screen.getByRole("listbox", { name: "Hour" })).getByRole("option", { name: "10" }));
    await user.click(within(screen.getByRole("listbox", { name: "Minute" })).getByRole("option", { name: "15" }));
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: /Start, 09:00/ })).toHaveFocus();

    await user.click(screen.getByRole("button", { name: /Start, 09:00/ }));
    await user.click(within(screen.getByRole("listbox", { name: "Hour" })).getByRole("option", { name: "10" }));
    await user.click(within(screen.getByRole("listbox", { name: "Minute" })).getByRole("option", { name: "15" }));
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith("10:15");
    expect(screen.getByRole("button", { name: /Start, 10:15/ })).toBeInTheDocument();
  });

  it("lists every minute and sets Now beside Apply without committing", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <TimePicker
        label="Start"
        locale="en-US"
        hourCycle="h24"
        viewport="desktop"
        onChange={onChange}
        now={() => new Date(2026, 10, 15, 14, 7, 40)}
      />,
    );
    await user.click(screen.getByRole("button", { name: /Choose a time/ }));
    const minutes = within(screen.getByRole("listbox", { name: "Minute" })).getAllByRole("option");
    expect(minutes).toHaveLength(60);
    expect(minutes[0]).toHaveTextContent("00");
    expect(minutes[59]).toHaveTextContent("59");
    await user.click(screen.getByRole("button", { name: "Now" }));
    expect(screen.getAllByText("14:07").length).toBeGreaterThanOrEqual(2);
    expect(screen.getByRole("button", { name: /Start, 14:07/ })).toBeInTheDocument();
    expect(onChange).not.toHaveBeenCalled();
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith("14:07");
  });

  it("does not show an error just because the desktop columns open", async () => {
    const user = userEvent.setup();
    render(<TimePicker label="Start" locale="en-US" hourCycle="h24" viewport="desktop" />);
    await user.click(screen.getByRole("button", { name: /Choose a time/ }));
    expect(screen.getByRole("listbox", { name: "Hour" })).toBeInTheDocument();
    expect(screen.queryByText(/Arrow keys step/)).not.toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("commits a pasted time on Apply", () => {
    const onChange = vi.fn();
    render(<TimePicker label="Close" locale="en-US" hourCycle="h24" viewport="desktop" minuteStep={15} onChange={onChange} />);
    fireEvent.click(screen.getByRole("button", { name: /Choose a time/ }));
    fireEvent.paste(screen.getByRole("listbox", { name: "Hour" }), { clipboardData: { getData: () => "18:45" } });
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
    fireEvent.paste(screen.getByRole("listbox", { name: "Hour" }), { clipboardData: { getData: () => "08:00" } });
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("alert")).toHaveTextContent(/or later/i);

    fireEvent.paste(screen.getByRole("listbox", { name: "Hour" }), { clipboardData: { getData: () => "10:00" } });
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(screen.getByRole("alert")).toHaveTextContent(/unavailable/i);
    expect(onChange).not.toHaveBeenCalled();

    const minute = screen.getByRole("listbox", { name: "Minute" });
    fireEvent.keyDown(minute, { key: "1" });
    fireEvent.keyDown(minute, { key: "0" });
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
    await user.click(within(screen.getByRole("listbox", { name: "Hour" })).getByRole("option", { name: "12" }));
    await user.click(within(screen.getByRole("listbox", { name: "Minute" })).getByRole("option", { name: "00" }));
    await user.click(within(screen.getByRole("listbox", { name: "AM or PM" })).getByRole("option", { name: "AM" }));
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(screen.getByRole("button", { name: /12:00\sAM/ })).toBeInTheDocument();

    rerender(<TimePicker label="Visit" locale="en-US" hourCycle="h24" viewport="desktop" readOnly value="07:45" />);
    expect(screen.getByRole("button", { name: /07:45/ })).toHaveAttribute("aria-readonly", "true");
    await user.click(screen.getByRole("button", { name: /07:45/ }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("previews a scrolled time in the field and header until Apply", () => {
    const onChange = vi.fn();
    render(<TimePicker label="Start" name="start" locale="en-US" hourCycle="h24" viewport="desktop" defaultValue="09:00" onChange={onChange} />);
    fireEvent.click(screen.getByRole("button", { name: /Start, 09:00/ }));
    const hour = screen.getByRole("listbox", { name: "Hour" });
    hour.scrollTop = WHEEL_ITEM_PX * 11;
    fireEvent.scroll(hour);
    const preview = formatTimeDisplay("11:00", "h24", "en-US") ?? "11:00";
    expect(screen.getByRole("button", { name: `Start, ${preview}` })).toBeInTheDocument();
    expect(screen.getAllByText(preview).length).toBeGreaterThanOrEqual(2);
    expect(document.querySelector("input[name='start']")).toHaveValue("09:00");
    expect(onChange).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    expect(screen.getByRole("button", { name: /Start, 09:00/ })).toBeInTheDocument();
    expect(onChange).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: /Start, 09:00/ }));
    const hourAgain = screen.getByRole("listbox", { name: "Hour" });
    hourAgain.scrollTop = WHEEL_ITEM_PX * 11;
    fireEvent.scroll(hourAgain);
    fireEvent.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledTimes(1);
    expect(onChange).toHaveBeenCalledWith("11:00");
    expect(document.querySelector("input[name='start']")).toHaveValue("11:00");
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
    await user.type(screen.getByLabelText("Hour typed"), "9");
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Enter an hour and minute.");
    await user.type(screen.getByLabelText("Minute typed"), "15");
    await user.click(within(screen.getByRole("listbox", { name: "Hour" })).getByRole("option", { name: "08" }));
    await user.click(within(screen.getByRole("listbox", { name: "Minute" })).getByRole("option", { name: "15" }));
    await user.click(screen.getByRole("button", { name: "Apply" }));
    expect(onChange).toHaveBeenCalledWith("08:15");
  });

  it("keeps footer depth on the Time Picker actions only", async () => {
    const user = userEvent.setup();
    render(<TimePicker label="Start" locale="en-US" hourCycle="h12" viewport="desktop" defaultValue="09:30" clearable />);
    await user.click(screen.getByRole("button", { name: /9:30 AM/ }));
    expect(screen.getByRole("button", { name: "Clear" })).toHaveAttribute("data-appearance", "outline");
    expect(screen.getByRole("button", { name: "Cancel" })).toHaveAttribute("data-appearance", "outline");
    expect(screen.getByRole("button", { name: "Now" })).toHaveAttribute("data-appearance", "soft");
    expect(screen.getByRole("button", { name: "Apply" })).toHaveAttribute("data-appearance", "solid");
    expect(timeCss).toContain('.actions .action[data-appearance="outline"]');
    expect(timeCss).toContain('.actions .action[data-appearance="soft"]');
    expect(timeCss).not.toContain('data-appearance="gradient"');
    expect(timeCss).not.toContain('data-appearance="solid"');
    expect(timeCss).not.toContain("backdrop-filter");
  });
});
