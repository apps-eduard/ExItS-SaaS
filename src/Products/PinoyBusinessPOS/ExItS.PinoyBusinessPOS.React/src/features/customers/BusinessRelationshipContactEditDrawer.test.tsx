import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  BusinessRelationshipContactEditDrawer,
  resolveInitialContactSource,
} from "@/features/customers/BusinessRelationshipContactEditDrawer";
import type { BusinessCustomer } from "@/api/pos/pos-connected-suppliers-client";
import * as connectedClient from "@/api/pos/pos-connected-suppliers-client";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

vi.mock("@/api/pos/pos-connected-suppliers-client", async () => {
  const actual = await vi.importActual<typeof connectedClient>(
    "@/api/pos/pos-connected-suppliers-client",
  );
  return {
    ...actual,
    listBusinessCustomerOrganizationContacts: vi.fn(),
    updateBusinessCustomerRelationshipContact: vi.fn(),
  };
});

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "22222222-2222-2222-2222-222222222222",
};

function baseCustomer(overrides: Partial<BusinessCustomer> = {}): BusinessCustomer {
  return {
    connectionId: "33333333-3333-3333-3333-333333333333",
    supplierOrganizationId: workspace.organizationId,
    buyerOrganizationId: "44444444-4444-4444-4444-444444444444",
    organizationDisplayName: "Paul Coffee",
    organizationPublicId: "ORG630729",
    relationshipStatus: "Active",
    catalogSharingMode: "SelectedOnly",
    customerDiscountPercent: null,
    eligibleCount: 0,
    sharedCount: 0,
    excludedCount: 0,
    overrideCount: 0,
    connectedSinceUtc: "2026-09-14T08:00:00Z",
    createdAtUtc: "2026-09-14T07:00:00Z",
    updatedAtUtc: "2026-09-14T08:00:00Z",
    displayNameIsLive: false,
    initiatedByParty: "Supplier",
    actionRequired: false,
    supplierBranchId: null,
    supplierBranchName: null,
    contactSource: "Custom",
    organizationMemberId: null,
    organizationMemberAvailable: null,
    contactPersonName: null,
    contactDepartment: null,
    contactRole: null,
    contactPhone: null,
    contactEmail: null,
    preferredContactMethod: null,
    deliveryInstructions: null,
    billingContactNotes: null,
    internalNotes: null,
    ...overrides,
  };
}

const owner = {
  organizationMemberId: "55555555-5555-5555-5555-555555555555",
  userId: "66666666-6666-6666-6666-666666666666",
  displayName: "Paul Owner",
  roleTitle: "Owner",
  isOwner: true,
  department: null,
  phone: "09171110000",
  email: "paul@paulcoffee.example",
  employeeCode: null,
};

const staff = {
  organizationMemberId: "77777777-7777-7777-7777-777777777777",
  userId: "88888888-8888-8888-8888-888888888888",
  displayName: "Maria Santos",
  roleTitle: "Purchasing Manager",
  isOwner: false,
  department: "Purchasing",
  phone: "09172220000",
  email: "maria@paulcoffee.example",
  employeeCode: null,
};

function renderDrawer(customer: BusinessCustomer) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <ToastProvider>
            <BusinessRelationshipContactEditDrawer
              open
              onClose={() => undefined}
              workspace={workspace}
              customer={customer}
            />
          </ToastProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("resolveInitialContactSource", () => {
  it("defaults Connected empty to Organization staff", () => {
    expect(resolveInitialContactSource(baseCustomer(), true)).toBe("OrganizationMember");
  });

  it("keeps Custom when custom content exists", () => {
    expect(
      resolveInitialContactSource(
        baseCustomer({ contactSource: "Custom", contactPersonName: "External AP" }),
        true,
      ),
    ).toBe("Custom");
  });

  it("forces Custom when Pending", () => {
    expect(
      resolveInitialContactSource(
        baseCustomer({ relationshipStatus: "Pending", contactSource: "OrganizationMember" }),
        false,
      ),
    ).toBe("Custom");
  });
});

describe("BusinessRelationshipContactEditDrawer", () => {
  beforeEach(() => {
    vi.mocked(connectedClient.listBusinessCustomerOrganizationContacts).mockReset();
    vi.mocked(connectedClient.updateBusinessCustomerRelationshipContact).mockReset();
    vi.mocked(connectedClient.listBusinessCustomerOrganizationContacts).mockResolvedValue([
      owner,
      staff,
    ]);
  });

  it("shows searchable staff combobox with Owner and Active staff", async () => {
    const user = userEvent.setup();
    renderDrawer(baseCustomer());

    expect(screen.getByTestId("b2b-contact-source-organization")).toBeInTheDocument();
    expect(screen.queryByTestId("b2b-contact-person")).not.toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByTestId("b2b-staff-combobox-search")).toBeInTheDocument();
    });

    expect(await screen.findByText("Paul Owner")).toBeInTheDocument();
    expect(screen.getByText("Maria Santos")).toBeInTheDocument();
    expect(screen.getByText("Purchasing Manager · Purchasing")).toBeInTheDocument();
    expect(screen.getAllByText("Owner").length).toBeGreaterThanOrEqual(1);

    await user.type(screen.getByTestId("b2b-staff-combobox-search"), "Maria");
    expect(screen.queryByText("Paul Owner")).not.toBeInTheDocument();
    expect(screen.getByText("Maria Santos")).toBeInTheDocument();
  });

  it("fills read-only summary after selecting staff and hides manual identity fields", async () => {
    const user = userEvent.setup();
    renderDrawer(baseCustomer());

    await user.click(await screen.findByTestId(`b2b-org-contact-${staff.organizationMemberId}`));

    const summary = await screen.findByTestId("b2b-selected-organization-contact");
    expect(within(summary).getByText("Maria Santos")).toBeInTheDocument();
    expect(within(summary).getByText("Purchasing Manager · Purchasing")).toBeInTheDocument();
    expect(screen.getByTestId("b2b-selected-phone")).toHaveTextContent("09172220000");
    expect(screen.getByTestId("b2b-selected-email")).toHaveTextContent("maria@paulcoffee.example");
    expect(screen.getByTestId("b2b-preferred-method")).toBeInTheDocument();
    expect(screen.queryByTestId("b2b-contact-person")).not.toBeInTheDocument();
    expect(screen.getByTestId("b2b-change-organization-contact")).toBeInTheDocument();
  });

  it("shows custom editable fields when Custom contact is selected", async () => {
    const user = userEvent.setup();
    renderDrawer(baseCustomer());

    await user.click(screen.getByTestId("b2b-contact-source-custom"));

    expect(screen.getByTestId("b2b-custom-contact-mode")).toBeInTheDocument();
    expect(screen.getByTestId("b2b-contact-person")).toBeInTheDocument();
    expect(screen.getByTestId("b2b-contact-department")).toBeInTheDocument();
    expect(screen.queryByTestId("b2b-organization-contact-picker")).not.toBeInTheDocument();
  });

  it("Pending relationships stay on Custom only", () => {
    renderDrawer(baseCustomer({ relationshipStatus: "Pending" }));

    expect(screen.getByTestId("b2b-contact-pending-custom-only")).toBeInTheDocument();
    expect(screen.queryByTestId("b2b-contact-source-organization")).not.toBeInTheDocument();
    expect(screen.getByTestId("b2b-custom-contact-mode")).toBeInTheDocument();
  });

  it("shows empty CTA when no staff are available", async () => {
    vi.mocked(connectedClient.listBusinessCustomerOrganizationContacts).mockResolvedValue([]);
    const user = userEvent.setup();
    renderDrawer(baseCustomer());

    expect(await screen.findByTestId("b2b-org-contacts-empty")).toBeInTheDocument();
    await user.click(screen.getByTestId("b2b-use-custom-from-empty"));
    expect(screen.getByTestId("b2b-custom-contact-mode")).toBeInTheDocument();
  });

  it("shows API error with Retry and does not fall back to blank manual fields", async () => {
    vi.mocked(connectedClient.listBusinessCustomerOrganizationContacts).mockRejectedValue(
      new Error("staff directory unavailable"),
    );
    const user = userEvent.setup();
    renderDrawer(baseCustomer());

    expect(await screen.findByTestId("b2b-org-contacts-error")).toBeInTheDocument();
    expect(screen.getByTestId("b2b-org-contacts-retry")).toBeInTheDocument();
    expect(screen.queryByTestId("b2b-contact-person")).not.toBeInTheDocument();

    vi.mocked(connectedClient.listBusinessCustomerOrganizationContacts).mockResolvedValue([owner]);
    await user.click(screen.getByTestId("b2b-org-contacts-retry"));
    expect(await screen.findByText("Paul Owner")).toBeInTheDocument();
  });

  it("reloads saved OrganizationMember source into selected summary", async () => {
    renderDrawer(
      baseCustomer({
        contactSource: "OrganizationMember",
        organizationMemberId: staff.organizationMemberId,
        organizationMemberAvailable: true,
        contactPersonName: "Maria Santos",
        contactRole: "Purchasing Manager",
        contactDepartment: "Purchasing",
        contactPhone: "09172220000",
        contactEmail: "maria@paulcoffee.example",
      }),
    );

    expect(await screen.findByTestId("b2b-selected-organization-contact")).toBeInTheDocument();
    expect(screen.queryByTestId("b2b-contact-person")).not.toBeInTheDocument();
  });
});
