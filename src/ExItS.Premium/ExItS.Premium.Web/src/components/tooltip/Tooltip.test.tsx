import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { Tooltip } from "./Tooltip";

function Example() {
  return (
    <Tooltip content="Saves the record">
      <button type="button">Save</button>
    </Tooltip>
  );
}

describe("Tooltip", () => {
  it("describes the focused control and hides on Escape", async () => {
    const user = userEvent.setup();
    render(<Example />);
    const save = screen.getByRole("button", { name: "Save" });
    save.focus();
    const tip = await screen.findByRole("tooltip");
    expect(tip).toHaveTextContent("Saves the record");
    expect(save.parentElement).toHaveAttribute("aria-describedby", tip.id);
    await user.keyboard("{Escape}");
    expect(save.parentElement).not.toHaveAttribute("aria-describedby");
  });
});
