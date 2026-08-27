import { afterEach, describe, expect, it, vi } from "vitest";
import { render, waitFor } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { getOnboardingProgress } from "@/api/pos/pos-onboarding-client";
import { OnboardingResumeGate } from "@/features/onboarding/OnboardingResumeGate";
import type { BoundWorkspace } from "@/workspace/types";

vi.mock("@/api/pos/pos-onboarding-client", () => ({
  getOnboardingProgress: vi.fn(),
}));

const boundWorkspace: BoundWorkspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationDisplayName: "Test Org",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  branchName: "Main",
  experience: "Operations",
};

let workspaceState = {
  status: "bound" as const,
  boundWorkspace,
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceState,
}));

const getProgress = vi.mocked(getOnboardingProgress);

function completedProgress() {
  return {
    organizationId: boundWorkspace.organizationId,
    organizationSetupStatus: "Completed" as const,
    businessSetupStatus: "Completed" as const,
    productTemplateStatus: "Completed" as const,
    overallStatus: "Completed" as const,
    primaryBusinessTypeId: null,
    updatedAtUtc: "2026-08-27T00:00:00.000Z",
    createdAtUtc: "2026-08-27T00:00:00.000Z",
  };
}

describe("OnboardingResumeGate", () => {
  afterEach(() => {
    getProgress.mockReset();
    workspaceState = { status: "bound", boundWorkspace };
  });

  it("checks onboarding progress once per organization, not on every tab path", async () => {
    getProgress.mockImplementation(
      () =>
        new Promise((resolve) => {
          window.setTimeout(() => resolve(completedProgress()), 20);
        }),
    );

    const router = createMemoryRouter(
      [
        {
          path: "/",
          element: <OnboardingResumeGate />,
          children: [
            { path: "role/manager", element: <div>home</div> },
            { path: "catalog", element: <div>catalog</div> },
            { path: "orders", element: <div>orders</div> },
          ],
        },
      ],
      { initialEntries: ["/role/manager"] },
    );

    render(<RouterProvider router={router} />);

    await waitFor(() => expect(getProgress).toHaveBeenCalledTimes(1));

    await router.navigate("/catalog");
    await router.navigate("/orders");
    await router.navigate("/role/manager");

    await new Promise((resolve) => window.setTimeout(resolve, 50));
    expect(getProgress).toHaveBeenCalledTimes(1);
  });

  it("does not call progress while on personal routes", async () => {
    getProgress.mockResolvedValue(completedProgress());

    const router = createMemoryRouter(
      [
        {
          path: "/",
          element: <OnboardingResumeGate />,
          children: [{ path: "personal", element: <div>personal</div> }],
        },
      ],
      { initialEntries: ["/personal"] },
    );

    render(<RouterProvider router={router} />);
    await new Promise((resolve) => window.setTimeout(resolve, 30));
    expect(getProgress).not.toHaveBeenCalled();
  });
});
