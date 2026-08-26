import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { getPosAccessToken, setPosAccessToken } from "@/api/platform/pos-access-token";
import { getUnlockedDek, setUnlockedDek } from "@/offline/offline-unlock-session";
import { generateRandomDek } from "@/offline/local-store-key";
import { SessionProvider, useSession } from "@/session/SessionProvider";
import {
  clearPendingRemoteLogout,
  hasPendingRemoteLogout,
} from "@/session/pending-remote-logout";

const USER = "248935e9-e462-425f-88f5-a9255bf12748";

function OfflineLogoutProbe() {
  const { status, signOut } = useSession();
  return (
    <div>
      <p data-testid="session-status">{status}</p>
      <button
        type="button"
        onClick={() => {
          void signOut();
        }}
      >
        Sign out
      </button>
    </div>
  );
}

describe("offline logout local lock", () => {
  beforeEach(async () => {
    window.localStorage.clear();
    clearPendingRemoteLogout();
    setPosAccessToken("online-token");
    setUnlockedDek(USER, await generateRandomDek());
  });

  it("clears DEK and session artifacts before remote logout completes", async () => {
    const user = userEvent.setup();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        const method = init?.method ?? "GET";
        if (url.includes("/auth/me")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({
              userId: USER,
              username: "kizy",
              displayName: "Kizy",
              accountClass: "Organization",
            }),
          } as Response;
        }
        if (url.includes("/auth/logout") && method === "POST") {
          await new Promise((resolve) => setTimeout(resolve, 50));
          return { ok: true, status: 204, json: async () => ({}) } as Response;
        }
        return { ok: true, status: 200, json: async () => ({}) } as Response;
      }),
    );

    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <SessionProvider>
          <OfflineLogoutProbe />
        </SessionProvider>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("session-status")).toHaveTextContent("authenticated");
    });
    expect(getUnlockedDek(USER)).not.toBeNull();

    await user.click(screen.getByRole("button", { name: "Sign out" }));

    await waitFor(() => {
      expect(getPosAccessToken()).toBeNull();
      expect(getUnlockedDek(USER)).toBeNull();
    });
  });

  it("marks pending remote logout when network logout fails", async () => {
    const user = userEvent.setup();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        const method = init?.method ?? "GET";
        if (url.includes("/auth/me")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({
              userId: USER,
              username: "kizy",
              displayName: "Kizy",
              accountClass: "Organization",
            }),
          } as Response;
        }
        if (url.includes("/auth/logout") && method === "POST") {
          return {
            ok: false,
            status: 503,
            json: async () => ({ detail: "logout unavailable" }),
          } as Response;
        }
        return { ok: true, status: 200, json: async () => ({}) } as Response;
      }),
    );

    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <SessionProvider>
          <OfflineLogoutProbe />
        </SessionProvider>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("session-status")).toHaveTextContent("authenticated");
    });

    await user.click(screen.getByRole("button", { name: "Sign out" }));

    await waitFor(() => {
      expect(hasPendingRemoteLogout()).toBe(true);
      expect(getPosAccessToken()).toBeNull();
      expect(getUnlockedDek(USER)).toBeNull();
    });
  });
});
