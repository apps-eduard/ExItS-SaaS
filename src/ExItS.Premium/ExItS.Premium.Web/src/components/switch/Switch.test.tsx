import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { Switch } from "./Switch";

function Example() {
  const [on, setOn] = useState(false);
  return <Switch label="Reminders" checked={on} onChange={(event) => setOn(event.target.checked)} />;
}

describe("Switch", () => {
  it("is a switch and toggles from the keyboard", async () => {
    const user = userEvent.setup();
    render(<Example />);
    const control = screen.getByRole("switch", { name: "Reminders" });
    expect(control).not.toBeChecked();
    control.focus();
    await user.keyboard(" ");
    expect(control).toBeChecked();
  });

  it("does not toggle when disabled", async () => {
    const user = userEvent.setup();
    render(<Switch label="Locked" disabled />);
    const control = screen.getByRole("switch", { name: "Locked" });
    expect(control).toBeDisabled();
    await user.click(control);
    expect(control).not.toBeChecked();
  });
});
