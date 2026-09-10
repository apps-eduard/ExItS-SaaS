import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppProviders } from "@/app/providers";
import {
  CheckoutCustomerDirectory,
  CheckoutCustomerSelectedCard,
} from "@/features/checkout/CheckoutCustomerDirectory";
import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import type { CustomerListConnectionOverlay } from "@/features/customers/customer-list-connection";

const walkIn: CheckoutCustomerOption = {
  kind: "Customer",
  customerId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
  displayName: "Local Walkin 20260826230002",
  mobileNumber: "09171110001",
  status: "Active",
};

const named: CheckoutCustomerOption = {
  kind: "Customer",
  customerId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  displayName: "Juan Dela Cruz",
  mobileNumber: "09171234567",
  status: "Active",
};

function renderDirectory(
  customers: CheckoutCustomerOption[],
  options: {
    search?: string;
    selected?: CheckoutCustomerOption | null;
    overlay?: CustomerListConnectionOverlay | null;
    onSelect?: (customer: CheckoutCustomerOption) => void;
    onSearchChange?: (value: string) => void;
    includeWalkInsWhenIdle?: boolean;
  } = {},
) {
  const onSelect = options.onSelect ?? vi.fn();
  const onSearchChange = options.onSearchChange ?? vi.fn();
  render(
    <AppProviders>
      <CheckoutCustomerDirectory
        searchId="checkout-customer-search"
        searchTestId="checkout-customer-search"
        searchLabel="Search customers"
        searchValue={options.search ?? ""}
        onSearchChange={onSearchChange}
        customers={customers}
        customersLoading={false}
        selectedCustomer={options.selected ?? null}
        overlay={options.overlay ?? null}
        onSelect={onSelect}
        includeWalkInsWhenIdle={options.includeWalkInsWhenIdle}
      />
    </AppProviders>,
  );
  return { onSelect, onSearchChange };
}

describe("CheckoutCustomerDirectory", () => {
  it("hides Local Validation walk-in seeds until the cashier searches", () => {
    renderDirectory([walkIn, named]);

    expect(screen.getByTestId(`checkout-customer-${named.customerId}`)).toHaveTextContent(
      "Juan Dela Cruz",
    );
    expect(screen.getByTestId(`checkout-customer-${named.customerId}`)).toHaveTextContent(
      "09171234567",
    );
    expect(screen.queryByTestId(`checkout-customer-${walkIn.customerId}`)).not.toBeInTheDocument();
  });

  it("shows walk-in seeds when idle browse is required for Utang", () => {
    renderDirectory([walkIn, named], { includeWalkInsWhenIdle: true });

    expect(screen.getByTestId(`checkout-customer-${walkIn.customerId}`)).toHaveTextContent(
      "Walk-in",
    );
    expect(screen.getByTestId(`checkout-customer-${named.customerId}`)).toBeInTheDocument();
  });

  it("shows walk-ins as Walk-in plus phone when searching", () => {
    renderDirectory([walkIn, named], { search: "0917" });

    expect(screen.getByTestId(`checkout-customer-${walkIn.customerId}`)).toHaveTextContent(
      "Walk-in",
    );
    expect(screen.getByTestId(`checkout-customer-${walkIn.customerId}`)).toHaveTextContent(
      "09171110001",
    );
    expect(screen.getByTestId(`checkout-customer-${walkIn.customerId}`)).not.toHaveTextContent(
      "20260826230002",
    );
  });

  it("shows No ExItS ID versus ExItS ID, and Connected only from the overlay", () => {
    const platformId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
    const linked: CheckoutCustomerOption = {
      kind: "Customer",
      customerId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
      displayName: "Rosa Santos",
      status: "Active",
      linkedPersonalPublicUserId: "EX-4827-1936",
      platformBusinessCustomerId: platformId,
    };
    const overlay: CustomerListConnectionOverlay = {
      connectedBusinessCustomerIds: new Set([platformId]),
      pendingBusinessCustomerIds: new Set(),
      loaded: true,
    };

    renderDirectory([named, linked], { overlay });

    expect(
      screen.getByTestId(`checkout-customer-${named.customerId}`).querySelector(
        "[data-testid='customer-list-badge-no-exits']",
      ),
    ).toHaveTextContent("No ExItS ID");
    expect(
      screen.getByTestId(`checkout-customer-${linked.customerId}`).querySelector(
        "[data-testid='customer-list-badge-exits-id']",
      ),
    ).toHaveTextContent("ExItS ID");
    expect(
      screen.getByTestId(`checkout-customer-${linked.customerId}`).querySelector(
        "[data-testid='customer-list-badge-connected']",
      ),
    ).toHaveTextContent("Connected");
    expect(
      screen.queryByTestId(`checkout-customer-${named.customerId}`)?.querySelector(
        "[data-testid='customer-list-badge-connected']",
      ),
    ).not.toBeInTheDocument();
  });

  it("shows Pending when the overlay lists a pending request", () => {
    const platformId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
    const linked: CheckoutCustomerOption = {
      kind: "Customer",
      customerId: "ffffffff-ffff-4fff-8fff-ffffffffffff",
      displayName: "Pending Person",
      status: "Active",
      linkedPersonalPublicUserId: "EX-1111-2222",
      platformBusinessCustomerId: platformId,
    };

    renderDirectory([linked], {
      overlay: {
        connectedBusinessCustomerIds: new Set(),
        pendingBusinessCustomerIds: new Set([platformId]),
        loaded: true,
      },
    });

    expect(screen.getByTestId("customer-list-badge-pending")).toHaveTextContent("Pending");
    expect(screen.queryByTestId("customer-list-badge-connected")).not.toBeInTheDocument();
  });

  it("shows Utang credit overlay without hiding ineligible people", () => {
    const pending: CheckoutCustomerOption = {
      kind: "Customer",
      customerId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
      displayName: "Maria Santos",
      status: "Active",
      creditStatus: "PendingApproval",
    };
    const approved: CheckoutCustomerOption = {
      ...named,
      creditStatus: "Approved",
      availableCredit: 12500,
    };
    const paused: CheckoutCustomerOption = {
      kind: "Customer",
      customerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      displayName: "Ana Cruz",
      status: "Active",
      creditStatus: "Disabled",
    };
    const notEnabled: CheckoutCustomerOption = {
      kind: "Customer",
      customerId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
      displayName: "Pedro Reyes",
      status: "Active",
      creditStatus: "NotConfigured",
    };

    render(
      <AppProviders>
        <CheckoutCustomerDirectory
          searchId="checkout-customer-search"
          searchTestId="checkout-customer-search"
          searchLabel="Search customers"
          searchValue=""
          onSearchChange={vi.fn()}
          customers={[approved, pending, paused, notEnabled]}
          customersLoading={false}
          selectedCustomer={null}
          onSelect={vi.fn()}
          includeWalkInsWhenIdle
          showCreditStatus
        />
      </AppProviders>,
    );

    expect(screen.getByTestId("checkout-credit-directory")).toBeInTheDocument();
    expect(screen.getByTestId(`checkout-customer-${approved.customerId}`)).toHaveTextContent(
      "Approved",
    );
    expect(screen.getByTestId(`checkout-customer-${pending.customerId}`)).toHaveTextContent(
      "Pending approval",
    );
    expect(screen.getByTestId(`checkout-customer-${paused.customerId}`)).toHaveTextContent(
      "Paused",
    );
    expect(screen.getByTestId(`checkout-customer-${notEnabled.customerId}`)).toHaveTextContent(
      "Credit not enabled",
    );
    expect(screen.getAllByTestId("checkout-customer-credit-line")).toHaveLength(4);
  });

  it("shows load error instead of empty when the directory request failed", () => {
    render(
      <AppProviders>
        <CheckoutCustomerDirectory
          searchId="checkout-customer-search"
          searchTestId="checkout-customer-search"
          searchLabel="Search customers"
          searchValue=""
          onSearchChange={vi.fn()}
          customers={[]}
          customersLoading={false}
          customersError
          onRetryLoad={vi.fn()}
          selectedCustomer={null}
          onSelect={vi.fn()}
        />
      </AppProviders>,
    );

    expect(screen.getByTestId("checkout-customer-load-error")).toBeInTheDocument();
    expect(screen.queryByTestId("checkout-customer-empty")).not.toBeInTheDocument();
    expect(screen.getByTestId("checkout-customer-retry")).toBeInTheDocument();
  });
});

describe("CheckoutCustomerSelectedCard", () => {
  it("lets the cashier check the scanned Personal name instead of the seed label", () => {
    render(
      <AppProviders>
        <CheckoutCustomerSelectedCard
          customer={{
            ...walkIn,
            resolvedPersonalDisplayName: "Rosa Santos",
            linkedPersonalPublicUserId: "EX-4827-1936",
          }}
          onClear={vi.fn()}
        />
      </AppProviders>,
    );

    expect(screen.getByTestId("checkout-customer-selected")).toHaveTextContent("Rosa Santos");
    expect(screen.getByTestId("checkout-customer-selected")).toHaveTextContent("09171110001");
    expect(screen.getByTestId("customer-list-badge-exits-id")).toHaveTextContent("ExItS ID");
    expect(screen.queryByTestId("customer-list-badge-connected")).not.toBeInTheDocument();
    expect(screen.getByTestId("checkout-customer-selected")).not.toHaveTextContent(
      "Local Walkin 20260826230002",
    );
  });

  it("clears the checked customer", async () => {
    const user = userEvent.setup();
    const onClear = vi.fn();
    render(
      <AppProviders>
        <CheckoutCustomerSelectedCard customer={named} onClear={onClear} />
      </AppProviders>,
    );

    await user.click(screen.getByTestId("checkout-customer-clear"));
    expect(onClear).toHaveBeenCalledTimes(1);
  });
});
