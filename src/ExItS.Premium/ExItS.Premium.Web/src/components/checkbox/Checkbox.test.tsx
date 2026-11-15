import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { Checkbox } from "./Checkbox";

function Example() {
  const [checked, setChecked] = useState(false);
  return <Checkbox label="Reminders" checked={checked} onChange={(event) => setChecked(event.target.checked)} />;
}

describe("Checkbox", () => {
  it("toggles from the label and exposes the label as its name", async () => {
    const user = userEvent.setup();
    render(<Example />);
    const box = screen.getByRole("checkbox", { name: "Reminders" });
    expect(box).not.toBeChecked();
    await user.click(box);
    expect(box).toBeChecked();
  });

  it("does not toggle when disabled", async () => {
    const user = userEvent.setup();
    render(<Checkbox label="Locked" disabled />);
    const box = screen.getByRole("checkbox", { name: "Locked" });
    expect(box).toBeDisabled();
    await user.click(box);
    expect(box).not.toBeChecked();
  });

  it("sets indeterminate only while the box is unchecked", () => {
    const { rerender } = render(<Checkbox label="All" indeterminate />);
    const box = screen.getByRole("checkbox", { name: "All" });
    expect(box).toBePartiallyChecked();
    rerender(<Checkbox label="All" indeterminate checked />);
    expect(box).not.toBePartiallyChecked();
    expect(box).toBeChecked();
  });
});
