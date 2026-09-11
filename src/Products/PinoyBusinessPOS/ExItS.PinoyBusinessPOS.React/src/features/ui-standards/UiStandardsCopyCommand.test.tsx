import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  formatUiStandardsCursorClipboard,
  UiStandardsCopyCommand,
  uiStandardsClipboardWriter,
} from "@/features/ui-standards/UiStandardsCopyCommand";

describe("formatUiStandardsCursorClipboard", () => {
  it("builds the safe full Cursor prompt with locked standard name", () => {
    expect(formatUiStandardsCursorClipboard("Tabs", "PILL BAR TABS + EQUAL WIDTH + WITH COUNT")).toBe(
      [
        "Use the locked ExItS Tabs Standard.",
        "Apply: PILL BAR TABS + EQUAL WIDTH + WITH COUNT.",
        "Preserve existing business behavior, domain rules, permissions, data flow, and API behavior unless explicitly instructed otherwise.",
      ].join("\n"),
    );
  });

  it("includes optional context between Apply and Preserve lines", () => {
    const text = formatUiStandardsCursorClipboard("Button", "MUTED + WITH ICON", "ICON: CircleX");
    expect(text).toContain("ICON: CircleX");
    expect(text.indexOf("Apply:")).toBeLessThan(text.indexOf("ICON: CircleX"));
    expect(text.indexOf("ICON: CircleX")).toBeLessThan(text.indexOf("Preserve existing"));
  });
});

describe("UiStandardsCopyCommand", () => {
  let writeSpy: ReturnType<typeof vi.spyOn<typeof uiStandardsClipboardWriter, "write">>;

  beforeEach(() => {
    writeSpy = vi.spyOn(uiStandardsClipboardWriter, "write").mockResolvedValue(undefined);
  });

  afterEach(() => {
    writeSpy.mockRestore();
    vi.useRealTimers();
  });

  it("shows compact shorthand and copies the full safe prompt", async () => {
    const user = userEvent.setup();
    render(
      <UiStandardsCopyCommand
        standard="Tabs"
        command="PILL BAR TABS + EQUAL WIDTH + WITH COUNT + SOLID PRIMARY ACTIVE"
      />,
    );

    expect(screen.getByText("Cursor")).toBeInTheDocument();
    expect(
      screen.getByText("PILL BAR TABS + EQUAL WIDTH + WITH COUNT + SOLID PRIMARY ACTIVE"),
    ).toBeInTheDocument();
    expect(screen.queryByText(/Preserve existing business behavior/i)).not.toBeInTheDocument();

    const copyBtn = screen.getByRole("button", { name: "Copy Cursor command" });
    await user.click(copyBtn);

    expect(writeSpy).toHaveBeenCalledWith(
      formatUiStandardsCursorClipboard(
        "Tabs",
        "PILL BAR TABS + EQUAL WIDTH + WITH COUNT + SOLID PRIMARY ACTIVE",
      ),
    );
    expect(copyBtn).toHaveAttribute("data-copy-status", "copied");

    await waitFor(
      () => {
        expect(copyBtn).toHaveAttribute("data-copy-status", "idle");
      },
      { timeout: 2500 },
    );
  });

  it("expands full command on demand and stops click propagation", async () => {
    const user = userEvent.setup();
    const parentClick = vi.fn();
    render(
      <div onClick={parentClick}>
        <UiStandardsCopyCommand standard="Table" command="FULL TABLE" />
      </div>,
    );

    await user.click(screen.getByRole("button", { name: "View full Cursor command" }));
    expect(screen.getByText(/Use the locked ExItS Table Standard\./)).toBeInTheDocument();
    expect(parentClick).not.toHaveBeenCalled();
  });

  it("shows a local failure state when clipboard write fails", async () => {
    writeSpy.mockRejectedValueOnce(new Error("denied"));
    const user = userEvent.setup();
    render(<UiStandardsCopyCommand standard="Chip" command="STATUS CHIP SUCCESS" />);

    const copyBtn = screen.getByRole("button", { name: "Copy Cursor command" });
    await user.click(copyBtn);

    expect(copyBtn).toHaveAttribute("data-copy-status", "failed");
    expect(screen.getByText(/Could not copy/i)).toBeInTheDocument();
  });

  it("covers representative locked vocabulary for each standard", () => {
    const samples: Array<[string, string]> = [
      ["Table", "FULL TABLE + ACTIONS ON + INLINE EDIT ON"],
      ["Button", "PRIMARY + SOFT + WITH ICON"],
      ["Chip", "FILTER CHIP PRIMARY SELECTED"],
      ["Tabs", "PILL BAR TABS + EQUAL WIDTH + WITH COUNT + SOLID PRIMARY ACTIVE"],
      ["Card", "ENTITY CARD + WITH CHIP + WITH ACTIONS"],
    ];
    for (const [standard, command] of samples) {
      const text = formatUiStandardsCursorClipboard(standard, command);
      expect(text.startsWith(`Use the locked ExItS ${standard} Standard.`)).toBe(true);
      expect(text).toContain(`Apply: ${command}.`);
      expect(text).not.toMatch(/bg-|rounded-full|#[0-9a-fA-F]{3,8}|\bgreen\b|\bred\b|\borange\b/);
    }
  });
});
