import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { AppProviders } from "@/app/providers";
import { ContactIdentityProfile } from "@/features/personal/ContactIdentityProfile";
import type { PeopleRowModel } from "@/features/personal/people-status";
import type { PersonalContactDto } from "@/api/platform/personal-types";

const contact: PersonalContactDto = {
  id: "c-id",
  displayName: "Very Long Contact Name That Should Wrap Nicely",
  resolvedUserIdentityId: null,
  resolvedPublicUserId: null,
  linkedUserIdentityId: null,
  status: "Active",
  createdAtUtc: "2026-08-25T00:00:00.000Z",
};

function row(overrides?: Partial<PeopleRowModel>): PeopleRowModel {
  return {
    contact,
    connectionStatus: "local",
    identityLine: "local",
    previousClosed: [],
    lentTotal: 0,
    lentCount: 0,
    oweTotal: 0,
    oweCount: 0,
    ...overrides,
  };
}

describe("ContactIdentityProfile", () => {
  it("shows only the kind badge when there is no outstanding balance", () => {
    render(
      <AppProviders>
        <ContactIdentityProfile
          row={row()}
          statusFallback="Local contact"
          statusId="status"
          testId="profile"
        />
      </AppProviders>,
    );

    expect(screen.getByTestId("profile-kind")).toHaveTextContent("Local Contact");
    expect(screen.queryByTestId("profile-direction")).not.toBeInTheDocument();
    expect(screen.queryByTestId("profile-amount")).not.toBeInTheDocument();
    expect(screen.queryByTestId("profile-due")).not.toBeInTheDocument();
  });

  it("stacks I Lent, amount, and hides non-overdue due", () => {
    render(
      <AppProviders>
        <ContactIdentityProfile
          row={row({
            utang: {
              relationshipId: "rel-1",
              direction: "lent",
              balance: 5000,
              dueDateUtc: "2099-12-01T00:00:00.000Z",
              isLedgerOwner: true,
              isSharedLedger: false,
            },
          })}
          statusFallback="Local contact"
          statusId="status"
          testId="profile"
        />
      </AppProviders>,
    );

    expect(screen.getByTestId("profile-kind")).toHaveTextContent("Local Contact");
    expect(screen.getByTestId("profile-direction")).toHaveTextContent("I Lent");
    expect(screen.getByTestId("profile-balance")).toBeInTheDocument();
    expect(screen.queryByTestId("profile-due")).not.toBeInTheDocument();
  });

  it("shows I Owe, Shared Contact, amount, and overdue badge", () => {
    render(
      <AppProviders>
        <ContactIdentityProfile
          row={row({
            connectionStatus: "connected",
            identityLine: "exits",
            utang: {
              relationshipId: "rel-2",
              direction: "owe",
              balance: 120,
              dueDateUtc: "2020-01-15T00:00:00.000Z",
              isLedgerOwner: true,
              isSharedLedger: true,
            },
          })}
          statusFallback="Connected"
          statusId="status"
          testId="profile"
        />
      </AppProviders>,
    );

    expect(screen.getByTestId("profile-kind")).toHaveTextContent("Shared Contact");
    expect(screen.getByTestId("profile-direction")).toHaveTextContent("I Owe");
    expect(screen.getByTestId("profile-amount")).toBeInTheDocument();
    expect(screen.getByTestId("profile-due")).toHaveTextContent(/Overdue:/);
    expect(screen.getByTestId("profile-due")).toHaveClass("personal-contact-sheet__overdue-badge");
  });
});
