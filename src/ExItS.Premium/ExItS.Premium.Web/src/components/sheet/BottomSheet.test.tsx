import { useState } from "react";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { BottomSheet } from "./BottomSheet";

function Harness() {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Open sheet
      </button>
      <BottomSheet open={open} title="Payment" description="Record a payment" onClose={() => setOpen(false)}>
        <button type="button">Save payment</button>
      </BottomSheet>
    </>
  );
}

async function waitClosed() {
  await waitFor(() => {
    const dialog = document.querySelector("dialog");
    if (dialog && (dialog.open || dialog.hasAttribute("open"))) {
      dialog.dispatchEvent(new AnimationEvent("animationend"));
    }
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });
}

describe("BottomSheet", () => {
  it("opens, names itself, and restores focus when closed", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("button", { name: "Open sheet" }));
    const dialog = screen.getByRole("dialog", { name: "Payment" });
    expect(dialog).toHaveAccessibleDescription("Record a payment");
    await user.click(screen.getAllByRole("button", { name: "Close" })[1]);
    await waitClosed();
    expect(screen.getByRole("button", { name: "Open sheet" })).toHaveFocus();
  });

  it("closes from Escape and restores focus", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("button", { name: "Open sheet" }));
    expect(screen.getByRole("dialog", { name: "Payment" })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    await waitClosed();
    expect(screen.getByRole("button", { name: "Open sheet" })).toHaveFocus();
  });

  it("closes from the handle and from a backdrop click outside the sheet", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("button", { name: "Open sheet" }));
    fireEvent.click(screen.getAllByRole("button", { name: "Close" })[0]);
    await waitClosed();

    await user.click(screen.getByRole("button", { name: "Open sheet" }));
    fireEvent.click(screen.getByRole("dialog", { name: "Payment" }), { clientX: -10, clientY: -10 });
    await waitClosed();
    expect(screen.getByRole("button", { name: "Open sheet" })).toHaveFocus();
  });
});
