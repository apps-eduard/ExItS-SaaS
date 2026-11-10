import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { SegmentedControl } from "./SegmentedControl";

function Example() {
  const [value, setValue] = useState<"soft" | "pill">("soft");
  return (
    <SegmentedControl
      label="Shape"
      name="shape"
      value={value}
      onChange={setValue}
      options={[
        { value: "soft", label: "Soft" },
        { value: "pill", label: "Pill" },
      ]}
    />
  );
}

describe("SegmentedControl", () => {
  it("selects another option from the radio group", async () => {
    const user = userEvent.setup();
    render(<Example />);
    const pill = screen.getByRole("radio", { name: "Pill" });
    expect(screen.getByRole("radio", { name: "Soft" })).toBeChecked();
    await user.click(pill);
    expect(pill).toBeChecked();
  });

  it("does not select a disabled option", async () => {
    const user = userEvent.setup();
    render(
      <SegmentedControl
        label="Range"
        name="range"
        value="day"
        onChange={() => undefined}
        options={[
          { value: "day", label: "Day" },
          { value: "week", label: "Week", disabled: true },
        ]}
      />,
    );
    expect(screen.getByRole("radio", { name: "Week" })).toBeDisabled();
    await user.click(screen.getByRole("radio", { name: "Week" }));
    expect(screen.getByRole("radio", { name: "Day" })).toBeChecked();
  });
});
