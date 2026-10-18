import { describe, expect, it, vi } from "vitest";

import { scrollWithinContainer } from "@/lib/scroll-within";

function rect(left: number, top: number, width: number, height: number): DOMRect {
  return { left, top, width, height, right: left + width, bottom: top + height, x: left, y: top, toJSON: () => ({}) };
}

function setup(containerRect: DOMRect, elementRect: DOMRect) {
  const container = document.createElement("div");
  const element = document.createElement("button");
  container.appendChild(element);
  container.getBoundingClientRect = () => containerRect;
  element.getBoundingClientRect = () => elementRect;
  const scrollBy = vi.fn();
  container.scrollBy = scrollBy as unknown as typeof container.scrollBy;
  return { container, element, scrollBy };
}

describe("scrollWithinContainer", () => {
  it("scrolls only the container to reveal an item past its right edge", () => {
    const windowScroll = vi.spyOn(window, "scrollBy").mockImplementation(() => undefined);
    const { container, element, scrollBy } = setup(rect(0, 0, 300, 40), rect(280, 0, 80, 40));
    scrollWithinContainer(container, element, "smooth");
    expect(scrollBy).toHaveBeenCalledWith({ left: 60, top: 0, behavior: "smooth" });
    expect(windowScroll).not.toHaveBeenCalled();
    windowScroll.mockRestore();
  });

  it("scrolls left for an item before the start and does nothing when visible", () => {
    const before = setup(rect(100, 0, 300, 40), rect(60, 0, 80, 40));
    scrollWithinContainer(before.container, before.element);
    expect(before.scrollBy).toHaveBeenCalledWith({ left: -40, top: 0, behavior: "auto" });

    const visible = setup(rect(0, 0, 300, 40), rect(100, 0, 80, 40));
    scrollWithinContainer(visible.container, visible.element);
    expect(visible.scrollBy).not.toHaveBeenCalled();
  });

  it("ignores page position so an off-screen list does not pull the page", () => {
    const { container, element, scrollBy } = setup(rect(0, -900, 300, 40), rect(20, -900, 80, 40));
    scrollWithinContainer(container, element);
    expect(scrollBy).not.toHaveBeenCalled();
  });
});
