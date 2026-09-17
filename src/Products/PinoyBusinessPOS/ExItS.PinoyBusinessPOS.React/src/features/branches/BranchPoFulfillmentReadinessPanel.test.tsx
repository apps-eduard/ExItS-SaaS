import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { BranchPoFulfillmentReadinessPanel } from "@/features/branches/BranchPoFulfillmentReadinessPanel";
import { catalogs } from "@/i18n/messages";

const t = (key: keyof typeof catalogs.en) => catalogs.en[key];

function readiness(
  partial: Partial<BranchFulfillmentReadinessDto> = {},
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
  it("both methods OFF → Setup required with why-required explanation", () => {
    renderPanel();
    expect(screen.getByTestId("branch-po-fulfillment-status")).toHaveTextContent(
      "Setup required",
    );
    expect(screen.getByTestId("branch-po-fulfillment-why-required")).toHaveTextContent(
      "Why is this required?",
    );
    expect(screen.getByTestId("branch-po-fulfillment-why-required")).toHaveTextContent(
      /at least one active fulfillment method/,
    );
    expect(screen.queryByTestId("branch-po-fulfillment-item-enableMethod")).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-po-fulfillment-item-branchInfo")).toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-item-deliveryProgress")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-methods")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-configure")).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-po-supplier-summary-view")).toHaveAttribute(
      "href",
      "/customers?kind=businesses",
    );
    expect(screen.getByTestId("po-supplier-summary-payments")).toHaveAttribute(
      "href",
      "/org/payment-methods",
    );
  });

  it("configured + both methods OFF → why-required only (no configured-but-off copy)", () => {
    renderPanel({
      branchDetailsComplete: true,
      pickupSectionsComplete: 2,
      deliverySectionsComplete: 5,
    });
    expect(screen.getByTestId("branch-po-fulfillment-status")).toHaveTextContent(
      "Setup required",
    );
    expect(screen.queryByTestId("branch-po-fulfillment-configured-but-off")).not.toBeInTheDocument();
    expect(
      screen.queryByText(
        "Configuration complete. Enable Pickup or Delivery to activate PO fulfillment.",
      ),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-po-fulfillment-why-required")).toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-item-enableMethod")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-checklist")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-configure")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-item-branchInfo")).not.toBeInTheDocument();
    expect(screen.queryByText("Setup requirements look complete.")).not.toBeInTheDocument();
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
    expect(screen.queryByTestId("branch-po-fulfillment-why-required")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-ready-note")).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-po-fulfillment-method-pickup")).toHaveTextContent(
      /Pickup · Ready · 2\/2/,
    );
    expect(screen.getByTestId("branch-po-fulfillment-method-delivery")).toHaveTextContent(
      /Delivery · Off/,
    );
    expect(screen.getByTestId("po-supplier-summary-fulfillment")).toHaveTextContent("Ready");
  });

  it("Delivery ready only → Ready", () => {
    renderPanel({
      deliveryEnabled: true,
      deliveryReady: true,
      branchDetailsComplete: true,
      deliverySectionsComplete: 5,
    });
    expect(screen.getByTestId("branch-po-fulfillment-panel")).toHaveAttribute(
      "data-ready",
      "true",
    );
    expect(screen.getByTestId("branch-po-fulfillment-status")).toHaveTextContent("Ready");
    expect(screen.getByTestId("branch-po-fulfillment-method-delivery")).toHaveTextContent(
      /Delivery · Ready/,
    );
    expect(screen.getByTestId("branch-po-fulfillment-method-pickup")).toHaveTextContent(
      /Pickup · Off/,
    );
  });

  it("Online orders ON alone does not make PO fulfillment Ready", () => {
    renderPanel({
      branchDetailsComplete: true,
      customerOrderingEnabled: true,
      customerOrderingReady: true,
      customerOrderingOperational: true,
      pickupSectionsComplete: 2,
      deliverySectionsComplete: 5,
    });
    expect(screen.getByTestId("branch-po-fulfillment-panel")).toHaveAttribute(
      "data-ready",
      "false",
    );
    expect(screen.getByTestId("branch-po-fulfillment-status")).toHaveTextContent(
      "Setup required",
    );
    expect(screen.queryByTestId("branch-po-fulfillment-item-enableMethod")).not.toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-configure")).not.toBeInTheDocument();
  });

  it("Branch details CTA remains when details are incomplete", () => {
    renderPanel({ branchDetailsComplete: false });
    expect(screen.getByTestId("branch-po-fulfillment-open-details")).toBeInTheDocument();
    expect(screen.queryByTestId("branch-po-fulfillment-configure")).not.toBeInTheDocument();
  });
});
