import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { BranchOverviewPanel } from "@/features/branches/BranchOverviewPanel";
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
    customerOrderingReady: true,
    pickupReady: true,
    deliveryReady: true,
    customerOrderingOperational: false,
    pickupOperational: false,
    deliveryOperational: false,
    missingRequirements: [],
    reasonCodes: ["PickupDisabled"],
    storeOpenStatus: null,
    storeIsOpenNow: false,
    storeStatusMessage: null,
    branchDetailsComplete: true,
    operatingHoursComplete: true,
    deliveryLocationComplete: true,
    deliveryPolicyComplete: true,
    deliveryAreasComplete: true,
    pickupSectionsComplete: 2,
    pickupSectionsTotal: 2,
    deliverySectionsComplete: 5,
    deliverySectionsTotal: 5,
    ...partial,
  };
}

describe("BranchOverviewPanel", () => {
  it("does not render Still turned off / reason-codes card", () => {
    render(
      <BranchOverviewPanel
        readiness={readiness()}
        busy={false}
        t={t}
        onTogglePickup={vi.fn()}
        onToggleDelivery={vi.fn()}
        onEnableOrdering={vi.fn()}
        onPauseOrders={vi.fn()}
        onResumeOrders={vi.fn()}
      />,
    );
    expect(screen.queryByTestId("branch-reason-codes")).not.toBeInTheDocument();
    expect(screen.queryByText("Still turned off")).not.toBeInTheDocument();
  });

  it("asks for confirmation before enabling pickup", async () => {
    const user = userEvent.setup();
    const onTogglePickup = vi.fn();
    render(
      <BranchOverviewPanel
        readiness={readiness()}
        busy={false}
        t={t}
        onTogglePickup={onTogglePickup}
        onToggleDelivery={vi.fn()}
        onEnableOrdering={vi.fn()}
        onPauseOrders={vi.fn()}
        onResumeOrders={vi.fn()}
      />,
    );

    await user.click(screen.getByTestId("overview-pickup-switch"));
    expect(onTogglePickup).not.toHaveBeenCalled();
    expect(screen.getByTestId("branch-readiness-toggle-confirm")).toHaveTextContent(
      "Turn on Pickup?",
    );

    await user.click(screen.getByTestId("branch-readiness-toggle-confirm-confirm"));
    expect(onTogglePickup).toHaveBeenCalledWith(true);
  });

  it("asks for confirmation before pausing online orders", async () => {
    const user = userEvent.setup();
    const onPauseOrders = vi.fn();
    render(
      <BranchOverviewPanel
        readiness={readiness({
          customerOrderingEnabled: true,
          onlineOrdersPaused: false,
        })}
        busy={false}
        t={t}
        onTogglePickup={vi.fn()}
        onToggleDelivery={vi.fn()}
        onEnableOrdering={vi.fn()}
        onPauseOrders={onPauseOrders}
        onResumeOrders={vi.fn()}
      />,
    );

    await user.click(screen.getByTestId("overview-ordering-switch"));
    expect(onPauseOrders).not.toHaveBeenCalled();
    expect(screen.getByTestId("branch-readiness-toggle-confirm")).toHaveTextContent(
      "Pause Online orders?",
    );

    await user.click(screen.getByTestId("branch-readiness-toggle-confirm-confirm"));
    expect(onPauseOrders).toHaveBeenCalledTimes(1);
  });

  it("cancels without applying the toggle", async () => {
    const user = userEvent.setup();
    const onToggleDelivery = vi.fn();
    render(
      <BranchOverviewPanel
        readiness={readiness({ deliveryEnabled: true })}
        busy={false}
        t={t}
        onTogglePickup={vi.fn()}
        onToggleDelivery={onToggleDelivery}
        onEnableOrdering={vi.fn()}
        onPauseOrders={vi.fn()}
        onResumeOrders={vi.fn()}
      />,
    );

    await user.click(screen.getByTestId("overview-delivery-switch"));
    expect(screen.getByTestId("branch-readiness-toggle-confirm")).toHaveTextContent(
      "Turn off Delivery?",
    );
    await user.click(screen.getByTestId("branch-readiness-toggle-confirm-cancel"));
    expect(onToggleDelivery).not.toHaveBeenCalled();
    expect(screen.queryByTestId("branch-readiness-toggle-confirm")).not.toBeInTheDocument();
  });
});
