import type { ReactElement } from "react";
import { MemoryRouter } from "react-router-dom";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ClipboardList, RefreshCw } from "lucide-react";
import { ActionChipBar } from "@/components/exits/ActionChipBar";
import { formatUiStandardsCursorClipboard } from "@/features/ui-standards/UiStandardsCopyCommand";

function renderBar(ui: ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe("ActionChipBar", () => {
  it("renders action items as native buttons without tab semantics", async () => {
    const onSelect = vi.fn();
    const user = userEvent.setup();
    renderBar(
      <ActionChipBar
        ariaLabel="Tools"
        testId="action-chip-bar"
        items={[
          { key: "refresh", label: "Refresh", icon: RefreshCw, onSelect },
          { key: "export", label: "Export", onSelect: () => undefined },
        ]}
      />,
    );

    const bar = screen.getByRole("toolbar", { name: "Tools" });
    expect(bar.querySelector('[role="tablist"]')).toBeNull();
    expect(bar.querySelector('[role="tab"]')).toBeNull();
    expect(bar.querySelector("[aria-selected]")).toBeNull();

    const refresh = screen.getByRole("button", { name: /Refresh/i });
    expect(refresh.tagName).toBe("BUTTON");
    expect(refresh).toHaveAttribute("type", "button");
    await user.click(refresh);
    expect(onSelect).toHaveBeenCalledTimes(1);
  });

  it("renders href items as router links", () => {
    renderBar(
      <ActionChipBar
        ariaLabel="Shortcuts"
        items={[
          {
            key: "stock-count",
            label: "Stock Count",
            icon: ClipboardList,
            href: "/demo/stock-count",
          },
        ]}
      />,
    );

    const link = screen.getByRole("link", { name: /Stock Count/i });
    expect(link).toHaveAttribute("href", "/demo/stock-count");
    expect(link.getAttribute("aria-selected")).toBeNull();
    expect(link.getAttribute("role")).not.toBe("tab");
  });

  it("supports disabled, icons, count hide-zero, and primary emphasis", () => {
    renderBar(
      <ActionChipBar
        ariaLabel="Mixed"
        layout="wrap"
        testId="action-mixed"
        items={[
          {
            key: "expiring",
            label: "Expiring stock",
            icon: ClipboardList,
            href: "/demo/expiring",
            emphasis: "primary",
            count: 4,
            countTone: "warning",
          },
          {
            key: "ready",
            label: "Ready",
            href: "/demo/ready",
            count: 0,
            countZeroMode: "hide",
          },
          {
            key: "export",
            label: "Export",
            onSelect: () => undefined,
            disabled: true,
          },
        ]}
      />,
    );

    expect(screen.getByTestId("action-mixed")).toHaveAttribute("data-layout", "wrap");
    expect(screen.getByRole("link", { name: /Expiring stock/i })).toHaveAttribute(
      "data-emphasis",
      "primary",
    );
    expect(within(screen.getByRole("link", { name: /Expiring stock/i })).getByText("4")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /^Ready$/i }).textContent).not.toMatch(/0/);
    expect(screen.getByRole("button", { name: /Export/i })).toBeDisabled();
  });

  it("supports scroll, grid, and responsive-auto layouts", () => {
    const { rerender } = renderBar(
      <ActionChipBar
        ariaLabel="Layouts"
        testId="action-layouts"
        layout="scroll"
        items={[
          { key: "a", label: "A", onSelect: () => undefined },
          { key: "b", label: "B", onSelect: () => undefined },
        ]}
      />,
    );
    expect(screen.getByTestId("action-layouts")).toHaveAttribute("data-layout", "scroll");
    expect(screen.getByTestId("action-layouts").innerHTML).toMatch(/overflow-x-auto/);

    rerender(
      <MemoryRouter>
        <ActionChipBar
          ariaLabel="Layouts"
          testId="action-layouts"
          layout="grid"
          items={[
            { key: "a", label: "A", onSelect: () => undefined },
            { key: "b", label: "B", onSelect: () => undefined },
          ]}
        />
      </MemoryRouter>,
    );
    expect(screen.getByTestId("action-layouts")).toHaveAttribute("data-layout", "grid");
    expect(screen.getByTestId("action-layouts").innerHTML).toMatch(/grid/);

    rerender(
      <MemoryRouter>
        <ActionChipBar
          ariaLabel="Layouts"
          testId="action-layouts"
          layout="responsiveAuto"
          items={[
            { key: "a", label: "A", onSelect: () => undefined },
            { key: "b", label: "B", onSelect: () => undefined },
          ]}
        />
      </MemoryRouter>,
    );
    expect(screen.getByTestId("action-layouts")).toHaveAttribute("data-layout", "responsiveAuto");
  });

  it("uses logical margin utilities for trailing utilities (RTL-safe)", () => {
    renderBar(
      <ActionChipBar
        ariaLabel="Trailing"
        testId="action-trailing"
        items={[{ key: "a", label: "Stock Count", onSelect: () => undefined }]}
        trailingItems={[
          {
            key: "refresh",
            label: "Refresh",
            icon: RefreshCw,
            iconOnly: true,
            ariaLabel: "Refresh",
            onSelect: () => undefined,
          },
        ]}
      />,
    );
    expect(screen.getByTestId("action-trailing-trailing").className).toMatch(/\bms-auto\b/);
    expect(screen.getByTestId("action-trailing-trailing").className).not.toMatch(/\bml-auto\b/);
  });
});

describe("Action Chip pilot clipboard", () => {
  it("does not claim locked for pilot status", () => {
    const text = formatUiStandardsCursorClipboard(
      "Action Chip",
      "ACTION CHIP GROUP + RESPONSIVE AUTO + WITH ICON",
      "Use lightweight Inventory workflow shortcuts. Keep navigation items as links and direct actions as buttons.",
      "pilot",
    );
    expect(text).toContain("Use the approved ExItS Action Chip pilot.");
    expect(text).not.toContain("locked ExItS Action Chip");
    expect(text).toContain("Apply: ACTION CHIP GROUP + RESPONSIVE AUTO + WITH ICON.");
    expect(text).toContain("Preserve existing");
  });
});
