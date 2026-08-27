import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { personalGuideStorageKey } from "@/features/personal/guide/personal-guide-storage";

const personalUserId = "11111111-1111-1111-1111-111111111111";
const otherUserId = "22222222-2222-2222-2222-222222222222";
const personalProfileId = "33333333-3333-3333-3333-333333333333";

function createPersonalFetchMock(userId = personalUserId) {
  return vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/me")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          sessionId: userId,
          userId,
          username: "ana",
          displayName: "Ana Reyes",
          email: "ana@example.com",
          selectedOrganizationId: null,
          accountClass: "Personal",
          homeOrganizationId: null,
          organizationContextLocked: false,
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/organizations")) {
      return {
        ok: true,
        status: 200,
        json: async () => [],
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/personal/dashboard")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          userIdentityId: userId,
          accountProfileId: personalProfileId,
          accountClass: "Personal",
          utangAvailable: true,
          contactCount: 0,
          activeRelationshipCount: 0,
          totalLentBalance: 0,
          totalBorrowedBalance: 0,
          pendingConfirmationCount: 0,
        }),
        text: async () => "",
      } as Response;
    }

    if (
      url.includes("/api/v1/personal/utang/") ||
      url.includes("/api/v1/personal/todos") ||
      url.includes("/api/v1/personal/linked-merchants") ||
      url.includes("/api/v1/personal/customer-link") ||
      url.includes("/api/v1/personal/notifications") ||
      url.includes("/api/v1/personal/ownership")
    ) {
      return {
        ok: true,
        status: 200,
        json: async () => (url.includes("linked-merchants") ? { items: [], page: 1, pageSize: 50, totalCount: 0 } : []),
        text: async () => "",
      } as Response;
    }

    return {
      ok: false,
      status: 404,
      json: async () => ({ detail: `unmocked ${url}` }),
      text: async () => "",
    } as Response;
  });
}

function renderAt(path: string) {
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: [path] });
  return render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
  );
}

describe("Personal Explore ExItS guide page", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  afterEach(async () => {
    await new Promise((resolve) => setTimeout(resolve, 25));
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
    window.localStorage.clear();
  });

  it("renders categories, expands a card, and updates progress when marked learned", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal/guide");

    await waitFor(() => {
      expect(screen.getByTestId("personal-guide-page")).toBeInTheDocument();
    });
    expect(screen.getByText("Explore ExItS")).toBeInTheDocument();
    expect(screen.getByTestId("guide-category-account")).toBeInTheDocument();
    expect(screen.getByTestId("guide-category-people")).toBeInTheDocument();
    expect(screen.getByTestId("guide-category-money")).toBeInTheDocument();
    expect(screen.getByTestId("guide-category-productivity")).toBeInTheDocument();
    expect(screen.getByTestId("guide-category-shopping")).toBeInTheDocument();
    expect(screen.getByTestId("guide-category-activity")).toBeInTheDocument();
    expect(screen.getByTestId("guide-category-business")).toBeInTheDocument();
    expect(screen.getByTestId("guide-progress-text")).toHaveTextContent("0 of 20 features explored");

    await user.click(screen.getByTestId("guide-card-toggle-stores"));
    const panel = document.getElementById("guide-feature-panel-stores");
    expect(panel).not.toHaveAttribute("hidden");
    expect(within(screen.getByTestId("guide-card-stores")).getByText("What you can do:")).toBeInTheDocument();

    await user.click(screen.getByTestId("guide-learned-stores"));
    expect(screen.getByTestId("guide-progress-text")).toHaveTextContent("1 of 20 features explored");
    expect(screen.getByTestId("guide-card-state-stores")).toHaveTextContent("Completed");

    await user.click(screen.getByTestId("guide-card-toggle-stores"));
    expect(document.getElementById("guide-feature-panel-stores")).toHaveAttribute("hidden");
  });

  it("filters All, Not explored, and Completed", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal/guide");

    await waitFor(() => {
      expect(screen.getByTestId("guide-card-stores")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("guide-card-toggle-stores"));
    await user.click(screen.getByTestId("guide-learned-stores"));

    await user.click(screen.getByTestId("guide-filter-completed"));
    expect(screen.getByTestId("guide-card-stores")).toBeInTheDocument();
    expect(screen.queryByTestId("guide-card-todo")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("guide-filter-not-explored"));
    expect(screen.queryByTestId("guide-card-stores")).not.toBeInTheDocument();
    expect(screen.getByTestId("guide-card-todo")).toBeInTheDocument();

    await user.click(screen.getByTestId("guide-filter-all"));
    expect(screen.getByTestId("guide-card-stores")).toBeInTheDocument();
    expect(screen.getByTestId("guide-card-todo")).toBeInTheDocument();
  });

  it("navigates Try It on Stores to the linked merchants route", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal/guide");

    await waitFor(() => {
      expect(screen.getByTestId("guide-card-stores")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("guide-card-toggle-stores"));
    expect(screen.getByTestId("guide-try-stores")).toHaveAttribute("href", "/personal/linked-merchants");
    await user.click(screen.getByTestId("guide-try-stores"));
    await waitFor(() => {
      expect(screen.getByTestId("linked-merchants-page")).toBeInTheDocument();
    });
  });

  it("restores learned progress after remount", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createPersonalFetchMock());
    const first = renderAt("/personal/guide");
    await waitFor(() => {
      expect(screen.getByTestId("guide-card-stores")).toBeInTheDocument();
    });
    await user.click(screen.getByTestId("guide-card-toggle-stores"));
    await user.click(screen.getByTestId("guide-learned-stores"));
    expect(JSON.parse(window.localStorage.getItem(personalGuideStorageKey(personalUserId)) ?? "{}").learned).toContain(
      "stores",
    );
    first.unmount();

    renderAt("/personal/guide");
    await waitFor(() => {
      expect(screen.getByTestId("guide-card-state-stores")).toHaveTextContent("Completed");
    });
    expect(screen.getByTestId("guide-progress-text")).toHaveTextContent("1 of 20 features explored");
  });

  it("shows the Home discovery card and hides it when dismissed", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal");

    await waitFor(() => {
      expect(screen.getByTestId("personal-guide-home-card")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-guide-home-continue")).toHaveAttribute("href", "/personal/guide");
    await user.click(screen.getByTestId("personal-guide-home-dismiss"));
    expect(screen.queryByTestId("personal-guide-home-card")).not.toBeInTheDocument();
  });
});

describe("Personal guide account isolation (page)", () => {
  afterEach(async () => {
    await new Promise((resolve) => setTimeout(resolve, 25));
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
    window.localStorage.clear();
  });

  it("does not show another account's learned state", async () => {
    window.localStorage.setItem(
      personalGuideStorageKey(otherUserId),
      JSON.stringify({ version: 1, learned: ["stores"], homeCardDismissed: false }),
    );
    vi.stubGlobal("fetch", createPersonalFetchMock(personalUserId));
    renderAt("/personal/guide");
    await waitFor(() => {
      expect(screen.getByTestId("guide-card-state-stores")).toHaveTextContent("Not explored");
    });
    expect(screen.getByTestId("guide-progress-text")).toHaveTextContent("0 of 20 features explored");
  });
});
