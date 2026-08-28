import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import { attachGlobalQueryErrorHandlers } from "@/diagnostics/attach-global-query-error-handlers";
import { clearGlobalClientErrorOverlay, subscribeGlobalClientErrors } from "@/diagnostics/global-error-reporter";

describe("attachGlobalQueryErrorHandlers", () => {
  afterEach(() => {
    clearGlobalClientErrorOverlay();
  });

  it("does not escalate ordinary query failures to the global overlay", async () => {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    attachGlobalQueryErrorHandlers(client);

    const seen: unknown[] = [];
    const unsubscribe = subscribeGlobalClientErrors((report) => {
      seen.push(report);
    });

    await expect(
      client.fetchQuery({
        queryKey: ["customers", "list"],
        queryFn: async () => {
          throw new Error("POS API request failed (404)");
        },
      }),
    ).rejects.toThrow(/404/);

    await vi.waitFor(() => {
      expect(client.getQueryState(["customers", "list"])?.status).toBe("error");
    });

    expect(seen.filter(Boolean)).toHaveLength(0);
    unsubscribe();
  });

  it("escalates when meta.reportGlobalError is set", async () => {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    attachGlobalQueryErrorHandlers(client);

    const seen: unknown[] = [];
    const unsubscribe = subscribeGlobalClientErrors((report) => {
      seen.push(report);
    });

    await expect(
      client.fetchQuery({
        queryKey: ["critical"],
        meta: { reportGlobalError: true, operation: "critical load" },
        queryFn: async () => {
          throw new Error("boom");
        },
      }),
    ).rejects.toThrow(/boom/);

    await vi.waitFor(() => {
      expect(seen.filter(Boolean).length).toBeGreaterThan(0);
    });
    unsubscribe();
  });
});
