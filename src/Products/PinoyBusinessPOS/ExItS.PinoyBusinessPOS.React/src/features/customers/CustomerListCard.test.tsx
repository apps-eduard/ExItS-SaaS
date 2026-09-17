import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import {
  CustomerListCard,
  resolveAbnormalAccountStatus,
  resolveBusinessRelationshipStatus,
  resolvePeopleRelationshipStatus,
  relationshipStatusTone,
} from "@/features/customers/CustomerListCard";

describe("CustomerListCard hierarchy", () => {
  it("Personal + Pending + Active shows Personal + Pending only (no Active)", () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <CustomerListCard
            href="/customers/a"
            testId="customer-row-a"
            name="Kizy Uy"
            kind="personal"
            relationshipStatus="Pending"
            accountStatus="Active"
            exitsId="EX-0456-4139"
            exitsIdTestId="customer-exits-id-a"
          />
        </MemoryRouter>
      </AppProviders>,
    );

    const card = screen.getByTestId("customer-row-a");
    expect(card).toHaveTextContent("Kizy Uy");
    expect(card).toHaveTextContent("Personal");
    expect(screen.getByTestId("customer-list-card-connection")).toHaveTextContent("Pending");
    expect(screen.getByTestId("customer-list-card-connection").querySelector("[data-tone]")).toHaveAttribute(
      "data-tone",
      "warning",
    );
    expect(screen.queryByTestId("customer-list-card-account")).not.toBeInTheDocument();
    expect(card).not.toHaveTextContent("Active");
    expect(screen.getByTestId("customer-exits-id-a")).toHaveTextContent("EX-0456-4139");
    expect(screen.getByTestId("customer-list-card-pending-hint")).toHaveTextContent(
      "Waiting for Kizy Uy to accept.",
    );
  });

  it("B2B + Connected + Active shows B2B + Connected only", () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <CustomerListCard
            href="/customers/business/b"
            testId="business-customer-row-b"
            name="Paul coffee"
            nameTestId="business-customer-name-b"
            kind="b2b"
            relationshipStatus="Connected"
            accountStatus="Active"
            exitsId="ORG630729"
            exitsIdTestId="business-customer-org-b"
          />
        </MemoryRouter>
      </AppProviders>,
    );

    expect(screen.getByTestId("business-customer-name-b")).toHaveTextContent("Paul coffee");
    expect(screen.getByTestId("business-customer-row-b")).toHaveTextContent("B2B");
    expect(screen.getByTestId("customer-list-card-connection")).toHaveTextContent("Connected");
    expect(screen.getByTestId("customer-list-card-connection").querySelector("[data-tone]")).toHaveAttribute(
      "data-tone",
      "success",
    );
    expect(screen.queryByTestId("customer-list-card-account")).not.toBeInTheDocument();
    expect(screen.getByTestId("business-customer-row-b")).not.toHaveTextContent("Active");
    expect(screen.queryByTestId("customer-list-card-pending-hint")).not.toBeInTheDocument();
  });

  it("Pending + Suspended shows both chips and account helper", () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <CustomerListCard
            href="/customers/a"
            testId="customer-row-suspended"
            name="Kizy Uy"
            kind="personal"
            relationshipStatus="Pending"
            accountStatus="Suspended"
            exitsId="EX-0456-4139"
          />
        </MemoryRouter>
      </AppProviders>,
    );

    expect(screen.getByTestId("customer-list-card-connection")).toHaveTextContent("Pending");
    expect(screen.getByTestId("customer-list-card-account")).toHaveTextContent("Suspended");
    expect(screen.getByTestId("customer-list-card-account").querySelector("[data-tone]")).toHaveAttribute(
      "data-tone",
      "warning",
    );
    expect(screen.getByTestId("customer-list-card-account-hint")).toHaveTextContent(
      "This account is suspended.",
    );
    expect(screen.getByTestId("customer-list-card-account-hint")).toHaveTextContent(
      "Connection actions are temporarily unavailable.",
    );
    expect(screen.queryByTestId("customer-list-card-pending-hint")).not.toBeInTheDocument();
  });

  it("Connected + Suspended shows Connected + Suspended with business helper", () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <CustomerListCard
            href="/customers/business/b"
            testId="business-customer-row-suspended"
            name="Paul coffee"
            kind="b2b"
            relationshipStatus="Connected"
            accountStatus="Suspended"
            exitsId="ORG630729"
          />
        </MemoryRouter>
      </AppProviders>,
    );

    expect(screen.getByTestId("customer-list-card-connection")).toHaveTextContent("Connected");
    expect(screen.getByTestId("customer-list-card-account")).toHaveTextContent("Suspended");
    expect(screen.getByTestId("customer-list-card-account-hint")).toHaveTextContent("Account suspended.");
    expect(screen.getByTestId("customer-list-card-account-hint")).toHaveTextContent(
      "New transactions are unavailable until the account is restored.",
    );
  });

  it("Disabled remains visible with danger tone", () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <CustomerListCard
            href="/customers/a"
            testId="customer-row-disabled"
            name="Kizy Uy"
            kind="personal"
            relationshipStatus="Connected"
            accountStatus="Disabled"
            exitsId="EX-0456-4139"
          />
        </MemoryRouter>
      </AppProviders>,
    );

    expect(screen.getByTestId("customer-list-card-account")).toHaveTextContent("Disabled");
    expect(screen.getByTestId("customer-list-card-account").querySelector("[data-tone]")).toHaveAttribute(
      "data-tone",
      "danger",
    );
    expect(screen.getByTestId("customer-list-card-account-hint")).toHaveTextContent(
      "This account is disabled.",
    );
  });

  it("keeps detail navigation href", () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <CustomerListCard
            href="/customers/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"
            testId="customer-row-nav"
            name="Rosa"
            kind="personal"
            relationshipStatus="Inactive"
            accountStatus="Active"
            exitsId={null}
          />
        </MemoryRouter>
      </AppProviders>,
    );

    expect(screen.getByTestId("customer-row-nav")).toHaveAttribute(
      "href",
      "/customers/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    );
  });
});

describe("customer list status helpers", () => {
  it("maps relationship and account independently", () => {
    expect(resolveBusinessRelationshipStatus("Active")).toBe("Connected");
    expect(resolveBusinessRelationshipStatus("Pending")).toBe("Pending");
    expect(resolveBusinessRelationshipStatus("Disconnected")).toBe("Inactive");
    expect(resolveBusinessRelationshipStatus("Declined")).toBe("Declined");
    expect(resolveAbnormalAccountStatus("Active")).toBeNull();
    expect(resolveAbnormalAccountStatus("Suspended")).toBe("Suspended");
    expect(resolveAbnormalAccountStatus("Disabled")).toBe("Disabled");
    expect(relationshipStatusTone("Declined")).toBe("danger");
    expect(relationshipStatusTone("Inactive")).toBe("neutral");
  });

  it("maps people overlay without treating ExItS ID alone as Connected", () => {
    expect(
      resolvePeopleRelationshipStatus(
        {
          linkedPersonalPublicUserId: "EX-1111-2222",
          platformBusinessCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        },
        {
          connectedBusinessCustomerIds: new Set(),
          pendingBusinessCustomerIds: new Set(["dddddddd-dddd-4ddd-8ddd-dddddddddddd"]),
          loaded: true,
        },
      ),
    ).toBe("Pending");

    expect(
      resolvePeopleRelationshipStatus(
        {
          linkedPersonalPublicUserId: "EX-1111-2222",
          platformBusinessCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        },
        null,
      ),
    ).toBe("Inactive");
  });
});
