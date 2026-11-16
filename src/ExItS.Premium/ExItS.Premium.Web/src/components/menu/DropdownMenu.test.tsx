import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ActionMenu, DropdownMenu } from "./DropdownMenu";

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

  it("jumps with Home and End and chooses once", async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    render(<DropdownMenu label="Move money" items={items} onSelect={onSelect} />);
    await user.click(screen.getByRole("button", { name: "Move money" }));
    await user.keyboard("{End}{Enter}");
    expect(onSelect).toHaveBeenCalledTimes(1);
    expect(onSelect).toHaveBeenCalledWith("send");
  });

  it("closes on outside click and on Tab", async () => {
    const user = userEvent.setup();
    render(
      <>
        <DropdownMenu label="Move money" items={items} onSelect={vi.fn()} />
        <button type="button">After</button>
      </>,
    );
    const trigger = screen.getByRole("button", { name: "Move money" });
    await user.click(trigger);
    expect(screen.getByRole("menu", { name: "Move money" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "After" }));
    expect(screen.queryByRole("menu", { name: "Move money" })).not.toBeInTheDocument();

    await user.click(trigger);
    await user.tab();
    expect(screen.queryByRole("menu", { name: "Move money" })).not.toBeInTheDocument();
  });

  it("closes the previous menu when another opens and leaves its trigger unfocused", async () => {
    const user = userEvent.setup();
    const onFirst = vi.fn();
    const onSecond = vi.fn();
    render(
      <>
        <DropdownMenu label="Menu A" items={items} onSelect={onFirst} />
        <ActionMenu label="Menu B" items={items} onSelect={onSecond} />
      </>,
    );
    const first = screen.getByRole("button", { name: "Menu A" });
    await user.click(first);
    expect(screen.getByRole("menu", { name: "Menu A" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Menu B" }));
    expect(screen.queryByRole("menu", { name: "Menu A" })).not.toBeInTheDocument();
    expect(screen.getByRole("menu", { name: "Menu B" })).toBeInTheDocument();
    expect(first).not.toHaveFocus();
    expect(screen.getByRole("menuitem", { name: "Add" })).toHaveFocus();
    await user.keyboard("{Enter}");
    expect(onSecond).toHaveBeenCalledTimes(1);
    expect(onSecond).toHaveBeenCalledWith("add");
    expect(onFirst).not.toHaveBeenCalled();
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
