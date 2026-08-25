import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  reportGlobalClientError,
  reportGlobalRuntimeError,
  subscribeGlobalClientErrors,
} from "@/diagnostics/global-error-reporter";

describe("global-error-reporter", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("notifies subscribers with operational reports", () => {
    const listener = vi.fn();
    const unsubscribe = subscribeGlobalClientErrors(listener);

    reportGlobalClientError({
      error: new Error("network down"),
      source: "network",
      operation: "fetch todos",
      friendlyMessage: "Could not load todos.",
    });

    expect(listener).toHaveBeenCalledTimes(1);
    expect(listener.mock.calls[0]?.[0]).toMatchObject({
      source: "network",
      operation: "fetch todos",
      friendlyMessage: "Could not load todos.",
    });

    unsubscribe();
  });

  it("dedupes identical reports within a short window", () => {
    const listener = vi.fn();
    subscribeGlobalClientErrors(listener);

    const input = {
      error: new Error("Not Found"),
      source: "network" as const,
      operation: "get todo",
    };

    reportGlobalClientError(input);
    reportGlobalClientError(input);

    expect(listener).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(3000);
    reportGlobalClientError(input);
    expect(listener).toHaveBeenCalledTimes(2);
  });

  it("reports runtime failures from window and promise rejections", () => {
    const listener = vi.fn();
    subscribeGlobalClientErrors(listener);

    reportGlobalRuntimeError({
      source: "unhandled-rejection",
      error: new Error("boom"),
    });

    expect(listener).toHaveBeenCalledTimes(1);
    expect(listener.mock.calls[0]?.[0].source).toBe("unhandled-rejection");
  });
});
