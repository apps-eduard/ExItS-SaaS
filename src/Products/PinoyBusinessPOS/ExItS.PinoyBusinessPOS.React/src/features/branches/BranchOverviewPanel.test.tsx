import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { ToastProvider } from "@/components/exits/ToastProvider";
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

function renderPanel(
  readinessValue: BranchFulfillmentReadinessDto,
  handlers?: Partial<{
    onTogglePickup: (next: boolean) => void;
    onToggleDelivery: (next: boolean) => void;
    onEnableOrdering: () => void;
    onPauseOrders: () => void;
    onResumeOrders: () => void;
  }>,
) {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <BranchOverviewPanel
          readiness={readinessValue}
          busy={false}
          t={t}
          onTogglePickup={handlers?.onTogglePickup ?? vi.fn()}
          onToggleDelivery={handlers?.onToggleDelivery ?? vi.fn()}
          onEnableOrdering={handlers?.onEnableOrdering ?? vi.fn()}
          onPauseOrders={handlers?.onPauseOrders ?? vi.fn()}
          onResumeOrders={handlers?.onResumeOrders ?? vi.fn()}
        />
      </ToastProvider>
    </MemoryRouter>,
  );
}

describe("BranchOverviewPanel", () => {
  it("does not render Still turned off / reason-codes card", () => {
    renderPanel(readiness());
    expect(screen.queryByTestId("branch-reason-codes")).not.toBeInTheDocument();
    expect(screen.queryByText("Still turned off")).not.toBeInTheDocument();
  });

  it("asks for confirmation before enabling pickup", async () => {
    const user = userEvent.setup();
    const onTogglePickup = vi.fn();
    renderPanel(readiness(), { onTogglePickup });

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
    renderPanel(
      readiness({
        customerOrderingEnabled: true,
        pickupEnabled: true,
        onlineOrdersPaused: false,
      }),
      { onPauseOrders },
    );

    await user.click(screen.getByTestId("overview-ordering-switch"));
    expect(onPauseOrders).not.toHaveBeenCalled();
    expect(screen.getByTestId("branch-readiness-toggle-confirm")).toHaveTextContent(
      "Pause Online orders?",
    );

    await user.click(screen.getByTestId("branch-readiness-toggle-confirm-confirm"));
    expect(onPauseOrders).toHaveBeenCalledTimes(1);
  });

  it("shows a centered message when Online is turned on with Pickup and Delivery off", async () => {
    const user = userEvent.setup();
    const onEnableOrdering = vi.fn();
    renderPanel(readiness(), { onEnableOrdering });

    await user.click(screen.getByTestId("overview-ordering-switch"));
    expect(onEnableOrdering).not.toHaveBeenCalled();
    expect(screen.queryByTestId("branch-readiness-toggle-confirm")).not.toBeInTheDocument();
    const toast = screen.getByTestId("exits-toast");
    expect(toast).toHaveTextContent("Turn on Pickup or Delivery before Online orders.");
    expect(toast).toHaveAttribute("data-placement", "center");
  });

  it("asks for confirmation before enabling online orders when pickup is on", async () => {
    const user = userEvent.setup();
    const onEnableOrdering = vi.fn();
    renderPanel(readiness({ pickupEnabled: true }), { onEnableOrdering });

    await user.click(screen.getByTestId("overview-ordering-switch"));
    expect(screen.getByTestId("branch-readiness-toggle-confirm")).toHaveTextContent(
      "Turn on Online orders?",
    );
    await user.click(screen.getByTestId("branch-readiness-toggle-confirm-confirm"));
    expect(onEnableOrdering).toHaveBeenCalledTimes(1);
  });

  it("confirms that online turns off when the last method is turned off", async () => {
    const user = userEvent.setup();
    const onTogglePickup = vi.fn();
    renderPanel(
      readiness({
        pickupEnabled: true,
        deliveryEnabled: false,
        customerOrderingEnabled: true,
      }),
      { onTogglePickup },
    );

    await user.click(screen.getByTestId("overview-pickup-switch"));
    expect(onTogglePickup).not.toHaveBeenCalled();
    expect(screen.getByTestId("branch-readiness-toggle-confirm")).toHaveTextContent(
      "Online orders will be turned off because Pickup and Delivery will both be off.",
    );

    await user.click(screen.getByTestId("branch-readiness-toggle-confirm-confirm"));
    expect(onTogglePickup).toHaveBeenCalledWith(false, true);
  });

  it("cancels without applying the toggle", async () => {
    const user = userEvent.setup();
    const onToggleDelivery = vi.fn();
    renderPanel(readiness({ deliveryEnabled: true }), { onToggleDelivery });

    await user.click(screen.getByTestId("overview-delivery-switch"));
    expect(screen.getByTestId("branch-readiness-toggle-confirm")).toHaveTextContent(
      "Turn off Delivery?",
    );
    await user.click(screen.getByTestId("branch-readiness-toggle-confirm-cancel"));
    expect(onToggleDelivery).not.toHaveBeenCalled();
    expect(screen.queryByTestId("branch-readiness-toggle-confirm")).not.toBeInTheDocument();
  });
});
