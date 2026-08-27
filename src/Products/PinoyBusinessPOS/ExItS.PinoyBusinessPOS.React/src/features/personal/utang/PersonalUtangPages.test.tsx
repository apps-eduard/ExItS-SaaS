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
  countPendingOutgoingLoanProposals,
  mapPersonalUtangMutationError,
  PERSONAL_UTANG_PROPOSAL_ERROR_CODES,
} from "@/features/personal/utang/PersonalUtangPages";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  listPersonalUtangHistory,
  recordPersonalUtangEntry,
  settlePersonalDebtRelationship,
  closePersonalDebtRelationship,
  getPersonalDebtRelationship,
  getPersonalUtangBalance,
} from "@/api/platform/personal-utang-client";
import { en } from "@/i18n/locales/en";

const onlineMock = vi.hoisted(() => ({ current: true }));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => onlineMock.current,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(onlineMock.current);
    return () => undefined;
  },
}));

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
        _relationshipId?: string,
        _entryId?: string,
        _body?: { expectedVersion?: number | null },
      ) => {
        void _relationshipId;
        void _entryId;
        void _body;
        return {
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
          intent: "Regular",
          settlementBalanceSnapshot: null,
          isSettlement: false,
        };
      },
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
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
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
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
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
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
      },
    ]),
    confirmPersonalUtangEntry: confirmMock,
    disputePersonalUtangEntry: vi.fn(),
    cancelPersonalUtangEntry: vi.fn(),
    recordPersonalUtangEntry: vi.fn(),
    createPersonalDebtRelationship: vi.fn(),
    settlePersonalDebtRelationship: vi.fn(),
    closePersonalDebtRelationship: vi.fn(),
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

async function openUtangRecordForm(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByTestId("utang-record-toggle"));
  await screen.findByTestId("utang-rel-contact");
}

describe("Personal Utang shared-ledger UI", () => {
  beforeEach(() => {
    onlineMock.current = true;
    confirmMock.mockClear();
    vi.mocked(recordPersonalUtangEntry).mockReset();
    vi.mocked(settlePersonalDebtRelationship).mockReset();
    vi.mocked(closePersonalDebtRelationship).mockReset();
    vi.mocked(listPersonalUtangHistory).mockReset();
    vi.mocked(listPersonalUtangHistory).mockImplementation(async () => [
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
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
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
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
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
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
      },
    ]);
  });

  it("shows hub owed/i-owe totals, pending confirmation, and active accounts", async () => {
    renderPath("/personal/utang");
    expect(await screen.findByTestId("utang-hub-owed-to-me")).toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-i-owe")).toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-record")).toHaveTextContent("Record money lent");
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

  it("redirects legacy utang people route to authoritative people page", () => {
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/personal/utang/people"]}>
          <Routes>
            <Route path="/personal/utang/people" element={<PersonalContactsPage />} />
            <Route path="/personal/people" element={<div data-testid="people-page" />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );
    expect(screen.getByTestId("people-page")).toBeInTheDocument();
  });

  it("hides record form by default on Money I lent", async () => {
    renderPath("/personal/utang/lent");
    expect(await screen.findByTestId("utang-record-toggle")).toHaveTextContent("Record money lent");
    expect(screen.queryByTestId("utang-rel-contact")).not.toBeInTheDocument();
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
      "Connected",
    );
    expect(screen.getByTestId("personal-utang-lent").textContent).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-/i,
    );
  });

  it("switches create submit to send-for-confirmation for linked contacts", async () => {
    const user = userEvent.setup();
    renderPath("/personal/utang/lent");
    await openUtangRecordForm(user);
    await user.selectOptions(screen.getByTestId("utang-rel-contact"), linkedContactId);
    expect(screen.getByTestId("utang-rel-submit")).toHaveTextContent("Send for confirmation");
    expect(screen.getByTestId("utang-rel-confirm-hint")).toBeInTheDocument();
  });

  it("requires Purpose / Note before recording a private Utang", async () => {
    const user = userEvent.setup();
    renderPath("/personal/utang/lent");
    await openUtangRecordForm(user);
    await user.selectOptions(screen.getByTestId("utang-rel-contact"), contactId);
    await user.type(screen.getByTestId("utang-rel-amount"), "100");
    await user.click(screen.getByTestId("utang-rel-submit"));
    expect(await screen.findByRole("alert")).toHaveTextContent(/purpose \/ note/i);
    expect(screen.getByTestId("utang-rel-submit")).toHaveTextContent("Save Utang");
  });

  it("shows private save hint for unlinked contacts", async () => {
    const user = userEvent.setup();
    renderPath("/personal/utang/lent");
    await openUtangRecordForm(user);
    await user.selectOptions(screen.getByTestId("utang-rel-contact"), contactId);
    expect(screen.getByTestId("utang-rel-private-hint")).toBeInTheDocument();
  });

  it("shows shared ledger detail with pending incoming and outgoing actions", async () => {
    const user = userEvent.setup();
    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);

    expect(await screen.findByTestId("utang-detail-ledger")).toHaveTextContent("Shared ledger");
    expect(screen.getByTestId("utang-entry-submit")).toHaveTextContent("Send for confirmation");
    expect(screen.getByTestId("utang-entry-confirm-hint")).toBeInTheDocument();

    const incoming = await screen.findByTestId(`utang-history-entry-${pendingIncomingId}`);
    expect(within(incoming).getByTestId(`utang-waiting-you-${pendingIncomingId}`)).toHaveTextContent(
      "Linked Ben recorded an Utang entry",
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
    expect(screen.getByTestId("utang-pending-waiting-hint")).toHaveTextContent(
      "1 entries are waiting for Linked Ben's review.",
    );

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

  it("maps pending-limit errorCode to friendly copy on record", async () => {
    const user = userEvent.setup();
    vi.mocked(recordPersonalUtangEntry).mockRejectedValueOnce(
      new PlatformApiError(429, {
        errorCode: PERSONAL_UTANG_PROPOSAL_ERROR_CODES.pendingLimit,
        detail: "server English detail must not be shown",
      }),
    );

    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);
    await screen.findByTestId("utang-entry-type");
    await user.selectOptions(screen.getByTestId("utang-entry-type"), "Payment");
    await user.type(screen.getByTestId("utang-entry-amount"), "10");
    await user.type(screen.getByTestId("utang-entry-notes"), "Partial payment");
    await user.click(screen.getByTestId("utang-entry-submit"));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      /You already have 3 entries waiting for Linked Ben's review/i,
    );
  });

  it("disables Loan submit and shows limit message when 3 outgoing proposals are pending", async () => {
    const user = userEvent.setup();
    vi.mocked(listPersonalUtangHistory).mockResolvedValueOnce([
      {
        id: "dddddddd-dddd-dddd-dddd-ddddddddddd1",
        relationshipId: sharedRelationshipId,
        entryType: "Loan",
        amount: 10,
        signedDelta: 10,
        balanceAfter: 100,
        notes: null,
        dueDateUtc: null,
        createdByUserIdentityId: meId,
        createdAtUtc: "2026-08-21T00:01:00Z",
        status: "Pending",
        resolvedByUserIdentityId: null,
        resolvedAtUtc: null,
        disputeReason: null,
        canConfirm: false,
        canDispute: false,
        canCancel: true,
        affectsBalance: false,
        isSharedLedger: true,
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
      },
      {
        id: "dddddddd-dddd-dddd-dddd-ddddddddddd2",
        relationshipId: sharedRelationshipId,
        entryType: "Loan",
        amount: 20,
        signedDelta: 20,
        balanceAfter: 100,
        notes: null,
        dueDateUtc: null,
        createdByUserIdentityId: meId,
        createdAtUtc: "2026-08-21T00:02:00Z",
        status: "Pending",
        resolvedByUserIdentityId: null,
        resolvedAtUtc: null,
        disputeReason: null,
        canConfirm: false,
        canDispute: false,
        canCancel: true,
        affectsBalance: false,
        isSharedLedger: true,
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
      },
      {
        id: "dddddddd-dddd-dddd-dddd-ddddddddddd3",
        relationshipId: sharedRelationshipId,
        entryType: "Loan",
        amount: 30,
        signedDelta: 30,
        balanceAfter: 100,
        notes: null,
        dueDateUtc: null,
        createdByUserIdentityId: meId,
        createdAtUtc: "2026-08-21T00:03:00Z",
        status: "Pending",
        resolvedByUserIdentityId: null,
        resolvedAtUtc: null,
        disputeReason: null,
        canConfirm: false,
        canDispute: false,
        canCancel: true,
        affectsBalance: false,
        isSharedLedger: true,
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
      },
    ]);

    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);
    expect(await screen.findByTestId("utang-pending-limit-hint")).toHaveTextContent(
      /You already have 3 entries waiting for Linked Ben's review/i,
    );
    expect(screen.getByTestId("utang-view-pending")).toHaveTextContent("View pending entries");

    await user.selectOptions(screen.getByTestId("utang-entry-type"), "Loan");
    expect(screen.getByTestId("utang-entry-submit")).toBeDisabled();

    await user.selectOptions(screen.getByTestId("utang-entry-type"), "Payment");
    expect(screen.getByTestId("utang-entry-submit")).not.toBeDisabled();
  });
});

describe("Personal Utang settlement UI", () => {
  beforeEach(() => {
    onlineMock.current = true;
    vi.mocked(settlePersonalDebtRelationship).mockReset();
    vi.mocked(closePersonalDebtRelationship).mockReset();
    vi.mocked(getPersonalDebtRelationship).mockReset();
    vi.mocked(getPersonalUtangBalance).mockReset();
    vi.mocked(listPersonalUtangHistory).mockReset();
  });

  it("shows Settle when Active with balance greater than zero", async () => {
    vi.mocked(getPersonalDebtRelationship).mockResolvedValue({
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
      version: 2,
      updatedAtUtc: "2026-08-21T00:00:00Z",
      isSharedLedger: false,
      isPrivate: true,
    });
    vi.mocked(getPersonalUtangBalance).mockResolvedValue({
      relationshipId,
      currentBalance: 200,
      currencyCode: "PHP",
      version: 2,
      updatedAtUtc: "2026-08-21T00:00:00Z",
    });
    vi.mocked(listPersonalUtangHistory).mockResolvedValue([]);

    renderPath(`/personal/utang/relationships/${relationshipId}`);
    expect(await screen.findByTestId("utang-settle")).toBeInTheDocument();
    expect(screen.getByTestId("utang-detail-status")).toHaveTextContent("Active");
    expect(screen.queryByTestId("utang-mark-settled")).not.toBeInTheDocument();
  });

  it("shows Mark as settled when Active with zero balance", async () => {
    vi.mocked(getPersonalDebtRelationship).mockResolvedValue({
      id: relationshipId,
      perspective: "Lent",
      creditorUserIdentityId: meId,
      creditorContactId: null,
      debtorUserIdentityId: null,
      debtorContactId: contactId,
      currencyCode: "PHP",
      currentBalance: 0,
      dueDateUtc: null,
      status: "Active",
      version: 2,
      updatedAtUtc: "2026-08-21T00:00:00Z",
      isSharedLedger: false,
      isPrivate: true,
    });
    vi.mocked(getPersonalUtangBalance).mockResolvedValue({
      relationshipId,
      currentBalance: 0,
      currencyCode: "PHP",
      version: 2,
      updatedAtUtc: "2026-08-21T00:00:00Z",
    });
    vi.mocked(listPersonalUtangHistory).mockResolvedValue([]);

    renderPath(`/personal/utang/relationships/${relationshipId}`);
    expect(await screen.findByTestId("utang-mark-settled")).toBeInTheDocument();
    expect(screen.queryByTestId("utang-settle")).not.toBeInTheDocument();
  });

  it("hides mutation form when Closed / Settled", async () => {
    vi.mocked(getPersonalDebtRelationship).mockResolvedValue({
      id: relationshipId,
      perspective: "Lent",
      creditorUserIdentityId: meId,
      creditorContactId: null,
      debtorUserIdentityId: null,
      debtorContactId: contactId,
      currencyCode: "PHP",
      currentBalance: 0,
      dueDateUtc: null,
      status: "Closed",
      version: 3,
      updatedAtUtc: "2026-08-21T00:00:00Z",
      isSharedLedger: false,
      isPrivate: true,
    });
    vi.mocked(getPersonalUtangBalance).mockResolvedValue({
      relationshipId,
      currentBalance: 0,
      currencyCode: "PHP",
      version: 3,
      updatedAtUtc: "2026-08-21T00:00:00Z",
    });
    vi.mocked(listPersonalUtangHistory).mockResolvedValue([
      {
        id: confirmedId,
        relationshipId,
        entryType: "Payment",
        amount: 200,
        signedDelta: -200,
        balanceAfter: 0,
        notes: null,
        dueDateUtc: null,
        createdByUserIdentityId: meId,
        createdAtUtc: "2026-08-21T00:00:00Z",
        status: "Confirmed",
        resolvedByUserIdentityId: null,
        resolvedAtUtc: null,
        disputeReason: null,
        canConfirm: false,
        canDispute: false,
        canCancel: false,
        affectsBalance: true,
        isSharedLedger: false,
        intent: "Settlement",
        settlementBalanceSnapshot: 200,
        isSettlement: true,
      },
    ]);

    renderPath(`/personal/utang/relationships/${relationshipId}`);
    expect(await screen.findByTestId("utang-detail-status")).toHaveTextContent("Settled");
    expect(screen.queryByTestId("utang-entry-type")).not.toBeInTheDocument();
    expect(screen.queryByTestId("utang-settle")).not.toBeInTheDocument();
    expect(screen.queryByTestId("utang-mark-settled")).not.toBeInTheDocument();
    expect(screen.getByTestId("utang-history")).toBeInTheDocument();
    expect(screen.getByTestId(`utang-history-entry-${confirmedId}`)).toHaveTextContent(
      "Settlement",
    );
  });

  it("disables Settle while offline", async () => {
    onlineMock.current = true;
    vi.mocked(getPersonalDebtRelationship).mockResolvedValue({
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
      version: 2,
      updatedAtUtc: "2026-08-21T00:00:00Z",
      isSharedLedger: false,
      isPrivate: true,
    });
    vi.mocked(getPersonalUtangBalance).mockResolvedValue({
      relationshipId,
      currentBalance: 200,
      currencyCode: "PHP",
      version: 2,
      updatedAtUtc: "2026-08-21T00:00:00Z",
    });
    vi.mocked(listPersonalUtangHistory).mockResolvedValue([]);

    const { QueryClient, QueryClientProvider } = await import("@tanstack/react-query");
    const { PreferencesProvider } = await import("@/hooks/usePreferences");
    const { I18nProvider } = await import("@/i18n/I18nProvider");
    const hostClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });

    function Host({ onlineFlag }: { onlineFlag: boolean }) {
      onlineMock.current = onlineFlag;
      return (
        <QueryClientProvider client={hostClient}>
          <PreferencesProvider>
            <I18nProvider>
              <MemoryRouter initialEntries={[`/personal/utang/relationships/${relationshipId}`]}>
                <Routes>
                  <Route
                    path="/personal/utang/relationships/:relationshipId"
                    element={<PersonalRelationshipDetailPage />}
                  />
                </Routes>
              </MemoryRouter>
            </I18nProvider>
          </PreferencesProvider>
        </QueryClientProvider>
      );
    }

    const view = render(<Host onlineFlag={true} />);
    expect(await screen.findByTestId("utang-settle")).not.toBeDisabled();
    view.rerender(<Host onlineFlag={false} />);
    expect(screen.getByTestId("utang-settle")).toBeDisabled();
    expect(
      screen.getByText("Settling this utang needs internet."),
    ).toBeInTheDocument();
  });

  it("shows awaiting banner after linked settle mock", async () => {
    const user = userEvent.setup();
    const settlementId = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    vi.mocked(getPersonalDebtRelationship).mockResolvedValue({
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
    });
    vi.mocked(getPersonalUtangBalance).mockResolvedValue({
      relationshipId: sharedRelationshipId,
      currentBalance: 100,
      currencyCode: "PHP",
      version: 3,
      updatedAtUtc: "2026-08-21T00:00:00Z",
    });
    vi.mocked(listPersonalUtangHistory).mockResolvedValue([
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
        intent: "Regular",
        settlementBalanceSnapshot: null,
        isSettlement: false,
      },
    ]);
    vi.mocked(settlePersonalDebtRelationship).mockImplementation(async () => {
      vi.mocked(listPersonalUtangHistory).mockResolvedValue([
        {
          id: settlementId,
          relationshipId: sharedRelationshipId,
          entryType: "Payment",
          amount: 100,
          signedDelta: -100,
          balanceAfter: 100,
          notes: null,
          dueDateUtc: null,
          createdByUserIdentityId: meId,
          createdAtUtc: "2026-08-21T03:00:00Z",
          status: "Pending",
          resolvedByUserIdentityId: null,
          resolvedAtUtc: null,
          disputeReason: null,
          canConfirm: false,
          canDispute: false,
          canCancel: true,
          affectsBalance: false,
          isSharedLedger: true,
          intent: "Settlement",
          settlementBalanceSnapshot: 100,
          isSettlement: true,
        },
      ]);
      return {
        outcome: "AwaitingCounterpartyConfirmation",
        relationship: {
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
          version: 4,
          updatedAtUtc: "2026-08-21T03:00:00Z",
          isSharedLedger: true,
          isPrivate: false,
        },
        settlementEntry: {
          id: settlementId,
          relationshipId: sharedRelationshipId,
          entryType: "Payment",
          amount: 100,
          signedDelta: -100,
          balanceAfter: 100,
          notes: null,
          dueDateUtc: null,
          createdByUserIdentityId: meId,
          createdAtUtc: "2026-08-21T03:00:00Z",
          status: "Pending",
          resolvedByUserIdentityId: null,
          resolvedAtUtc: null,
          disputeReason: null,
          canConfirm: false,
          canDispute: false,
          canCancel: true,
          affectsBalance: false,
          isSharedLedger: true,
          intent: "Settlement",
          settlementBalanceSnapshot: 100,
          isSettlement: true,
        },
      };
    });

    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);
    await user.click(await screen.findByTestId("utang-settle"));
    await user.click(screen.getByTestId("utang-settle-confirm"));
    expect(await screen.findByTestId("utang-settle-awaiting")).toBeInTheDocument();
    expect(settlePersonalDebtRelationship).toHaveBeenCalled();
  });
});

describe("Personal Utang anti-spam helpers", () => {
  const t = (key: keyof typeof en) => en[key];

  it("counts only pending outgoing Loan proposals", () => {
    expect(
      countPendingOutgoingLoanProposals([
        { status: "Pending", entryType: "Loan", canCancel: true, canConfirm: false },
        { status: "Pending", entryType: "Payment", canCancel: true, canConfirm: false },
        { status: "Pending", entryType: "Loan", canCancel: false, canConfirm: true },
        { status: "Confirmed", entryType: "Loan", canCancel: false, canConfirm: false },
      ]),
    ).toBe(1);
  });

  it("maps proposal anti-spam error codes with counterparty name", () => {
    expect(
      mapPersonalUtangMutationError(
        new PlatformApiError(429, {
          errorCode: PERSONAL_UTANG_PROPOSAL_ERROR_CODES.pendingLimit,
          detail: "ignore",
        }),
        "Ana",
        t,
      ),
    ).toContain("Ana");
    expect(
      mapPersonalUtangMutationError(
        new PlatformApiError(429, {
          errorCode: PERSONAL_UTANG_PROPOSAL_ERROR_CODES.dailyLimit,
          detail: "ignore",
        }),
        "Ben",
        t,
      ),
    ).toMatch(/today's limit.*Ben/i);
    expect(
      mapPersonalUtangMutationError(
        new PlatformApiError(409, {
          errorCode: PERSONAL_UTANG_PROPOSAL_ERROR_CODES.duplicate,
          detail: "ignore",
        }),
        "Ben",
        t,
      ),
    ).toBe(en["personal.utang.duplicateSubmission"]);
  });
});
