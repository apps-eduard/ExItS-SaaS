import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppProviders } from "@/app/providers";
import { PeopleMobileRow } from "@/features/personal/PeopleMobileRow";
import type { PeopleRowModel } from "@/features/personal/people-status";
import type { PersonalContactDto } from "@/api/platform/personal-types";

const contact: PersonalContactDto = {
  id: "c1",
  displayName: "Juan Dela Cruz",
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
    lentTotal: 250,
    lentCount: 1,
    oweTotal: 0,
    oweCount: 0,
    utang: {
      relationshipId: "rel-1",
      direction: "lent",
      balance: 250,
      dueDateUtc: "2020-01-01T00:00:00.000Z",
      isLedgerOwner: true,
      isSharedLedger: false,
    },
    ...overrides,
  };
}

function renderRow(model: PeopleRowModel, statusLabel = "Local Contact", onOpenContact = vi.fn()) {
  return render(
    <AppProviders>
      <ul>
        <PeopleMobileRow
          row={model}
          statusLabel={statusLabel}
          onOpenContact={onOpenContact}
        />
      </ul>
    </AppProviders>,
  );
}

describe("PeopleMobileRow", () => {
  it("renders compact identity row without financial summary", () => {
    renderRow(row());

    expect(screen.getByTestId("people-row-c1")).toBeInTheDocument();
    expect(screen.getByTestId("people-row-open-c1")).toHaveTextContent("Juan Dela Cruz");
    expect(screen.getByTestId("people-row-status-c1")).toHaveTextContent("Local Contact");
    expect(screen.getByTestId("person-avatar")).toBeInTheDocument();

    expect(screen.queryByTestId("people-row-balance-summary-c1")).not.toBeInTheDocument();
    expect(screen.queryByText(/I Lent/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/I Owe/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Overdue/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/No balance/i)).not.toBeInTheDocument();
  });

  it("does not render more menu or expand toggle", () => {
    renderRow(row());

    expect(screen.queryByTestId("people-row-more-c1")).not.toBeInTheDocument();
    expect(screen.queryByTestId("people-row-caret-c1")).not.toBeInTheDocument();
    expect(screen.queryByTestId("people-row-secondary-toggle-c1")).not.toBeInTheDocument();
    expect(screen.queryByTestId("people-row-actions-c1")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^More$/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Show actions|Hide actions/i })).not.toBeInTheDocument();
  });

  it("shows Connected and Pending status labels", () => {
    const { unmount } = renderRow(
      row({
        connectionStatus: "connected",
        utang: undefined,
        lentTotal: 0,
        lentCount: 0,
      }),
      "Connected",
    );
    expect(screen.getByTestId("people-row-status-c1")).toHaveTextContent("Connected");
    unmount();

    renderRow(
      row({
        connectionStatus: "request_sent",
        utang: undefined,
        lentTotal: 0,
        lentCount: 0,
      }),
      "Pending",
    );
    expect(screen.getByTestId("people-row-status-c1")).toHaveTextContent("Pending");
  });

  it("opens contact stacked sheet via row click", async () => {
    const user = userEvent.setup();
    const onOpenContact = vi.fn();
    renderRow(row(), "Local Contact", onOpenContact);

    await user.click(screen.getByTestId("people-row-c1"));
    expect(onOpenContact).toHaveBeenCalledTimes(1);
  });
});
