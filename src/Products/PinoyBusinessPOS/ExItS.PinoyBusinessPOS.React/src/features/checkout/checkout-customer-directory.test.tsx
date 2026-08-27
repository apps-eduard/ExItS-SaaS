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
  customerId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
  displayName: "Local Walkin 20260826230002",
  mobileNumber: "09171110001",
  status: "Active",
};

const named: CheckoutCustomerOption = {
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
