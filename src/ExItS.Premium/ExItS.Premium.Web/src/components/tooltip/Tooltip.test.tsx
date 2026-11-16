import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import popoverCss from "../time/AnchoredPopover.module.scss?raw";
import { placeTooltip } from "../overlay/overlay";
import tooltipCss from "./Tooltip.module.scss?raw";
import { Tooltip } from "./Tooltip";

function Example() {
  return (
    <Tooltip content="Saves the record">
      <button type="button">Save</button>
    </Tooltip>
  );
}

describe("Tooltip", () => {
  it("describes the focused control and hides on Escape", async () => {
    const user = userEvent.setup();
    render(<Example />);
    const save = screen.getByRole("button", { name: "Save" });
    save.focus();
    const tip = await screen.findByRole("tooltip");
    expect(tip).toHaveTextContent("Saves the record");
    expect(save.parentElement).toHaveAttribute("aria-describedby", tip.id);
    await user.keyboard("{Escape}");
    expect(save.parentElement).not.toHaveAttribute("aria-describedby");
    expect(tip).not.toHaveAttribute("aria-hidden");
    expect(tip.childElementCount).toBe(0);
  });

  it("caps the tooltip radius at 12px and keeps the tail off the popover", () => {
    expect(tooltipCss).toMatch(/border-radius:\s*min\(var\(--radius-control\), 12px\)/);
    expect(tooltipCss).toContain('[data-side="top"]::after');
    expect(tooltipCss).toContain('[data-side="bottom"]::after');
    expect(popoverCss).not.toContain("tooltip-tail");
    expect(popoverCss).not.toContain("data-side");
  });

  it("points the tail at the trigger, including a flipped and shifted tip", () => {
    const anchor = document.createElement("button");
    const tip = document.createElement("span");
    document.body.append(anchor, tip);
    const view = { width: 400, height: 800 };
    vi.stubGlobal("innerWidth", view.width);
    Object.defineProperty(window, "innerWidth", { configurable: true, value: view.width });
    Object.defineProperty(window, "innerHeight", { configurable: true, value: view.height });
    const tipBox = rect({ width: 160, height: 40, top: 0, left: 0, bottom: 40, right: 160 });
    vi.spyOn(tip, "getBoundingClientRect").mockReturnValue(tipBox);

    vi.spyOn(anchor, "getBoundingClientRect").mockReturnValue(rect({ top: 200, bottom: 240, left: 40, width: 80, height: 40, right: 120 }));
    placeTooltip(anchor, tip);
    expect(tip.dataset.side).toBe("top");
    expect(tip.style.top).toBe("150px");
    expect(tip.style.left).toBe("40px");
    expect(tip.style.getPropertyValue("--tooltip-tail")).toBe("40px");

    vi.spyOn(anchor, "getBoundingClientRect").mockReturnValue(rect({ top: 200, bottom: 240, left: 300, width: 80, height: 40, right: 380 }));
    placeTooltip(anchor, tip);
    expect(tip.style.left).toBe("232px");
    expect(tip.dataset.side).toBe("top");
    expect(tip.style.getPropertyValue("--tooltip-tail")).toBe("108px");

    vi.spyOn(anchor, "getBoundingClientRect").mockReturnValue(rect({ top: 4, bottom: 44, left: 40, width: 80, height: 40, right: 120 }));
    placeTooltip(anchor, tip);
    expect(tip.dataset.side).toBe("bottom");
    expect(tip.style.top).toBe("54px");
    expect(tip.style.getPropertyValue("--tooltip-tail")).toBe("40px");
  });
});

function rect(values: Partial<DOMRect>): DOMRect {
  return {
    x: 0,
    y: 0,
    top: 0,
    left: 0,
    bottom: 0,
    right: 0,
    width: 0,
    height: 0,
    toJSON() {
      return {};
    },
    ...values,
  };
}
