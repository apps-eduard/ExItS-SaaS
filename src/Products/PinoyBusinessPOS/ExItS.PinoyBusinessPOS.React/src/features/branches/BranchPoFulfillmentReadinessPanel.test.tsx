import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { BranchPoFulfillmentReadinessPanel } from "@/features/branches/BranchPoFulfillmentReadinessPanel";
import { catalogs } from "@/i18n/messages";

const t = (key: keyof typeof catalogs.en) => catalogs.en[key];

function readiness(
  partial: Partial<BranchFulfillmentReadinessDto>,
): BranchFulfillmentReadinessDto {
  return {
    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    canUseCustomerOrdering: true,
    canUseDelivery: true,
    customerOrderingEnabled: false,
    pickupEnabled: false,
    deliveryEnabled: false,
    onlineOrdersPaused: false,
    onlineOrdersPauseReason: null,
    customerOrderingReady: false,
    pickupReady: false,
    deliveryReady: false,
    customerOrderingOperational: false,
    pickupOperational: false,
    deliveryOperational: false,
    missingRequirements: [],
    reasonCodes: [],
    storeOpenStatus: null,
    storeIsOpenNow: false,
    storeStatusMessage: null,
    branchDetailsComplete: false,
    operatingHoursComplete: false,
    deliveryLocationComplete: false,
    deliveryPolicyComplete: false,
    deliveryAreasComplete: false,
    pickupSectionsComplete: 0,
    pickupSectionsTotal: 2,
    deliverySectionsComplete: 0,
    deliverySectionsTotal: 5,
    ...partial,
  };
}

function renderPanel(partial: Partial<BranchFulfillmentReadinessDto> = {}) {
  return render(
    <MemoryRouter>
      <BranchPoFulfillmentReadinessPanel
        branchId="bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
        readiness={readiness(partial)}
        t={t}
        catalogOk={false}
        paymentsOk={false}
        contactOk={false}
      />
    </MemoryRouter>,
  );
}

describe("BranchPoFulfillmentReadinessPanel", () => {
  it("both methods OFF → Setup required without delivery detail warnings", () => {
    renderPanel();
    expect(screen.getByTestId("branch-po-fulfillment-status")).toHaveTextContent(
      "Setup required",
    );
    expect(screen.getByTestId("branch-po-fulfillment-item-enableMethod")).toBeInTheDocument();
    expect(screen.getByTestId("branch-po-fulfillment-item-branchInfo")).toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-item-deliveryProgress")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-methods")).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-po-fulfillment-configure")).toBeInTheDocument();
    expect(screen.getByTestId("branch-po-supplier-summary-view")).toHaveAttribute(
      "href",
      "/customers?kind=businesses",
    );
    expect(screen.getByTestId("po-supplier-summary-payments")).toHaveAttribute(
      "href",
      "/org/payment-methods",
    );
  });

  it("Pickup ready only → Ready with compact channel chips (no duplicate copy)", () => {
    renderPanel({
      pickupEnabled: true,
      pickupReady: true,
      branchDetailsComplete: true,
      pickupSectionsComplete: 2,
    });
    expect(screen.getByTestId("branch-po-fulfillment-panel")).toHaveAttribute(
      "data-ready",
      "true",
    );
    expect(screen.getByTestId("branch-po-fulfillment-status")).toHaveTextContent("Ready");
    expect(screen.queryByTestId("branch-po-fulfillment-checklist")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-ready-note")).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-po-fulfillment-method-pickup")).toHaveTextContent(
      /Pickup · Ready · 2\/2/,
    );
    expect(screen.getByTestId("branch-po-fulfillment-method-delivery")).toHaveTextContent(
      /Delivery · Off/,
    );
    expect(screen.getByTestId("po-supplier-summary-fulfillment")).toHaveTextContent("Ready");
  });

  it("configure scrolls toward fulfillment toggles", async () => {
    const user = userEvent.setup();
    const onConfigure = vi.fn();
    const scrollIntoView = vi.fn();
    Element.prototype.scrollIntoView = scrollIntoView;

    render(
      <MemoryRouter>
        <div data-testid="branch-fulfillment-toggles" />
        <BranchPoFulfillmentReadinessPanel
          branchId="bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
          readiness={readiness()}
          t={t}
          onConfigure={onConfigure}
        />
      </MemoryRouter>,
    );

    await user.click(screen.getByTestId("branch-po-fulfillment-configure"));
    expect(onConfigure).toHaveBeenCalled();
    expect(scrollIntoView).toHaveBeenCalled();
  });
});
