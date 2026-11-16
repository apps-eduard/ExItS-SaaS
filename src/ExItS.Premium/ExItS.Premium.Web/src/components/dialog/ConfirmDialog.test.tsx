import { useState } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MotionConfig } from "motion/react";
import { describe, expect, it, vi } from "vitest";
import { ConfirmDialog } from "./ConfirmDialog";

function wrap(ui: React.ReactNode) {
  return <MotionConfig reducedMotion="always">{ui}</MotionConfig>;
}

describe("ConfirmDialog", () => {
  it("runs the danger action only after confirmation", async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();

    function Harness() {
      const [open, setOpen] = useState(false);
      return (
        <>
          <button type="button" onClick={() => setOpen(true)}>
            Delete transaction
          </button>
          <ConfirmDialog
            open={open}
            title="Delete this transaction?"
            description="This removes the sample."
            confirmLabel="Delete"
            tone="danger"
            onConfirm={() => {
              onConfirm();
              setOpen(false);
            }}
            onClose={() => setOpen(false)}
          />
        </>
      );
    }

    render(wrap(<Harness />));
    await user.click(screen.getByRole("button", { name: "Delete transaction" }));
    const dialog = screen.getByRole("dialog", { name: "Delete this transaction?" });
    expect(dialog).toHaveAccessibleDescription("This removes the sample.");
    expect(onConfirm).not.toHaveBeenCalled();
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(onConfirm).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: "Delete transaction" }));
    await user.click(screen.getByRole("button", { name: "Delete" }));
    expect(onConfirm).toHaveBeenCalledTimes(1);
  });
});
