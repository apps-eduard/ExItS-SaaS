import { createElement } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { CountBadge, CountChip } from "@/components/exits/CountChip";
import { FilterChip } from "@/components/exits/FilterChip";
import { RemovableChip } from "@/components/exits/RemovableChip";
import { StatusChip } from "@/components/exits/StatusChip";
import { TagChip } from "@/components/exits/TagChip";
import { UI_STANDARDS_DEFAULT_OPEN } from "@/features/ui-standards/ui-standards-disclosure";

describe("ExItS chip visual pilot primitives", () => {
  it("keeps StatusChip API compatible including primary tone", () => {
    for (const tone of ["info", "success", "warning", "danger", "neutral", "primary"] as const) {
      const { unmount } = render(createElement(StatusChip, { tone, children: tone }));
      const el = screen.getByText(tone);
      expect(el.className).toContain("exits-status-chip");
      expect(el.className).toContain(`exits-status-chip--${tone}`);
      expect(el.tagName).toBe("SPAN");
      unmount();
    }
  });

  it("FilterChip toggles selection with button semantics and disabled state", async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    const { rerender } = render(
      createElement(FilterChip, { selected: false, onClick, children: "Active" }),
    );
    const btn = screen.getByRole("button", { name: "Active" });
    expect(btn).toHaveAttribute("aria-pressed", "false");
    await user.click(btn);
    expect(onClick).toHaveBeenCalledTimes(1);

    rerender(createElement(FilterChip, { selected: true, showCheck: true, children: "Active" }));
    expect(screen.getByRole("button", { name: "Active" })).toHaveAttribute("aria-pressed", "true");

    rerender(createElement(FilterChip, { disabled: true, children: "Inactive" }));
    expect(screen.getByRole("button", { name: "Inactive" })).toBeDisabled();
  });

  it("TagChip and CountChip render tones and layouts", () => {
    const { unmount } = render(createElement(TagChip, { tone: "info", children: "B2B" }));
    expect(screen.getByText("B2B")).toBeInTheDocument();
    unmount();

    render(
      createElement(CountChip, {
        layout: "inline",
        tone: "danger",
        label: "Overdue",
        count: 5,
      }),
    );
    expect(screen.getByText("Overdue")).toBeInTheDocument();
    expect(screen.getByText("5")).toBeInTheDocument();

    render(
      createElement(CountChip, {
        layout: "split",
        tone: "warning",
        label: "Low stock",
        count: 12,
      }),
    );
    expect(screen.getByText("Low stock")).toBeInTheDocument();
    expect(screen.getByText("12")).toBeInTheDocument();

    render(createElement(CountBadge, { tone: "primary", count: "99+" }));
    expect(screen.getByText("99+")).toBeInTheDocument();
  });

  it("RemovableChip fires accessible remove action", async () => {
    const user = userEvent.setup();
    const onRemove = vi.fn();
    render(
      createElement(RemovableChip, {
        removeLabel: "Remove Branch: Main filter",
        onRemove,
        children: "Branch: Main",
      }),
    );
    await user.click(screen.getByRole("button", { name: "Remove Branch: Main filter" }));
    expect(onRemove).toHaveBeenCalledTimes(1);
  });

  it("UI Standards chip disclosure defaults match pilot open/closed map", () => {
    expect(UI_STANDARDS_DEFAULT_OPEN["chips.status"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["chips.filter"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["chips.tags"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["chips.status-icons"]).toBe(false);
    expect(UI_STANDARDS_DEFAULT_OPEN["chips.cheatsheet"]).toBe(false);
  });
});
