import { afterEach, describe, expect, it, vi } from "vitest";
import { render, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { getOnboardingProgress } from "@/api/pos/pos-onboarding-client";
import { PostSubscriptionOnboardingPage } from "@/features/onboarding/PostSubscriptionOnboardingPage";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";

vi.mock("@/api/pos/pos-onboarding-client", () => ({
  getOnboardingProgress: vi.fn(),
  ensureOnboardingProgress: vi.fn(),
  updateOnboardingProgress: vi.fn(),
}));

const bindDestination = vi.fn().mockResolvedValue(true);

let workspaceState: {
  status: "idle" | "loading" | "ready" | "binding" | "bound" | "access_denied" | "error";
  boundWorkspace: null;
  workspaces: { organizationId: string; displayName: string }[];
  sessionGrant: SessionGrantResponse | null;
  bindFailureKind: string | null;
  bindDestination: typeof bindDestination;
} = {
  status: "ready",
  boundWorkspace: null,
  workspaces: [],
  sessionGrant: null,
  bindFailureKind: null,
  bindDestination,
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceState,
}));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: {
      selectedOrganizationId: "37c4c64c-728d-40a3-80c5-df0cf7629d25",
      accountClass: "Organization",
    },
  }),
}));

const getProgress = vi.mocked(getOnboardingProgress);

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter>
            <PostSubscriptionOnboardingPage />
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("PostSubscriptionOnboardingPage POS grant gate", () => {
  afterEach(() => {
    getProgress.mockReset();
    bindDestination.mockClear();
    workspaceState = {
      status: "ready",
      boundWorkspace: null,
      workspaces: [],
      sessionGrant: null,
      bindFailureKind: null,
      bindDestination,
    };
  });

  it("does not query POS onboarding until a session grant exists", async () => {
    renderPage();
    await new Promise((resolve) => window.setTimeout(resolve, 40));
    expect(getProgress).not.toHaveBeenCalled();
    expect(bindDestination).toHaveBeenCalled();
  });

  it("queries POS onboarding after the organization session grant is present", async () => {
    workspaceState.status = "bound";
    workspaceState.sessionGrant = {
      accessToken: "pos-bearer",
      productAccessAllowed: true,
    };
    getProgress.mockResolvedValue({
      organizationId: "37c4c64c-728d-40a3-80c5-df0cf7629d25",
      organizationSetupStatus: "NotStarted",
      businessSetupStatus: "NotStarted",
      productTemplateStatus: "NotStarted",
      overallStatus: "InProgress",
      primaryBusinessTypeId: null,
      updatedAtUtc: "2026-08-27T00:00:00.000Z",
      createdAtUtc: "2026-08-27T00:00:00.000Z",
    });

    renderPage();
    await waitFor(() => expect(getProgress).toHaveBeenCalledTimes(1));
  });
});
