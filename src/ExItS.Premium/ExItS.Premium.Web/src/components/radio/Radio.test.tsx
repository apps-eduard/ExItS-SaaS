import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { Radio } from "./Radio";

function Group() {
  const [value, setValue] = useState("soft");
  return (
    <fieldset>
      <legend>Shape</legend>
      <Radio label="Soft" name="shape" value="soft" checked={value === "soft"} onChange={() => setValue("soft")} />
      <Radio label="Pill" name="shape" value="pill" checked={value === "pill"} onChange={() => setValue("pill")} />
    </fieldset>
  );
}

describe("Radio", () => {
  it("selects one option in the group", async () => {
    const user = userEvent.setup();
    render(<Group />);
    const soft = screen.getByRole("radio", { name: "Soft" });
    const pill = screen.getByRole("radio", { name: "Pill" });
    expect(soft).toBeChecked();
    await user.click(pill);
    expect(pill).toBeChecked();
    expect(soft).not.toBeChecked();
  });

  it("does not select a disabled option", async () => {
    const user = userEvent.setup();
    render(<Radio label="Locked" name="shape" value="locked" disabled />);
    const locked = screen.getByRole("radio", { name: "Locked" });
    expect(locked).toBeDisabled();
    await user.click(locked);
    expect(locked).not.toBeChecked();
  });
});
