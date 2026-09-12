import { describe, expect, it, vi } from "vitest";
import { createElement } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CountBadge } from "@/components/exits/CountChip";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { FilterChip } from "@/components/exits/FilterChip";

describe("CountBadge filter embedding", () => {
  it("ExitsChipBar renders CountBadge for 0 and omits when count is undefined", () => {
    const { rerender } = render(
      createElement(ExitsChipBar, {
        ariaLabel: "Kinds",
        testId: "kind-filters",
        variant: "filter",
        items: [
          { key: "all", label: "All", count: 1, state: "active", testId: "kind-all", onSelect: () => undefined },
          { key: "people", label: "People", count: 0, state: "idle", testId: "kind-people", onSelect: () => undefined },
          {
            key: "businesses",
            label: "Businesses",
            count: 1,
            state: "idle",
            testId: "kind-businesses",
            onSelect: () => undefined,
          },
        ],
      }),
    );

    const people = screen.getByTestId("kind-people");
    expect(people).toHaveAccessibleName("People, 0");
    expect(within(people).getByText("People")).toBeInTheDocument();
    expect(within(people).getByText("0")).toBeInTheDocument();
    expect(within(people).getByText("0").closest("[data-tone]")).toHaveAttribute("data-tone", "neutral");
    expect(within(people).getByText("0").closest("[data-shape]")).toHaveAttribute("data-shape", "pill");

    const all = screen.getByTestId("kind-all");
    expect(all).toHaveAccessibleName("All, 1");
    expect(within(all).getByText("1").closest("[data-tone]")).toHaveAttribute("data-tone", "primary");

    rerender(
      createElement(ExitsChipBar, {
        ariaLabel: "Kinds",
        testId: "kind-filters",
        variant: "filter",
        items: [
          { key: "all", label: "All", state: "active", testId: "kind-all", onSelect: () => undefined },
          { key: "people", label: "People", count: undefined, state: "idle", testId: "kind-people", onSelect: () => undefined },
        ],
      }),
    );
    expect(screen.getByTestId("kind-all")).not.toHaveAccessibleName("All, 0");
    expect(within(screen.getByTestId("kind-all")).queryByText("0")).not.toBeInTheDocument();
    expect(within(screen.getByTestId("kind-people")).queryByText("0")).not.toBeInTheDocument();
  });

  it("FilterChip keeps selection and embeds CountBadge without separate focus", async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(
      createElement(FilterChip, {
        selected: false,
        count: 25,
        onClick,
        children: "Pending",
        "data-testid": "filter-pending",
      }),
    );
    const chip = screen.getByTestId("filter-pending");
    expect(chip).toHaveAccessibleName("Pending, 25");
    expect(within(chip).getByText("25")).toBeInTheDocument();
    await user.tab();
    expect(chip).toHaveFocus();
    await user.keyboard("{Enter}");
    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it("CountBadge renders compact tabular counts", () => {
    const { rerender } = render(createElement(CountBadge, { count: 0 }));
    expect(screen.getByText("0")).toBeInTheDocument();
    rerender(createElement(CountBadge, { count: 1 }));
    expect(screen.getByText("1")).toBeInTheDocument();
    rerender(createElement(CountBadge, { count: 25 }));
    expect(screen.getByText("25")).toBeInTheDocument();
  });
});
