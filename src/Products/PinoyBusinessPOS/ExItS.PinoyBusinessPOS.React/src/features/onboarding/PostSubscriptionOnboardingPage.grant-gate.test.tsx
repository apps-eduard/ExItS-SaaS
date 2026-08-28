import { afterEach, describe, expect, it, vi } from "vitest";
import { render, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { ensureOnboardingProgress, getOnboardingProgress } from "@/api/pos/pos-onboarding-client";
import { PosApiError } from "@/api/pos/pos-http";
import { PostSubscriptionOnboardingPage } from "@/features/onboarding/PostSubscriptionOnboardingPage";
import { writePendingPostSubscriptionOnboarding } from "@/features/onboarding/post-subscription-onboarding";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";
import type { BoundWorkspace } from "@/workspace/types";

vi.mock("@/api/pos/pos-onboarding-client", () => ({
  getOnboardingProgress: vi.fn(),
  ensureOnboardingProgress: vi.fn(),
  updateOnboardingProgress: vi.fn(),
}));

const bindDestination = vi.fn().mockResolvedValue(true);
const orgId = "37c4c64c-728d-40a3-80c5-df0cf7629d25";

const progress = {
  organizationId: orgId,
  organizationSetupStatus: "NotStarted" as const,
  businessSetupStatus: "NotStarted" as const,
  productTemplateStatus: "NotStarted" as const,
  overallStatus: "InProgress" as const,
  primaryBusinessTypeId: null,
  updatedAtUtc: "2026-08-27T00:00:00.000Z",
  createdAtUtc: "2026-08-27T00:00:00.000Z",
};

let workspaceState: {
  status: "idle" | "loading" | "ready" | "binding" | "bound" | "access_denied" | "error";
  boundWorkspace: BoundWorkspace | null;
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
      selectedOrganizationId: orgId,
      accountClass: "Organization",
    },
  }),
}));

const getProgress = vi.mocked(getOnboardingProgress);
const ensureProgress = vi.mocked(ensureOnboardingProgress);

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
    sessionStorage.clear();
    getProgress.mockReset();
    ensureProgress.mockReset();
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
    expect(ensureProgress).not.toHaveBeenCalled();
    expect(bindDestination).toHaveBeenCalled();
  });

  it("queries POS onboarding after a manage-business grant is present", async () => {
    workspaceState.status = "bound";
    workspaceState.boundWorkspace = {
      organizationId: orgId,
      organizationDisplayName: "Mica Cofee",
      branchId: null,
      branchName: null,
      experience: "manage_business",
    };
    workspaceState.sessionGrant = {
      accessToken: "pos-bearer",
      productAccessAllowed: true,
      organizationManagementAuthority: true,
    };
    getProgress.mockResolvedValue(progress);

    renderPage();
    await waitFor(() => expect(getProgress).toHaveBeenCalledTimes(1));
  });

  it("binds manage-business when only a selling grant is present", async () => {
    workspaceState.status = "bound";
    workspaceState.boundWorkspace = {
      organizationId: orgId,
      organizationDisplayName: "Mica Cofee",
      branchId: "branch-1",
      branchName: "Main",
      experience: "start_selling",
    };
    workspaceState.sessionGrant = {
      accessToken: "pos-bearer",
      productAccessAllowed: true,
    };

    renderPage();
    await waitFor(() => expect(bindDestination).toHaveBeenCalled());
    expect(getProgress).not.toHaveBeenCalled();
    expect(ensureProgress).not.toHaveBeenCalled();
  });

  it("opens the setup wizard from the post-subscribe pending flag", async () => {
    writePendingPostSubscriptionOnboarding({ organizationId: orgId });
    workspaceState.status = "bound";
    workspaceState.boundWorkspace = {
      organizationId: orgId,
      organizationDisplayName: "Mica Cofee",
      branchId: null,
      branchName: null,
      experience: "manage_business",
    };
    workspaceState.sessionGrant = {
      accessToken: "pos-bearer",
      productAccessAllowed: true,
      organizationManagementAuthority: true,
    };
    ensureProgress.mockResolvedValue(progress);
    getProgress.mockRejectedValue(
      new PosApiError(404, { detail: "not found", errorCode: "pos.onboarding.progress.not_found" }),
    );

    const view = renderPage();
    await waitFor(() => {
      expect(ensureProgress).toHaveBeenCalledTimes(1);
      expect(view.getByTestId("post-subscription-onboarding-page")).toBeTruthy();
    });
    expect(getProgress).toHaveBeenCalled();
  });
});
