import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DropdownMenu } from "./DropdownMenu";

const items = [
  { id: "add", label: "Add" },
  { id: "send", label: "Send" },
  { id: "request", label: "Request", disabled: true },
];

describe("DropdownMenu", () => {
  it("moves with the arrow keys and skips a disabled item", async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    render(<DropdownMenu label="Move money" items={items} onSelect={onSelect} />);
    await user.click(screen.getByRole("button", { name: "Move money" }));
    expect(screen.getByRole("menuitem", { name: "Request" })).toBeDisabled();
    await user.keyboard("{ArrowDown}{Enter}");
    expect(onSelect).toHaveBeenCalledWith("send");
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
  });

  it("returns focus to the trigger on Escape", async () => {
    const user = userEvent.setup();
    render(<DropdownMenu label="Move money" items={items} onSelect={vi.fn()} />);
    const trigger = screen.getByRole("button", { name: "Move money" });
    await user.click(trigger);
    await user.keyboard("{Escape}");
    expect(trigger).toHaveFocus();
  });
});
