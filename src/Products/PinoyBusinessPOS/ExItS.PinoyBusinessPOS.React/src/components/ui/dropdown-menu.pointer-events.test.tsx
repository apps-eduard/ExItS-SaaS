import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { DropdownMenu } from "@/components/ui/dropdown-menu";

/**
 * VISUALLY HIDDEN OR LOGICALLY CLOSED OVERLAYS MUST NEVER HIT-TEST.
 * Portaled menus with coords==null use visibility:hidden — must also be PE:none.
 */
describe("DropdownMenu pointer-events", () => {
  it("keeps pointer-events none while hidden before position (coords null)", () => {
    // Omit trigger id/aria so reposition cannot resolve a trigger → coords stay null.
    render(
      <DropdownMenu
        open
        onOpenChange={vi.fn()}
        portal
        trigger={() => <span data-testid="broken-trigger">trigger</span>}
      >
        <button type="button" role="menuitem">
          Item
        </button>
      </DropdownMenu>,
    );

    const menu = screen.getByRole("menu", { hidden: true });
    expect(menu).toHaveAttribute("data-exits-dropdown-portal", "true");
    expect(menu).toHaveStyle({ visibility: "hidden", pointerEvents: "none" });
  });

  it("enables pointer-events after the menu is positioned", () => {
    render(
      <DropdownMenu
        open
        onOpenChange={vi.fn()}
        portal
        trigger={(props) => (
          <button type="button" id={props.id} aria-haspopup="menu" aria-expanded={props.expanded}>
            Open
          </button>
        )}
      >
        <button type="button" role="menuitem">
          Item
        </button>
      </DropdownMenu>,
    );

    const menu = screen.getByRole("menu");
    expect(menu).toHaveStyle({ visibility: "visible", pointerEvents: "auto" });
  });
});
