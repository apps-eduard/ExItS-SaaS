import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { PlatformAntiforgeryDefaults } from "@/api/platform-antiforgery";
import { clearPlatformAntiforgeryToken } from "@/api/platform-http";
import { resetCommercialMutationInFlight } from "@/api/commercial/commercial-http";
import { commercialQueryKeyRoots } from "@/api/commercial/commercial-query-keys";
import { useSuspendSubscriptionMutation } from "@/features/commercial/use-commercial-mutations";

const subscription = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  productCode: "pinoy-business-pos",
  planId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
  status: "Suspended",
};

function wrapper(queryClient: QueryClient) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe("commercial mutation hooks", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
    clearPlatformAntiforgeryToken();
    resetCommercialMutationInFlight();
  });

  it("invalidates organization commercial queries on success and keeps retry disabled", async () => {
    vi.stubEnv("VITE_PLATFORM_API_BASE_URL", "http://platform.test");
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      if (String(input).endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
        } as Response;
      }
      return {
        ok: true,
        status: 200,
        json: async () => subscription,
      } as Response;
    });
    vi.stubGlobal("fetch", fetchMock);

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    const { result } = renderHook(() => useSuspendSubscriptionMutation(), {
      wrapper: wrapper(queryClient),
    });
    expect(result.current.reset).toBeTypeOf("function");
    await result.current.mutateAsync({ subscriptionId: subscription.id, body: { expectedVersion: 1 } });
    await waitFor(() => expect(invalidate).toHaveBeenCalled());
    const keys = invalidate.mock.calls.map((call) => call[0]?.queryKey);
    expect(keys).toContainEqual([
      ...commercialQueryKeyRoots.organizationSubscriptions,
      subscription.organizationId,
    ]);
    expect(result.current.failureCount).toBe(0);
  });

  it("does not invalidate cached reads when the mutation fails", async () => {
    vi.stubEnv("VITE_PLATFORM_API_BASE_URL", "http://platform.test");
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        if (String(input).endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          return {
            ok: true,
            status: 200,
            json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
          } as Response;
        }
        return {
          ok: false,
          status: 409,
          json: async () => ({
            status: 409,
            errorCode: "application.concurrency_conflict",
            detail: "Version mismatch.",
          }),
        } as Response;
      }),
    );
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const cacheKey = [...commercialQueryKeyRoots.organizationSubscriptions, subscription.organizationId];
    queryClient.setQueryData(cacheKey, { items: ["unchanged"] });
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    const { result } = renderHook(() => useSuspendSubscriptionMutation(), {
      wrapper: wrapper(queryClient),
    });
    await expect(
      result.current.mutateAsync({ subscriptionId: subscription.id }),
    ).rejects.toMatchObject({ status: 409 });
    expect(invalidate).not.toHaveBeenCalled();
    expect(queryClient.getQueryData(cacheKey)).toEqual({ items: ["unchanged"] });
  });
});
