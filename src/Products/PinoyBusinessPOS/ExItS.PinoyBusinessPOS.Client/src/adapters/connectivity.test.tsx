import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { getBrowserNetworkReachability } from "@/adapters/connectivity";

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: null,
    signOut: async () => ({ ok: true as const, nextRoute: "/sign-in" }),
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: null,
    clearBoundWorkspace: () => undefined,
    workspaces: [],
  }),
}));

describe("connectivity foundation", () => {
  it("does not treat navigator.onLine as API-health Online status", () => {
    expect(getBrowserNetworkReachability()).not.toBe("Online");
    expect(["unknown", true, false]).toContain(getBrowserNetworkReachability());
  });

  it("AppTopBar does not claim Online or Offline in the closed chrome", () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <AppTopBar />
        </MemoryRouter>
      </AppProviders>,
    );
    const header = screen.getByRole("banner");
    expect(header).toHaveTextContent("Pinoy Business POS");
    // Closed connection control must not surface Online/Offline as chrome copy.
    expect(header.textContent ?? "").not.toMatch(/\bOnline\b/);
    expect(header.textContent ?? "").not.toMatch(/\bOffline\b/);
    expect(header).not.toHaveTextContent("Syncing");
  });
});
