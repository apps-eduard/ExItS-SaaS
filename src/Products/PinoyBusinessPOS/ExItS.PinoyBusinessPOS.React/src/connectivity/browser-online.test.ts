import { afterEach, describe, expect, it, vi } from "vitest";
import { subscribeBrowserOnline } from "@/connectivity/browser-online";

function setNavigatorOnline(value: boolean) {
  Object.defineProperty(window.navigator, "onLine", {
    configurable: true,
    get: () => value,
  });
}

describe("browser online advisory", () => {
  afterEach(() => {
    setNavigatorOnline(true);
  });

  it("reports the initial online state", () => {
    setNavigatorOnline(true);
    const onChange = vi.fn();
    const stop = subscribeBrowserOnline(onChange);
    expect(onChange).toHaveBeenCalledWith(true);
    stop();
  });

  it("treats the offline event as advisory even if navigator.onLine is stale", () => {
    setNavigatorOnline(true);
    const onChange = vi.fn();
    const stop = subscribeBrowserOnline(onChange);
    onChange.mockClear();
    window.dispatchEvent(new Event("offline"));
    expect(onChange).toHaveBeenCalledWith(false);
    stop();
  });

  it("removes listeners on unsubscribe", () => {
    const onChange = vi.fn();
    const stop = subscribeBrowserOnline(onChange);
    onChange.mockClear();
    stop();
    window.dispatchEvent(new Event("offline"));
    expect(onChange).not.toHaveBeenCalled();
  });
});
