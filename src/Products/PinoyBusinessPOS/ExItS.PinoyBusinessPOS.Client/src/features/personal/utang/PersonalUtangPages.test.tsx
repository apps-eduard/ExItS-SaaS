import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { PersonalUtangHubPage } from "@/features/personal/PersonalHubPages";
import {
  PersonalContactsPage,
  PersonalLentPage,
  PersonalRelationshipDetailPage,
} from "@/features/personal/utang/PersonalUtangPages";

const {
  contactId,
  linkedContactId,
  relationshipId,
  sharedRelationshipId,
  pendingIncomingId,
  pendingOutgoingId,
  confirmedId,
  meId,
  otherId,
  confirmMock,
} = vi.hoisted(() => {
  const pendingIncomingId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
  const sharedRelationshipId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1";
  const meId = "11111111-1111-1111-1111-111111111111";
  const otherId = "22222222-2222-2222-2222-222222222222";
  return {
    contactId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    linkedContactId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
    relationshipId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    sharedRelationshipId,
    pendingIncomingId,
    pendingOutgoingId: "cccccccc-cccc-cccc-cccc-ccccccccccc1",
    confirmedId: "cccccccc-cccc-cccc-cccc-ccccccccccc2",
    meId,
    otherId,
    confirmMock: vi.fn(
      async (
        _relationshipId: string,
        _entryId: string,
        _body?: { expectedVersion?: number | null },
      ) => ({
        id: pendingIncomingId,
        relationshipId: sharedRelationshipId,
        entryType: "Payment",
        amount: 50,
        signedDelta: -50,
        balanceAfter: 50,
        notes: null,
        dueDateUtc: null,
        createdByUserIdentityId: otherId,
        createdAtUtc: "2026-08-21T00:00:00Z",
        status: "Confirmed",
        resolvedByUserIdentityId: meId,
        resolvedAtUtc: "2026-08-21T01:00:00Z",
        disputeReason: null,
        canConfirm: false,
        canDispute: false,
        canCancel: false,
        affectsBalance: true,
        isSharedLedger: true,
      }),
    ),
  };
});

vi.mock("@/api/platform/personal-utang-client", async () => {
  const actual = await vi.importActual<typeof import("@/api/platform/personal-utang-client")>(
    "@/api/platform/personal-utang-client",
  );
  return {
    ...actual,
    listPersonalContacts: vi.fn(async () => [
      {
        id: contactId,
        displayName: "Walk-in Ana",
        phone: null,
        email: null,
        linkedUserIdentityId: null,
        publicUserId: null,
        status: "Active",
        createdAtUtc: "2026-08-20T00:00:00Z",
      },
      {
        id: linkedContactId,
        displayName: "Linked Ben",
        phone: null,
        email: null,
        linkedUserIdentityId: otherId,
        publicUserId: "EX-1111-2222",
        linkedMaskedEmail: "b***@example.com",
        linkedMaskedPhone: "****4567",
        status: "Active",
        createdAtUtc: "2026-08-20T00:00:00Z",
      },
    ]),
    getPersonalMe: vi.fn(async () => ({ userIdentityId: meId })),
    listLentRelationships: vi.fn(async () => [
      {
        id: relationshipId,
        perspective: "Lent",
        creditorUserIdentityId: meId,
        creditorContactId: null,
        debtorUserIdentityId: null,
        debtorContactId: contactId,
        currencyCode: "PHP",
        currentBalance: 200,
        dueDateUtc: null,
        status: "Active",
        version: 1,
        updatedAtUtc: "2026-08-21T00:00:00Z",
        isSharedLedger: false,
        isPrivate: true,
      },
      {
        id: sharedRelationshipId,
        perspective: "Lent",
        creditorUserIdentityId: meId,
        creditorContactId: null,
        debtorUserIdentityId: otherId,
        debtorContactId: linkedContactId,
        currencyCode: "PHP",
        currentBalance: 100,
        dueDateUtc: null,
        status: "Active",
        version: 3,
        updatedAtUtc: "2026-08-21T00:00:00Z",
        isSharedLedger: true,
        isPrivate: false,
      },
    ]),
    listBorrowedRelationships: vi.fn(async () => []),
    getPersonalDebtRelationship: vi.fn(async () => ({
      id: sharedRelationshipId,
      perspective: "Lent",
      creditorUserIdentityId: meId,
      creditorContactId: null,
      debtorUserIdentityId: otherId,
      debtorContactId: linkedContactId,
      currencyCode: "PHP",
      currentBalance: 100,
      dueDateUtc: null,
      status: "Active",
      version: 3,
      updatedAtUtc: "2026-08-21T00:00:00Z",
      isSharedLedger: true,
      isPrivate: false,
    })),
    getPersonalUtangBalance: vi.fn(async () => ({
      relationshipId: sharedRelationshipId,
      currentBalance: 100,
      currencyCode: "PHP",
      version: 3,
      updatedAtUtc: "2026-08-21T00:00:00Z",
    })),
    listPersonalUtangHistory: vi.fn(async () => [
      {
        id: pendingIncomingId,
        relationshipId: sharedRelationshipId,
        entryType: "Payment",
        amount: 50,
        signedDelta: -50,
        balanceAfter: 100,
        notes: null,
        dueDateUtc: null,
        createdByUserIdentityId: otherId,
        createdAtUtc: "2026-08-21T00:00:00Z",
        status: "Pending",
        resolvedByUserIdentityId: null,
        resolvedAtUtc: null,
        disputeReason: null,
        canConfirm: true,
        canDispute: true,
        canCancel: false,
        affectsBalance: false,
        isSharedLedger: true,
      },
      {
        id: pendingOutgoingId,
        relationshipId: sharedRelationshipId,
        entryType: "Loan",
        amount: 25,
        signedDelta: 25,
        balanceAfter: 100,
        notes: null,
        dueDateUtc: null,
        createdByUserIdentityId: meId,
        createdAtUtc: "2026-08-21T00:10:00Z",
        status: "Pending",
        resolvedByUserIdentityId: null,
        resolvedAtUtc: null,
        disputeReason: null,
        canConfirm: false,
        canDispute: false,
        canCancel: true,
        affectsBalance: false,
        isSharedLedger: true,
      },
      {
        id: confirmedId,
        relationshipId: sharedRelationshipId,
        entryType: "Loan",
        amount: 100,
        signedDelta: 100,
        balanceAfter: 100,
        notes: null,
        dueDateUtc: null,
        createdByUserIdentityId: meId,
        createdAtUtc: "2026-08-20T00:00:00Z",
        status: "Confirmed",
        resolvedByUserIdentityId: otherId,
        resolvedAtUtc: "2026-08-20T01:00:00Z",
        disputeReason: null,
        canConfirm: false,
        canDispute: false,
        canCancel: false,
        affectsBalance: true,
        isSharedLedger: true,
      },
    ]),
    confirmPersonalUtangEntry: confirmMock,
    disputePersonalUtangEntry: vi.fn(),
    cancelPersonalUtangEntry: vi.fn(),
    recordPersonalUtangEntry: vi.fn(),
    createPersonalDebtRelationship: vi.fn(),
  };
});

vi.mock("@/api/platform/personal-dashboard-client", () => ({
  getPersonalDashboard: vi.fn(async () => ({
    userIdentityId: meId,
    accountProfileId: "33333333-3333-3333-3333-333333333333",
    accountClass: "Personal",
    utangAvailable: true,
    contactCount: 2,
    activeRelationshipCount: 2,
    totalLentBalance: 300,
    totalBorrowedBalance: 0,
    pendingConfirmationCount: 2,
  })),
}));

vi.mock("@/features/personal/social/PersonalSocialPages", () => ({
  RelationshipInviteReminderPanel: () => null,
}));

vi.mock("@/offline/personal-offline-context", () => ({
  usePersonalOfflineContext: () => null,
}));

function renderPath(path: string) {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/personal/utang" element={<PersonalUtangHubPage />} />
          <Route path="/personal/utang/lent" element={<PersonalLentPage />} />
          <Route path="/personal/utang/people" element={<PersonalContactsPage />} />
          <Route
            path="/personal/utang/relationships/:relationshipId"
            element={<PersonalRelationshipDetailPage />}
          />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("Personal Utang shared-ledger UI", () => {
  beforeEach(() => {
    confirmMock.mockClear();
  });

  it("shows hub owed/i-owe totals, pending confirmation, and active accounts", async () => {
    renderPath("/personal/utang");
    expect(await screen.findByTestId("utang-hub-owed-to-me")).toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-i-owe")).toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-pending")).toHaveTextContent("Waiting for you (2)");
    expect(await screen.findByTestId("utang-hub-segments")).toBeInTheDocument();
    expect(screen.getByTestId(`utang-account-${relationshipId}`)).toBeInTheDocument();
    expect(screen.getByTestId(`utang-account-${sharedRelationshipId}`)).toBeInTheDocument();
    expect(screen.getByTestId("utang-open-lent")).toBeInTheDocument();
    expect(screen.getByTestId("utang-open-owe")).toBeInTheDocument();
    expect(screen.getByTestId("utang-open-people")).toBeInTheDocument();
  });

  it("filters hub accounts by Owed to me segment", async () => {
    const user = userEvent.setup();
    renderPath("/personal/utang");
    expect(await screen.findByTestId("utang-hub-segments")).toBeInTheDocument();
    await user.click(screen.getByTestId("utang-segment-lent"));
    expect(screen.getByTestId(`utang-account-${relationshipId}`)).toBeInTheDocument();
    await user.click(screen.getByTestId("utang-segment-owe"));
    expect(screen.queryByTestId(`utang-account-${relationshipId}`)).not.toBeInTheDocument();
  });

  it("lists linked people first with ExItS ID under the name, and keeps unlinked names visible", async () => {
    renderPath("/personal/utang/people");
    expect(await screen.findByTestId("utang-people-summary")).toHaveTextContent(
      "1 linked with ExItS ID · 1 without ExItS ID",
    );

    const linked = await screen.findByTestId(`utang-contact-${linkedContactId}`);
    expect(linked).toHaveTextContent("Linked Ben");
    expect(linked).toHaveTextContent("EX-1111-2222");
    const linkedRow = screen.getByTestId(`utang-contact-linked-row-${linkedContactId}`);
    expect(linkedRow).toHaveTextContent("EX-1111-2222");
    expect(linkedRow).toHaveTextContent("Linked");
    expect(within(linked).getByText("Linked Ben").compareDocumentPosition(linkedRow)).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );

    const unlinked = screen.getByTestId(`utang-contact-${contactId}`);
    expect(unlinked).toHaveTextContent("Walk-in Ana");
    expect(unlinked).toHaveTextContent("Not linked to an ExItS account");
    expect(screen.getByTestId(`utang-contact-link-${contactId}`)).toBeInTheDocument();

    const cards = screen.getAllByTestId(/utang-contact-/).filter((el) =>
      /^utang-contact-[0-9a-f-]{36}$/i.test(el.getAttribute("data-testid") ?? ""),
    );
    expect(cards[0]).toHaveAttribute("data-testid", `utang-contact-${linkedContactId}`);
    expect(cards[1]).toHaveAttribute("data-testid", `utang-contact-${contactId}`);
  });

  it("fills linked email as read-only when editing a linked person", async () => {
    const user = userEvent.setup();
    renderPath("/personal/utang/people");
    const card = await screen.findByTestId(`utang-contact-${linkedContactId}`);
    await user.click(within(card).getByRole("button", { name: /Edit person/i }));
    const email = await screen.findByTestId("utang-contact-email");
    expect(email).toHaveValue("b***@example.com");
    expect(email).toHaveAttribute("readonly");
    expect(screen.getByLabelText(/Email from linked ExItS account/i)).toBeInTheDocument();
    const phone = screen.getByTestId("utang-contact-phone");
    expect(phone).toHaveValue("****4567");
    expect(phone).toHaveAttribute("readonly");
    expect(screen.getByLabelText(/Phone from linked ExItS account/i)).toBeInTheDocument();
  });

  it("uses owes-you wording and linked vs private labels on I Lent", async () => {
    renderPath("/personal/utang/lent");
    expect(await screen.findByTestId(`utang-rel-row-${relationshipId}`)).toHaveTextContent(
      "Owes you",
    );
    expect(screen.getByTestId(`utang-rel-ledger-${relationshipId}`)).toHaveTextContent(
      "Not linked to ExItS",
    );
    expect(screen.getByTestId(`utang-rel-ledger-${sharedRelationshipId}`)).toHaveTextContent(
      "Linked",
    );
    expect(screen.getByTestId("personal-utang-lent").textContent).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-/i,
    );
  });

  it("switches create submit to send-for-confirmation for linked contacts", async () => {
    const user = userEvent.setup();
    renderPath("/personal/utang/lent");
    await screen.findByTestId("utang-rel-contact");
    await user.selectOptions(screen.getByTestId("utang-rel-contact"), linkedContactId);
    expect(screen.getByTestId("utang-rel-submit")).toHaveTextContent("Send for confirmation");
    expect(screen.getByTestId("utang-rel-confirm-hint")).toBeInTheDocument();
  });

  it("shows shared ledger detail with pending incoming and outgoing actions", async () => {
    const user = userEvent.setup();
    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);

    expect(await screen.findByTestId("utang-detail-ledger")).toHaveTextContent("Shared ledger");
    expect(screen.getByTestId("utang-entry-submit")).toHaveTextContent("Send for confirmation");
    expect(screen.getByTestId("utang-entry-confirm-hint")).toBeInTheDocument();

    const incoming = await screen.findByTestId(`utang-history-entry-${pendingIncomingId}`);
    expect(within(incoming).getByTestId(`utang-waiting-you-${pendingIncomingId}`)).toHaveTextContent(
      "Waiting for you",
    );
    expect(within(incoming).getByTestId(`utang-entry-status-${pendingIncomingId}`)).toHaveTextContent(
      "Pending",
    );
    expect(within(incoming).getByTestId(`utang-no-balance-${pendingIncomingId}`)).toBeInTheDocument();
    expect(within(incoming).getByTestId(`utang-confirm-${pendingIncomingId}`)).toHaveTextContent(
      "Confirm received",
    );
    expect(within(incoming).queryByTestId(`utang-cancel-${pendingIncomingId}`)).toBeNull();

    const outgoing = screen.getByTestId(`utang-history-entry-${pendingOutgoingId}`);
    expect(within(outgoing).getByTestId(`utang-waiting-other-${pendingOutgoingId}`)).toHaveTextContent(
      "Waiting for Linked Ben",
    );
    expect(within(outgoing).getByTestId(`utang-cancel-${pendingOutgoingId}`)).toBeInTheDocument();
    expect(within(outgoing).queryByTestId(`utang-confirm-${pendingOutgoingId}`)).toBeNull();

    const confirmed = screen.getByTestId(`utang-history-entry-${confirmedId}`);
    expect(within(confirmed).getByTestId(`utang-entry-status-${confirmedId}`)).toHaveTextContent(
      "Confirmed",
    );
    expect(confirmed.textContent).toMatch(/Balance after/);

    expect(screen.getByTestId("personal-utang-detail").textContent).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-/i,
    );

    await user.click(screen.getByTestId(`utang-confirm-${pendingIncomingId}`));
    await waitFor(() => expect(confirmMock).toHaveBeenCalled());
  });
});
