import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonResponse } from "@/test/session-context";
import { render, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";
import {
  clearPlatformAntiforgeryToken,
  hasPlatformAntiforgeryToken,
  prefetchPlatformAntiforgeryToken,
} from "@/api/platform/platform-http";
import { SessionProvider, useSession } from "@/session/SessionProvider";

function SignInProbe() {
  const { signIn } = useSession();
  return (
    <button type="button" onClick={() => void signIn("owner", "secret")}>
      Sign in
    </button>
  );
}

describe("sign-in antiforgery preservation", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("prefetches antiforgery with credentials include when bootstrap succeeds", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          expect(init?.credentials).toBe("include");
          return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        throw new Error(`Unexpected fetch: ${url}`);
      }),
    );

    await expect(prefetchPlatformAntiforgeryToken()).resolves.toBe(true);
    expect(hasPlatformAntiforgeryToken()).toBe(true);
  });

  it("refreshes antiforgery after login so authenticated bootstrap can succeed", async () => {
    let antiforgeryBootstrapCount = 0;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          antiforgeryBootstrapCount += 1;
          expect(init?.credentials).toBe("include");
          return jsonResponse(200, {
              headerName: "X-XSRF-TOKEN",
              token: `csrf-${antiforgeryBootstrapCount}`,
            });
        }
        if (url.includes("/api/v1/platform/auth/login") && init?.method === "POST") {
          return jsonResponse(200, {
              sessionId: "22222222-2222-2222-2222-222222222222",
              userId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
              username: "owner",
              accountClass: "Organization",
            });
        }
        if (url.includes("/api/v1/platform/auth/me")) {
          return jsonResponse(200, {
              sessionId: "22222222-2222-2222-2222-222222222222",
              userId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
              username: "owner",
              accountClass: "Organization",
            });
        }
        throw new Error(`Unexpected fetch: ${url}`);
      }),
    );

    await prefetchPlatformAntiforgeryToken();
    expect(antiforgeryBootstrapCount).toBe(1);

    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const user = userEvent.setup();
    render(
      <QueryClientProvider client={client}>
        <SessionProvider>
          <SignInProbe />
        </SessionProvider>
      </QueryClientProvider>,
    );

    await user.click(await waitFor(() => document.querySelector("button")!));
    await waitFor(() => {
      expect(hasPlatformAntiforgeryToken()).toBe(true);
    });
    expect(antiforgeryBootstrapCount).toBeGreaterThanOrEqual(2);
  });
});
