import { useState } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MotionConfig } from "motion/react";
import { describe, expect, it } from "vitest";
import { Dialog } from "./Dialog";

function wrap(ui: React.ReactNode) {
  return <MotionConfig reducedMotion="always">{ui}</MotionConfig>;
}

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
    render(wrap(<Harness />));
    await user.click(screen.getByRole("button", { name: "Open note" }));
    const dialog = screen.getByRole("dialog", { name: "Add a note" });
    expect(dialog).toHaveAccessibleDescription("Optional note");
    expect(dialog).toHaveAttribute("data-motion", "desktop");
    await user.click(screen.getByRole("button", { name: "Close" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(screen.getByRole("button", { name: "Open note" })).toHaveFocus();
  });

  it("keeps Tab and Shift+Tab inside the dialog", async () => {
    const user = userEvent.setup();
    render(wrap(<Harness />));
    await user.click(screen.getByRole("button", { name: "Open note" }));
    const close = screen.getByRole("button", { name: "Close" });
    const save = screen.getByRole("button", { name: "Save note" });
    expect(close).toHaveFocus();
    await user.tab();
    expect(save).toHaveFocus();
    await user.tab();
    expect(close).toHaveFocus();
    await user.tab({ shift: true });
    expect(save).toHaveFocus();
    expect(screen.getByRole("button", { name: "Open note" })).not.toHaveFocus();
  });

  it("closes from Escape", async () => {
    const user = userEvent.setup();
    render(wrap(<Harness />));
    await user.click(screen.getByRole("button", { name: "Open note" }));
    await user.keyboard("{Escape}");
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  });

  it("keeps Escape from dismissing when dismissible is false", async () => {
    const user = userEvent.setup();
    function Persistent() {
      const [open, setOpen] = useState(true);
      return (
        <Dialog open={open} title="Finish setup" dismissible={false} onClose={() => setOpen(false)}>
          <button type="button">Done</button>
        </Dialog>
      );
    }
    render(wrap(<Persistent />));
    expect(screen.getByRole("dialog", { name: "Finish setup" })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(screen.getByRole("dialog", { name: "Finish setup" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Close" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
  });

  it("renders an optional header icon without changing the accessible name", async () => {
    const user = userEvent.setup();
    function WithIcon() {
      const [open, setOpen] = useState(false);
      return (
        <>
          <button type="button" onClick={() => setOpen(true)}>
            Open note
          </button>
          <Dialog open={open} title="Add a note" icon="edit_note" iconTone="primary" onClose={() => setOpen(false)}>
            <button type="button">Save note</button>
          </Dialog>
        </>
      );
    }
    render(wrap(<WithIcon />));
    await user.click(screen.getByRole("button", { name: "Open note" }));
    expect(screen.getByRole("dialog", { name: "Add a note" })).toHaveAttribute("data-mobile", "center");
  });

  it("uses sheet motion under 640px when mobilePresentation is sheet", async () => {
    const user = userEvent.setup();
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      configurable: true,
      value: (query: string) => ({
        matches: query.includes("max-width: 639px"),
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });

    function SheetHarness() {
      const [open, setOpen] = useState(false);
      return (
        <>
          <button type="button" onClick={() => setOpen(true)}>
            Open sheet
          </button>
          <Dialog
            open={open}
            title="Add a note"
            mobilePresentation="sheet"
            onClose={() => setOpen(false)}
          >
            <button type="button">Save note</button>
          </Dialog>
        </>
      );
    }

    render(wrap(<SheetHarness />));
    await user.click(screen.getByRole("button", { name: "Open sheet" }));
    const dialog = screen.getByRole("dialog", { name: "Add a note" });
    expect(dialog).toHaveAttribute("data-mobile", "sheet");
    expect(dialog).toHaveAttribute("data-motion", "sheet");
  });

  it("exposes divider, gray-footer, and transition chrome", async () => {
    const user = userEvent.setup();
    function ChromeHarness() {
      const [open, setOpen] = useState(false);
      return (
        <>
          <button type="button" onClick={() => setOpen(true)}>
            Open note
          </button>
          <Dialog
            open={open}
            title="Add a note"
            transition="fade"
            lineTop={false}
            lineBottom={false}
            grayFooter={false}
            footer={<button type="button">Save</button>}
            onClose={() => setOpen(false)}
          >
            <button type="button">Save note</button>
          </Dialog>
        </>
      );
    }
    render(wrap(<ChromeHarness />));
    await user.click(screen.getByRole("button", { name: "Open note" }));
    const dialog = screen.getByRole("dialog", { name: "Add a note" });
    expect(dialog).toHaveAttribute("data-transition", "fade");
    expect(dialog).toHaveAttribute("data-line-top", "false");
    expect(dialog).toHaveAttribute("data-line-bottom", "false");
    expect(dialog).toHaveAttribute("data-gray-footer", "false");
  });
});
