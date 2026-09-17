import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { getOnboardingProgress } from "@/api/pos/pos-onboarding-client";
import { createMemoryRouter, Outlet, RouterProvider } from "react-router-dom";
import { OnboardingResumeGate } from "@/features/onboarding/OnboardingResumeGate";
import {
  clearPendingSubscriptionCheckout,
  writePendingSubscriptionCheckout,
} from "@/features/subscription-checkout/pending-subscription-checkout";
import type { BoundWorkspace } from "@/workspace/types";

vi.mock("@/api/pos/pos-onboarding-client", () => ({
  getOnboardingProgress: vi.fn(),
}));

const boundWorkspace: BoundWorkspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationDisplayName: "Test Org",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  branchName: "Main",
  experience: "operations",
};

let workspaceState = {
  status: "bound" as const,
  boundWorkspace,
  sessionGrant: { accessToken: "test-grant", productAccessAllowed: true },
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceState,
}));

const getProgress = vi.mocked(getOnboardingProgress);

function inProgressProgress() {
  return {
    organizationId: boundWorkspace.organizationId,
    organizationSetupStatus: "Completed" as const,
    businessSetupStatus: "Completed" as const,
    productTemplateStatus: "NotStarted" as const,
    overallStatus: "InProgress" as const,
    primaryBusinessTypeId: null,
    updatedAtUtc: "2026-08-27T00:00:00.000Z",
    createdAtUtc: "2026-08-27T00:00:00.000Z",
  };
}

function completedProgress() {
  return {
    ...inProgressProgress(),
    productTemplateStatus: "Completed" as const,
    overallStatus: "Completed" as const,
  };
}

/** Gate is an effect-only sibling of routed content (same as App.tsx). */
function GateLayout() {
  return (
    <>
      <OnboardingResumeGate />
      <Outlet />
    </>
  );
}

describe("OnboardingResumeGate", () => {
  afterEach(() => {
    clearPendingSubscriptionCheckout();
    workspaceState = {
      status: "bound",
      boundWorkspace,
      sessionGrant: { accessToken: "test-grant", productAccessAllowed: true },
    };
    getProgress.mockReset();
  });

  it("does not redirect away from subscription checkout while payment is Pending", async () => {
    getProgress.mockResolvedValue(inProgressProgress());

    const router = createMemoryRouter(
      [
        {
          path: "/",
          element: <GateLayout />,
          children: [
            {
              path: "subscription-checkout/:paymentId",
              element: <div>Checkout methods visible</div>,
            },
            { path: "onboarding", element: <div>Onboarding redirected</div> },
          ],
        },
      ],
      { initialEntries: ["/subscription-checkout/payment-1"] },
    );

    render(<RouterProvider router={router} />);

    expect(await screen.findByText("Checkout methods visible")).toBeTruthy();
    await waitFor(() => {
      expect(screen.queryByText("Onboarding redirected")).toBeNull();
    });
    expect(getProgress).not.toHaveBeenCalled();
  });

  it("does not redirect when pending checkout marker is set for the bound org", async () => {
    getProgress.mockResolvedValue(inProgressProgress());
    writePendingSubscriptionCheckout({
      paymentId: "payment-1",
      organizationId: boundWorkspace.organizationId,
    });

    const router = createMemoryRouter(
      [
        {
          path: "/",
          element: <GateLayout />,
          children: [
            { path: "dashboard", element: <div>Dashboard stay</div> },
            { path: "onboarding", element: <div>Onboarding redirected</div> },
          ],
        },
      ],
      { initialEntries: ["/dashboard"] },
    );

    render(<RouterProvider router={router} />);

    expect(await screen.findByText("Dashboard stay")).toBeTruthy();
    await waitFor(() => {
      expect(screen.queryByText("Onboarding redirected")).toBeNull();
    });
    expect(getProgress).not.toHaveBeenCalled();
  });

  it("still resumes onboarding when only a pre-org pending checkout marker exists", async () => {
    getProgress.mockResolvedValue(inProgressProgress());
    writePendingSubscriptionCheckout({
      paymentId: "payment-preorg",
      planKey: "pro",
      billingCycle: "Monthly",
    });

    const router = createMemoryRouter(
      [
        {
          path: "/",
          element: <GateLayout />,
          children: [
            { path: "dashboard", element: <div>Dashboard</div> },
            { path: "onboarding", element: <div>Onboarding redirected</div> },
          ],
        },
      ],
      { initialEntries: ["/dashboard"] },
    );

    render(<RouterProvider router={router} />);

    expect(await screen.findByText("Onboarding redirected")).toBeTruthy();
    expect(getProgress).toHaveBeenCalled();
  });

  it("still redirects incomplete onboarding when no pending checkout", async () => {
    getProgress.mockResolvedValue(inProgressProgress());

    const router = createMemoryRouter(
      [
        {
          path: "/",
          element: <GateLayout />,
          children: [
            { path: "dashboard", element: <div>Dashboard</div> },
            { path: "onboarding", element: <div>Onboarding redirected</div> },
          ],
        },
      ],
      { initialEntries: ["/dashboard"] },
    );

    render(<RouterProvider router={router} />);

    expect(await screen.findByText("Onboarding redirected")).toBeTruthy();
  });

  it("does not redirect completed onboarding", async () => {
    getProgress.mockResolvedValue(completedProgress());

    const router = createMemoryRouter(
      [
        {
          path: "/",
          element: <GateLayout />,
          children: [
            { path: "dashboard", element: <div>Dashboard stay</div> },
            { path: "onboarding", element: <div>Onboarding redirected</div> },
          ],
        },
      ],
      { initialEntries: ["/dashboard"] },
    );

    render(<RouterProvider router={router} />);

    expect(await screen.findByText("Dashboard stay")).toBeTruthy();
    await waitFor(() => {
      expect(screen.queryByText("Onboarding redirected")).toBeNull();
    });
  });
});
