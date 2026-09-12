import type { ReactElement } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { ClipboardList, Inbox } from "lucide-react";
import { ModuleSubnav } from "@/components/exits/ModuleSubnav";
import { formatUiStandardsCursorClipboard } from "@/features/ui-standards/UiStandardsCopyCommand";

function renderNav(ui: ReactElement, initial = "/demo/incoming") {
  return render(
    <MemoryRouter initialEntries={[initial]}>
      <Routes>
        <Route path="*" element={ui} />
      </Routes>
    </MemoryRouter>,
  );
}

const purchasingItems = [
  { key: "po", label: "Purchase orders", to: "/demo/po", icon: ClipboardList, count: 0 },
  { key: "incoming", label: "Incoming orders", to: "/demo/incoming", icon: Inbox, count: 2 },
  { key: "receive", label: "Ready to receive", to: "/demo/receive", count: 0 },
];

describe("ModuleSubnav", () => {
  it("renders nav links with aria-current and no tab roles", async () => {
    const user = userEvent.setup();
    renderNav(
      <ModuleSubnav
        variant="pillBar"
        ariaLabel="Purchasing"
        testId="module-subnav-demo"
        scrollable
        items={purchasingItems}
      />,
    );

    const nav = screen.getByRole("navigation", { name: "Purchasing" });
    expect(nav.querySelector('[role="tablist"]')).toBeNull();
    expect(nav.querySelector('[role="tab"]')).toBeNull();

    const incoming = screen.getByRole("link", { name: /Incoming orders/i });
    expect(incoming).toHaveAttribute("aria-current", "page");
    expect(incoming.getAttribute("aria-selected")).toBeNull();

    expect(within(screen.getByRole("link", { name: /Purchase orders/i })).getByText("0")).toBeInTheDocument();
    expect(within(incoming).getByText("2")).toBeInTheDocument();

    await user.click(screen.getByRole("link", { name: /Purchase orders/i }));
    expect(screen.getByRole("link", { name: /Purchase orders/i })).toHaveAttribute(
      "aria-current",
      "page",
    );
  });

  it("supports equal width, disabled item, and scrollable list class", () => {
    renderNav(
      <ModuleSubnav
        variant="soft"
        layout="equal"
        scrollable
        ariaLabel="Demo"
        testId="subnav-eq"
        items={[
          ...purchasingItems,
          { key: "x", label: "Disabled", to: "/demo/x", disabled: true },
        ]}
      />,
    );

    const list = screen.getByTestId("subnav-eq-list");
    expect(list).toHaveAttribute("data-layout", "equal");
    expect(list.className).toMatch(/overflow-x-auto/);
    expect(list.className).toMatch(/flex-nowrap/);
    expect(screen.getByText("Disabled").closest("[aria-disabled]")).toHaveAttribute(
      "aria-disabled",
      "true",
    );
  });
});

describe("Module Subnav pilot clipboard", () => {
  it("does not claim locked for pilot status", () => {
    const text = formatUiStandardsCursorClipboard(
      "Module Subnav",
      "MODULE SUBNAV + PILL BAR",
      'Use related route navigation with aria-current="page"; do not use tab/tabpanel semantics.',
      "pilot",
    );
    expect(text).toContain("Use the approved ExItS Module Subnav pilot.");
    expect(text).not.toContain("locked ExItS Module Subnav");
    expect(text).toContain("Preserve existing routing");
  });
});
