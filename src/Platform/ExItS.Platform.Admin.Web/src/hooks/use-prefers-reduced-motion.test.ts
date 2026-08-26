import { renderHook } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { usePrefersReducedMotion } from "@/hooks/use-prefers-reduced-motion";

describe("usePrefersReducedMotion", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("reports the current reduced-motion media query", () => {
    const listeners = new Set<(event: MediaQueryListEvent) => void>();
    vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
      const matches = query.includes("prefers-reduced-motion") && query.includes("reduce");
      return {
        matches,
        media: query,
        onchange: null,
        addEventListener: (_type: string, listener: EventListener) => {
          listeners.add(listener as (event: MediaQueryListEvent) => void);
        },
        removeEventListener: (_type: string, listener: EventListener) => {
          listeners.delete(listener as (event: MediaQueryListEvent) => void);
        },
        addListener: () => undefined,
        removeListener: () => undefined,
        dispatchEvent: () => true,
      } satisfies MediaQueryList;
    });

    const { result } = renderHook(() => usePrefersReducedMotion());
    expect(result.current).toBe(true);
  });
});
