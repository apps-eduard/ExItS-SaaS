import { createElement, useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import type { ExitsTabsVariant } from "@/components/exits/ExitsTabs";
import { UI_STANDARDS_DEFAULT_OPEN } from "@/features/ui-standards/ui-standards-disclosure";

function ControlledTabs({
  variant = "underline" as ExitsTabsVariant,
  items,
  initial = "a",
  layout,
  activeTreatment,
  scrollable,
}: {
  variant?: ExitsTabsVariant;
  items: Parameters<typeof ExitsTabs>[0]["items"];
  initial?: string;
  layout?: "equal" | "content";
  activeTreatment?: "solid" | "accent";
  scrollable?: boolean;
}) {
  const [value, setValue] = useState(initial);
  return createElement(ExitsTabs, {
    variant,
    items,
    value,
    onValueChange: setValue,
    ariaLabel: "Demo tabs",
    testId: "tabs-demo",
    layout,
    activeTreatment,
    scrollable,
    panels: Object.fromEntries(items.map((i) => [i.key, `Panel ${i.key}`])),
  });
}

describe("ExitsTabs visual pilot", () => {
  it("renders all seven variants including pillBar", () => {
    for (const variant of [
      "underline",
      "soft",
      "pill",
      "pillBar",
      "segmented",
      "enclosed",
      "vertical",
    ] as const) {
      const { unmount } = render(
        createElement(ControlledTabs, {
          variant,
          items: [
            { key: "a", label: "One" },
            { key: "b", label: "Two" },
          ],
        }),
      );
      expect(screen.getByRole("tablist")).toHaveAttribute("data-variant", variant);
      unmount();
    }
  });

  it("pill is independent of segmented (gaps vs shared container)", () => {
    const { rerender } = render(
      createElement(ControlledTabs, {
        variant: "pill",
        items: [
          { key: "a", label: "All", count: 24 },
          { key: "b", label: "Pending", count: 6 },
        ],
      }),
    );
    const pillList = screen.getByRole("tablist");
    expect(pillList).toHaveAttribute("data-variant", "pill");
    expect(pillList.className).toMatch(/gap-2/);
    expect(pillList.className).not.toMatch(/border-border bg-\[var\(--exits-surface-muted\)\]/);

    rerender(
      createElement(ControlledTabs, {
        variant: "segmented",
        items: [
          { key: "a", label: "All" },
          { key: "b", label: "Pending" },
        ],
      }),
    );
    const segList = screen.getByRole("tablist");
    expect(segList).toHaveAttribute("data-variant", "segmented");
    expect(segList.className).toMatch(/p-0\.5/);
  });

  it("pillBar is distinct from pill and segmented", () => {
    const { rerender } = render(
      createElement(ControlledTabs, {
        variant: "pillBar",
        layout: "equal",
        items: [
          { key: "a", label: "Overview" },
          { key: "b", label: "Orders" },
        ],
      }),
    );
    const bar = screen.getByRole("tablist");
    expect(bar).toHaveAttribute("data-variant", "pillBar");
    expect(bar).toHaveAttribute("data-layout", "equal");
    expect(bar).toHaveAttribute("data-active-treatment", "solid");
    expect(bar.className).toMatch(/rounded-full/);
    expect(bar.className).not.toMatch(/gap-2/);
    expect(screen.getByRole("tab", { name: "Overview" }).className).toMatch(/rounded-full/);
    expect(screen.getByRole("tab", { name: "Overview" }).className).toMatch(/flex-1/);

    rerender(
      createElement(ControlledTabs, {
        variant: "pill",
        items: [
          { key: "a", label: "Overview" },
          { key: "b", label: "Orders" },
        ],
      }),
    );
    expect(screen.getByRole("tablist").className).toMatch(/gap-2/);
    expect(screen.getByRole("tablist")).not.toHaveAttribute("data-active-treatment");

    rerender(
      createElement(ControlledTabs, {
        variant: "segmented",
        items: [
          { key: "a", label: "Overview" },
          { key: "b", label: "Orders" },
        ],
      }),
    );
    expect(screen.getByRole("tablist").className).toMatch(/rounded-\[var\(--exits-radius-md\)\]/);
  });

  it("pillBar supports content layout and accent treatment", () => {
    render(
      createElement(ControlledTabs, {
        variant: "pillBar",
        layout: "content",
        activeTreatment: "accent",
        items: [
          { key: "a", label: "All", count: 24, countTone: "neutral" },
          { key: "b", label: "Pending", count: 6, countTone: "warning" },
        ],
      }),
    );
    const list = screen.getByRole("tablist");
    expect(list).toHaveAttribute("data-layout", "content");
    expect(list).toHaveAttribute("data-active-treatment", "accent");
    expect(screen.getByRole("tab", { name: /All/i }).className).not.toMatch(/flex-1/);
    expect(screen.getByText("24")).toBeInTheDocument();
  });

  it("pill supports icon + count composition", () => {
    render(
      createElement(ControlledTabs, {
        variant: "pill",
        items: [
          { key: "a", label: "Products", count: 24, countTone: "neutral" },
          { key: "b", label: "Orders", count: 6, countTone: "neutral" },
        ],
      }),
    );
    expect(screen.getByText("24")).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: /Products/i }).className).toMatch(/rounded-full/);
  });

  it("selects tabs, updates aria-selected, and shows panel", async () => {
    const user = userEvent.setup();
    render(
      createElement(ControlledTabs, {
        items: [
          { key: "a", label: "Overview" },
          { key: "b", label: "Products", count: 24, countTone: "neutral" },
        ],
      }),
    );
    expect(screen.getByRole("tab", { name: /Overview/i })).toHaveAttribute("aria-selected", "true");
    expect(screen.getByText("Panel a")).toBeInTheDocument();
    await user.click(screen.getByRole("tab", { name: /Products/i }));
    expect(screen.getByRole("tab", { name: /Products/i })).toHaveAttribute("aria-selected", "true");
    expect(screen.getByText("Panel b")).toBeInTheDocument();
    expect(screen.getByText("24")).toBeInTheDocument();
  });

  it("skips disabled tabs and supports keyboard arrows / Home / End", async () => {
    const user = userEvent.setup();
    render(
      createElement(ControlledTabs, {
        items: [
          { key: "a", label: "Overview" },
          { key: "b", label: "Products", disabled: true },
          { key: "c", label: "Orders" },
        ],
      }),
    );
    const overview = screen.getByRole("tab", { name: "Overview" });
    overview.focus();
    await user.keyboard("{ArrowRight}");
    expect(screen.getByRole("tab", { name: "Orders" })).toHaveAttribute("aria-selected", "true");
    await user.keyboard("{Home}");
    expect(screen.getByRole("tab", { name: "Overview" })).toHaveAttribute("aria-selected", "true");
    await user.keyboard("{End}");
    expect(screen.getByRole("tab", { name: "Orders" })).toHaveAttribute("aria-selected", "true");
  });

  it("supports vertical ArrowDown navigation", async () => {
    const user = userEvent.setup();
    render(
      createElement(ControlledTabs, {
        variant: "vertical",
        items: [
          { key: "a", label: "General" },
          { key: "b", label: "Branches" },
        ],
      }),
    );
    expect(screen.getByRole("tablist")).toHaveAttribute("aria-orientation", "vertical");
    screen.getByRole("tab", { name: "General" }).focus();
    await user.keyboard("{ArrowDown}");
    expect(screen.getByRole("tab", { name: "Branches" })).toHaveAttribute("aria-selected", "true");
  });

  it("UI Standards tabs disclosure defaults match pilot map", () => {
    expect(UI_STANDARDS_DEFAULT_OPEN["tabs.variants"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["tabs.counts"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["tabs.icon-options"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["tabs.pill-bar"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["tabs.real-world"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["tabs.cheatsheet"]).toBe(false);
  });
});
