import { useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { Dialog } from "./Dialog";

function Harness() {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Open note
      </button>
      <Dialog open={open} title="Add a note" description="Optional note" onClose={() => setOpen(false)}>
        <button type="button">Save note</button>
      </Dialog>
    </>
  );
}

describe("Dialog", () => {
  it("opens, labels itself, and restores focus when closed", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("button", { name: "Open note" }));
    const dialog = screen.getByRole("dialog", { name: "Add a note" });
    expect(dialog).toHaveAccessibleDescription("Optional note");
    await user.click(screen.getByRole("button", { name: "Close" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Open note" })).toHaveFocus();
  });

  it("closes from Escape", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("button", { name: "Open note" }));
    await user.keyboard("{Escape}");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });
});
