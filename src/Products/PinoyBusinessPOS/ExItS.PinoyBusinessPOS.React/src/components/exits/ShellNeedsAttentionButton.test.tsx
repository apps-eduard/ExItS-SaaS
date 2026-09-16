import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { ShellNeedsAttentionButton } from "@/components/exits/ShellNeedsAttentionButton";
import {
  buildNeedsAttentionAlerts,
  formatNeedsAttentionBadge,
  groupNeedsAttentionAlerts,
} from "@/features/shell/needs-attention";

vi.mock("@/features/shell/useNeedsAttentionAlerts", () => ({
  useNeedsAttentionAlerts: () => ({
    alerts: [],
    groups: [],
    count: 0,
    badge: null,
    isLoading: false,
    isFetching: false,
  }),
}));

vi.mock("@/hooks/useMediaQuery", () => ({
  useMediaMin: () => true,
}));

function renderButton(
  overrides: Partial<Parameters<typeof ShellNeedsAttentionButton>[0]> = {},
) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/"]}>
        <Routes>
          <Route
            path="/"
            element={<ShellNeedsAttentionButton testId="org-needs-attention" {...overrides} />}
          />
          <Route path="/inventory" element={<div data-testid="inventory-page" />} />
          <Route
            path="/inventory/expiration"
            element={<div data-testid="expiration-page" />}
          />
          <Route
            path="/customers/business/:id"
            element={<div data-testid="business-customer-page" />}
          />
          <Route
            path="/org/branches/:branchId/fulfillment"
            element={<div data-testid="fulfillment-page" />}
          />
          <Route path="/org/payment-methods" element={<div data-testid="payments-page" />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("ShellNeedsAttentionButton", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("keeps the icon visible with All clear empty state when count is zero", async () => {
    const user = userEvent.setup();
    renderButton({ countOverride: 0, groupsOverride: [], badgeOverride: null });

    const trigger = screen.getByTestId("org-needs-attention");
    expect(trigger).toBeInTheDocument();
    expect(trigger).toHaveAttribute("data-has-issues", "false");
    expect(screen.queryByTestId("org-needs-attention-badge")).not.toBeInTheDocument();
    expect(screen.getByTestId("org-needs-attention-icon")).toHaveAttribute(
      "data-emphasis",
      "neutral",
    );

    await user.click(trigger);

    const panel = await screen.findByTestId("org-needs-attention-panel");
    expect(within(panel).getByTestId("org-needs-attention-empty")).toBeInTheDocument();
    expect(within(panel).getByText("All clear")).toBeInTheDocument();
    expect(within(panel).getByText("No items currently need attention.")).toBeInTheDocument();
  });

  it("shows grouped dropdown with aggregated inventory alerts and deep-links", async () => {
    const user = userEvent.setup();
    const alerts = buildNeedsAttentionAlerts({
      inventory: {
        lowStockProductCount: 8,
        outOfStockProductCount: 0,
        expiredLotCount: 0,
        nearExpiryLotCount: 3,
      },
      commerce: {
        incompleteSupplierConnectionIds: ["cccccccc-cccc-cccc-cccc-cccccccccccc"],
        paymentSetupIncomplete: true,
      },
      branch: {
        branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        deliveryEnabled: true,
        deliveryReady: false,
        deliveryLocationComplete: true,
        deliveryPolicyComplete: false,
        deliveryAreasComplete: true,
        pickupEnabled: true,
        pickupReady: false,
        branchDetailsComplete: false,
      },
    });
    const groups = groupNeedsAttentionAlerts(alerts);

    renderButton({
      countOverride: alerts.length,
      badgeOverride: formatNeedsAttentionBadge(alerts.length),
      groupsOverride: groups,
    });

    const trigger = screen.getByTestId("org-needs-attention");
    expect(trigger).toBeInTheDocument();
    expect(trigger).toHaveAttribute("data-has-issues", "true");
    expect(screen.getByTestId("org-needs-attention-badge")).toHaveTextContent(
      String(alerts.length),
    );
    expect(screen.getByTestId("org-needs-attention-icon")).toHaveAttribute(
      "data-emphasis",
      "warning",
    );

    await user.click(trigger);

    const panel = await screen.findByTestId("org-needs-attention-panel");
    expect(within(panel).getByText("Needs attention")).toBeInTheDocument();
    expect(screen.queryByTestId("org-needs-attention-empty")).not.toBeInTheDocument();
    expect(screen.getByTestId("org-needs-attention-group-inventory")).toBeInTheDocument();
    expect(
      screen.getByTestId("org-needs-attention-group-connectedCommerce"),
    ).toBeInTheDocument();
    expect(
      screen.getByTestId("org-needs-attention-group-branchFulfillment"),
    ).toBeInTheDocument();

    const lowStock = screen.getByTestId("needs-attention-low-stock");
    expect(lowStock).toHaveAttribute("href", "/inventory?lowStock=1");
    expect(lowStock).toHaveTextContent("Low stock");
    expect(lowStock).toHaveTextContent("· 8");

    const expiry = screen.getByTestId("needs-attention-expiring");
    expect(expiry).toHaveAttribute("href", "/inventory/expiration");
    expect(expiry).toHaveTextContent("· 3");

    expect(screen.getByTestId("needs-attention-supplier-readiness")).toHaveAttribute(
      "href",
      "/customers/business/cccccccc-cccc-cccc-cccc-cccccccccccc",
    );
    expect(screen.getByTestId("needs-attention-payment-setup")).toHaveAttribute(
      "href",
      "/org/payment-methods",
    );
    expect(screen.getByTestId("needs-attention-delivery")).toHaveAttribute(
      "href",
      "/org/branches/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/fulfillment?tab=policy",
    );
    expect(screen.getByTestId("needs-attention-pickup")).toHaveAttribute(
      "href",
      "/org/branches/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/fulfillment",
    );
    expect(screen.getByTestId("needs-attention-branch-info")).toHaveAttribute(
      "href",
      "/org/branches/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/fulfillment?tab=details",
    );

    await user.click(lowStock);
    expect(await screen.findByTestId("inventory-page")).toBeInTheDocument();
  });
});
