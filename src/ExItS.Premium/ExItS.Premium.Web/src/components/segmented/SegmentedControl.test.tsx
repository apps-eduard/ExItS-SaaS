import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import segmentedCss from "./SegmentedControl.module.scss?raw";
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

  it("keeps the visual option height and widens only the mobile hit area to 44px within each segment", () => {
    expect(segmentedCss).toContain("min-height: calc(var(--control-height) - 6px)");
    expect(segmentedCss).toMatch(
      /@media \(max-width: 767px\)[\s\S]*\.option input\s*\{[^}]*left:\s*0;[^}]*height:\s*max\(44px, 100%\);[^}]*transform:\s*translateY\(-50%\);/,
    );
  });
});
