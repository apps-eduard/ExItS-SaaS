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
  parseAdjustmentDelta,
  projectUtangBalance,
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
import {
  defaultUiPreferences,
  writeUiPreferences,
} from "@/lib/preferences/ui-preferences";

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
        confirmationSource: "None",
        wasAutoSynced: false,
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
    getPersonalSharedUtangPreference: vi.fn(async () => ({
      id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
      ownerUserIdentityId: meId,
      counterpartyUserIdentityId: otherId,
      receiveSharedUtang: true,
      autoAcceptSharedUtang: false,
      sharedUtangNotifications: true,
      version: 1,
      updatedAtUtc: "2026-08-21T00:00:00Z",
    })),
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
      ledgerOwnerUserIdentityId: null,
      isLedgerOwner: true,
      shareOutcome: "Private",
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
      ledgerOwnerUserIdentityId: meId,
      isLedgerOwner: true,
      shareOutcome: "Shared",
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
      ledgerOwnerUserIdentityId: meId,
      isLedgerOwner: true,
      shareOutcome: "Shared",
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
        confirmationSource: "None",
        wasAutoSynced: false,
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
        confirmationSource: "None",
        wasAutoSynced: false,
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
        confirmationSource: "None",
        wasAutoSynced: false,
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
    sharedWithMeLentBalance: 0,
    sharedWithMeBorrowedBalance: 0,
    sharedWithMeActiveCount: 0,
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

async function openEntryDialog(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByTestId("utang-qa-update-balance"));
  return screen.findByTestId("utang-entry-dialog");
}

async function chooseEntryType(
  user: ReturnType<typeof userEvent.setup>,
  value: "Payment" | "Loan" | "Adjustment",
) {
  await user.click(screen.getByTestId("utang-entry-type"));
  await user.click(await screen.findByTestId(`utang-entry-type-option-${value}`));
}

async function openUtangRecordForm(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByTestId("utang-summary-record"));
  await screen.findByTestId("utang-rel-contact");
}

async function chooseRecordContact(user: ReturnType<typeof userEvent.setup>, id: string) {
  await user.click(screen.getByTestId("utang-rel-contact"));
  await user.click(await screen.findByTestId(`utang-rel-contact-option-${id}`));
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
        confirmationSource: "None",
        wasAutoSynced: false,
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
        confirmationSource: "None",
        wasAutoSynced: false,
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
        confirmationSource: "None",
        wasAutoSynced: false,
      },
    ]);
  });

  it("shows hub My record / Shared with me cards for owed and I owe", async () => {
    renderPath("/personal/utang");
    expect(await screen.findByTestId("utang-hub-owed-to-me")).toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-owed-to-me-mine")).toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-owed-to-me-shared")).toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-i-owe")).toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-i-owe-mine")).toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-i-owe-shared")).toBeInTheDocument();
    expect(screen.queryByTestId("utang-hub-record")).not.toBeInTheDocument();
    expect(screen.getByTestId("utang-hub-pending")).toHaveTextContent("Waiting for you (2)");
    const actions = screen.getByTestId("utang-hub-actions");
    expect(actions).toContainElement(screen.getByTestId("utang-open-lent"));
    expect(actions).toContainElement(screen.getByTestId("utang-open-owe"));
    expect(actions).toContainElement(screen.getByTestId("utang-open-people"));
    expect(await screen.findByTestId("utang-hub-segments")).toBeInTheDocument();
    expect(screen.getByTestId(`utang-account-${relationshipId}`)).toHaveClass(
      "utang-account-card--mine",
    );
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

  it("uses underline tabs for Active accounts when that preference is saved", async () => {
    writeUiPreferences({ ...defaultUiPreferences, tabStyle: "underline" });
    try {
      renderPath("/personal/utang");
      expect(await screen.findByTestId("utang-hub-segments")).toHaveAttribute("data-variant", "underline");
      expect(screen.getByRole("tab", { name: /All/i })).toHaveAttribute("aria-selected", "true");
    } finally {
      writeUiPreferences(defaultUiPreferences);
    }
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
    expect(await screen.findByTestId("utang-summary-record")).toHaveTextContent("Record money lent");
    expect(screen.queryByTestId("utang-record-dialog")).not.toBeInTheDocument();
    expect(screen.queryByTestId("utang-rel-contact")).not.toBeInTheDocument();
  });

  it("uses owes-you wording and linked vs private labels on I Lent", async () => {
    renderPath("/personal/utang/lent");
    expect(await screen.findByTestId(`utang-rel-row-${relationshipId}`)).toHaveTextContent(
      "Owes you",
    );
    expect(screen.getByTestId(`utang-rel-ledger-${relationshipId}`)).toHaveTextContent(
      "Local contact",
    );
    expect(screen.getByTestId(`utang-rel-ledger-${relationshipId}`)).toHaveAttribute(
      "data-tone",
      "info",
    );
    expect(screen.getByTestId(`utang-rel-ledger-${sharedRelationshipId}`)).toHaveTextContent(
      "Connected",
    );
    expect(screen.getByTestId(`utang-rel-ledger-${sharedRelationshipId}`)).toHaveAttribute(
      "data-tone",
      "success",
    );
    expect(screen.getByTestId("personal-utang-lent").textContent).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-/i,
    );
  });

  it("shows Share toggle for linked contacts defaulting to Save privately", async () => {
    const user = userEvent.setup();
    renderPath("/personal/utang/lent");
    await openUtangRecordForm(user);
    await chooseRecordContact(user, linkedContactId);
    expect(screen.getByTestId("utang-rel-submit")).toHaveTextContent("Save privately");
    expect(screen.getByTestId("utang-rel-share-toggle")).toBeInTheDocument();
    expect(screen.getByTestId("utang-rel-share-checkbox")).not.toBeChecked();
    await user.click(screen.getByTestId("utang-rel-share-checkbox"));
    expect(screen.getByTestId("utang-rel-submit")).toHaveTextContent("Share with");
  });

  it("requires Purpose / Note before recording a private Utang", async () => {
    const user = userEvent.setup();
    renderPath("/personal/utang/lent");
    await openUtangRecordForm(user);
    await chooseRecordContact(user, contactId);
    await user.type(screen.getByTestId("utang-rel-amount"), "100");
    await user.click(screen.getByTestId("utang-rel-submit"));
    expect(await screen.findByRole("alert")).toHaveTextContent(/purpose \/ note/i);
    expect(screen.getByTestId("utang-rel-submit")).toHaveTextContent("Save privately");
  });

  it("shows private save hint for unlinked contacts", async () => {
    const user = userEvent.setup();
    renderPath("/personal/utang/lent");
    await openUtangRecordForm(user);
    await chooseRecordContact(user, contactId);
    expect(screen.getByTestId("utang-rel-private-hint")).toBeInTheDocument();
  });

  it("shows shared ledger detail with pending incoming and outgoing actions", async () => {
    const user = userEvent.setup();
    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);

    expect(await screen.findByTestId("utang-detail-ledger")).toHaveTextContent("My Record");
    expect(screen.queryByTestId("utang-entry-submit")).not.toBeInTheDocument();
    await openEntryDialog(user);
    expect(screen.getByTestId("utang-entry-submit")).toHaveTextContent("Save");
    expect(screen.getByTestId("utang-entry-confirm-hint")).toBeInTheDocument();
    await user.click(screen.getByTestId("utang-entry-cancel"));
    expect(screen.queryByTestId("utang-entry-dialog")).not.toBeInTheDocument();

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
    expect(within(confirmed).getByTestId(`utang-entry-action-${confirmedId}`)).toHaveTextContent(
      "Starting balance",
    );
    expect(confirmed.textContent).not.toMatch(/Balance after/);

    expect(screen.getByTestId("personal-utang-detail").textContent).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-/i,
    );

    await user.click(screen.getByTestId(`utang-confirm-${pendingIncomingId}`));
    await waitFor(() => expect(confirmMock).toHaveBeenCalled());
  });

  it("opens the entry dialog from Update Balance with the amount focused", async () => {
    const user = userEvent.setup();
    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);

    const quickActions = await screen.findByTestId("utang-detail-quick-actions");
    expect(within(quickActions).getByText("Quick actions")).toBeInTheDocument();
    expect(within(quickActions).getByTestId("utang-qa-reminders")).toHaveTextContent("Create reminders");

    await user.click(within(quickActions).getByTestId("utang-qa-shared-setting"));
    const sharedSetting = await screen.findByTestId("utang-shared-setting-dialog");
    expect(within(sharedSetting).getByTestId("utang-shared-setting-dialog-ribbon")).toHaveTextContent(
      "Shared setting from Linked Ben",
    );
    expect(await within(sharedSetting).findByTestId("person-detail-shared-settings-heading")).toHaveTextContent(
      "Shared settings",
    );
    expect(within(sharedSetting).getByTestId("person-detail-connection-heading")).toHaveTextContent(
      "Connection & safety",
    );
    expect(within(sharedSetting).getByTestId("person-detail-prefs-receive")).toBeInTheDocument();
    expect(within(sharedSetting).getByTestId("person-detail-unlink")).toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(screen.queryByTestId("utang-shared-setting-dialog")).not.toBeInTheDocument();

    await user.click(within(quickActions).getByTestId("utang-qa-view-contact"));
    const viewContact = await screen.findByTestId("utang-view-contact-dialog");
    expect(within(viewContact).getByTestId("utang-view-contact-status")).toHaveTextContent("Connected");
    expect(within(viewContact).getByTestId("utang-view-contact-name")).toHaveTextContent("Linked Ben");
    expect(within(viewContact).getByTestId("utang-view-contact-id")).toHaveTextContent("EX-1111-2222");
    expect(within(viewContact).getByTestId("utang-view-contact-email")).toHaveTextContent("b***@example.com");
    await user.keyboard("{Escape}");

    await user.click(within(quickActions).getByTestId("utang-qa-reminders"));
    const reminders = await screen.findByTestId("utang-reminders-dialog");
    expect(within(reminders).getByTestId("utang-reminders-dialog-ribbon")).toHaveTextContent("Reminders");
    await user.keyboard("{Escape}");
    expect(screen.queryByTestId("utang-reminders-dialog")).not.toBeInTheDocument();

    const dialog = await openEntryDialog(user);
    expect(dialog).toHaveAttribute("role", "dialog");
    expect(within(dialog).getByTestId("utang-entry-dialog-ribbon")).toHaveTextContent("Update Balance");
    expect(dialog).toHaveAccessibleName("Update Balance");
    const head = within(dialog).getByTestId("utang-entry-dialog-head");
    expect(within(head).getByTestId("utang-entry-dialog-balance")).toHaveTextContent("₱");
    const amount = screen.getByTestId("utang-entry-amount");
    await waitFor(() => expect(amount).toHaveFocus());
    expect(amount).toHaveValue("");
    expect(amount.closest(".ui-standards-qinput")).toHaveClass("ui-standards-qinput--outlined");
    expect(screen.getByTestId("utang-entry-cancel")).toHaveAttribute("data-appearance", "glossy");
    expect(screen.getByTestId("utang-entry-submit")).toHaveAttribute("data-appearance", "glossy");
    expect(screen.getByTestId("utang-entry-submit")).toHaveClass("exits-btn--glossy");
    expect(within(head).getByText("Name")).toBeInTheDocument();
    expect(within(head).getByText("You lent")).toBeInTheDocument();
    expect(screen.getByTestId("utang-entry-save-icon")).toBeInTheDocument();
    await user.type(amount, "100");
    expect(amount).toHaveValue("100");
    expect(screen.getByTestId("utang-entry-amount-clear")).toBeInTheDocument();
    await user.type(amount, "1");
    expect(amount).toHaveValue("100");
    expect(screen.getByRole("alert")).toHaveTextContent("Amount cannot be more than what is lent or owed.");
    await user.click(screen.getByTestId("utang-entry-amount-clear"));
    expect(amount).toHaveValue("");
    expect(screen.getByTestId("utang-entry-type").closest(".ui-standards-qdropdown")).toHaveClass(
      "ui-standards-qdropdown--outlined",
    );
    await user.click(screen.getByTestId("utang-entry-notes"));
    expect(amount).toHaveValue("0.00");
    await user.click(amount);
    expect(amount).toHaveFocus();
    expect(amount).toHaveValue("");
    await user.keyboard("{Escape}");
    expect(screen.queryByTestId("utang-entry-dialog")).not.toBeInTheDocument();
  });

  it("swaps Amount for Adjustment on Correct balance and sends the signed delta", async () => {
    const user = userEvent.setup();
    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);
    await openEntryDialog(user);
    expect(screen.getByTestId("utang-entry-amount")).toBeInTheDocument();
    expect(screen.queryByTestId("utang-entry-delta")).not.toBeInTheDocument();

    await chooseEntryType(user, "Adjustment");
    expect(screen.queryByTestId("utang-entry-amount")).not.toBeInTheDocument();
    expect(screen.queryByTestId("utang-entry-delta")).not.toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Adjustment (+/-)" })).toBeInTheDocument();
    expect(screen.getByTestId("utang-entry-delta-add")).toHaveTextContent("Add");
    expect(screen.getByTestId("utang-entry-delta-minus")).toHaveTextContent("Minus");

    await user.type(screen.getByTestId("utang-entry-notes"), "Fix double entry");
    await user.click(screen.getByTestId("utang-entry-submit"));
    expect(await screen.findByRole("alert")).toHaveTextContent("Choose Add or Minus");
    expect(recordPersonalUtangEntry).not.toHaveBeenCalled();

    await user.click(screen.getByTestId("utang-entry-delta-add"));
    expect(screen.queryByTestId("utang-entry-delta-add")).not.toBeInTheDocument();
    expect(screen.queryByTestId("utang-entry-delta-minus")).not.toBeInTheDocument();
    expect(screen.getByTestId("utang-entry-delta-sign")).toHaveTextContent("+");
    await waitFor(() => expect(screen.getByTestId("utang-entry-delta")).toHaveFocus());
    await user.click(screen.getByTestId("utang-entry-delta-sign"));
    expect(screen.queryByTestId("utang-entry-delta")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("utang-entry-delta-minus"));
    const delta = screen.getByTestId("utang-entry-delta");
    expect(screen.getByLabelText("Adjustment (+/-)")).toBe(delta);
    expect(screen.getByTestId("utang-entry-delta-sign")).toHaveTextContent("−");

    const headBalance = screen.getByTestId("utang-entry-dialog-balance");
    const before = headBalance.textContent;
    await user.type(delta, "500");
    expect(delta).toHaveValue("500");
    expect(headBalance.textContent).not.toBe(before);
    expect(headBalance).toHaveClass("utang-entry-dialog__amount--changed");
    await user.clear(delta);
    expect(headBalance.textContent).toBe(before);
    await user.type(delta, "500");
    await user.click(screen.getByTestId("utang-entry-submit"));
    await waitFor(() => expect(recordPersonalUtangEntry).toHaveBeenCalledTimes(1));
    expect(vi.mocked(recordPersonalUtangEntry).mock.calls[0]![1]).toMatchObject({
      entryType: "Adjustment",
      amount: 500,
      adjustmentDelta: -500,
      notes: "Fix double entry",
    });
  });

  function expectEntryFormReset() {
    expect(screen.getByTestId("utang-entry-type")).toHaveTextContent("Record payment");
    expect(screen.getByTestId("utang-entry-amount")).toHaveValue("");
    expect(screen.getByTestId("utang-entry-notes")).toHaveValue("");
    expect(screen.queryByTestId("utang-entry-delta")).not.toBeInTheDocument();
    expect(screen.queryByTestId("utang-entry-error")).not.toBeInTheDocument();
  }

  it("resets the whole entry form when the entry type changes or the dialog closes", async () => {
    const user = userEvent.setup();
    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);
    await openEntryDialog(user);

    await chooseEntryType(user, "Adjustment");
    await user.click(screen.getByTestId("utang-entry-delta-add"));
    expect(screen.getByTestId("utang-entry-delta-sign")).toHaveTextContent("+");
    await chooseEntryType(user, "Loan");
    await chooseEntryType(user, "Adjustment");
    expect(screen.queryByTestId("utang-entry-delta")).not.toBeInTheDocument();
    expect(screen.getByTestId("utang-entry-delta-add")).toBeInTheDocument();
    expect(screen.getByTestId("utang-entry-delta-minus")).toBeInTheDocument();

    await user.click(screen.getByTestId("utang-entry-delta-minus"));
    await user.type(screen.getByTestId("utang-entry-delta"), "200");
    await user.type(screen.getByTestId("utang-entry-notes"), "Draft note");
    await user.click(screen.getByTestId("utang-entry-cancel"));
    await openEntryDialog(user);
    expectEntryFormReset();

    await chooseEntryType(user, "Adjustment");
    expect(screen.getByTestId("utang-entry-delta-add")).toBeInTheDocument();
    await user.click(screen.getByTestId("utang-entry-delta-add"));
    await user.keyboard("{Escape}");
    await openEntryDialog(user);
    expectEntryFormReset();

    const amount = screen.getByTestId("utang-entry-amount");
    await user.clear(amount);
    await user.type(amount, "75");
    await user.click(screen.getByTestId("utang-entry-dialog-close"));
    await openEntryDialog(user);
    expectEntryFormReset();
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
    await openEntryDialog(user);
    await chooseEntryType(user, "Payment");
    const amountInput = screen.getByTestId("utang-entry-amount");
    await user.clear(amountInput);
    await user.type(amountInput, "10");
    await user.type(screen.getByTestId("utang-entry-notes"), "Partial payment");
    await user.click(screen.getByTestId("utang-entry-submit"));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      /You already have 3 entries waiting for Linked Ben's review/i,
    );
  });

  it("shows pending-limit hint when 3 outgoing proposals are pending without blocking Loan submit", async () => {
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
        confirmationSource: "None",
        wasAutoSynced: false,
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
        confirmationSource: "None",
        wasAutoSynced: false,
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
        confirmationSource: "None",
        wasAutoSynced: false,
      },
    ]);

    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);
    await openEntryDialog(user);
    expect(await screen.findByTestId("utang-pending-limit-hint")).toHaveTextContent(
      /You already have 3 entries waiting for Linked Ben's review/i,
    );
    expect(screen.getByTestId("utang-view-pending")).toHaveTextContent("View pending entries");

    // Ledger owner records Confirmed entries; client no longer blocks Loan at the pending limit.
    await chooseEntryType(user, "Loan");
    expect(screen.getByTestId("utang-entry-type")).toHaveTextContent("Add amount");
    expect(screen.getByTestId("utang-entry-submit")).not.toBeDisabled();

    await chooseEntryType(user, "Payment");
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
    const user = userEvent.setup();
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
      ledgerOwnerUserIdentityId: null,
      isLedgerOwner: true,
      shareOutcome: "Private",
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
    expect(await screen.findByTestId("utang-qa-view-contact")).toHaveTextContent("View contact details");
    await user.click(screen.getByTestId("utang-qa-view-contact"));
    const viewContact = await screen.findByTestId("utang-view-contact-dialog");
    expect(within(viewContact).getByTestId("utang-view-contact-dialog-ribbon")).toHaveTextContent(
      "View contact details",
    );
    expect(within(viewContact).getByTestId("utang-view-contact-status")).toHaveTextContent("Local contact");
    expect(within(viewContact).getByTestId("utang-view-contact-name")).toHaveTextContent("Walk-in Ana");
    expect(within(viewContact).queryByTestId("utang-view-contact-id")).not.toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(await screen.findByTestId("utang-qa-edit-contact")).toHaveTextContent("Edit contact details");
    await user.click(screen.getByTestId("utang-qa-edit-contact"));
    const editContact = await screen.findByTestId("utang-edit-contact-dialog");
    expect(within(editContact).getByTestId("utang-edit-contact-dialog-ribbon")).toHaveTextContent(
      "Edit contact details",
    );
    expect(within(editContact).getByTestId("utang-edit-contact-name")).toHaveValue("Walk-in Ana");
    await user.keyboard("{Escape}");
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
      ledgerOwnerUserIdentityId: null,
      isLedgerOwner: true,
      shareOutcome: "Private",
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
      ledgerOwnerUserIdentityId: null,
      isLedgerOwner: true,
      shareOutcome: "Private",
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
          confirmationSource: "None",
          wasAutoSynced: false,
      },
    ]);

    renderPath(`/personal/utang/relationships/${relationshipId}`);
    expect(await screen.findByTestId("utang-detail-status")).toHaveTextContent("Settled");
    expect(screen.queryByTestId("utang-entry-type")).not.toBeInTheDocument();
    expect(screen.queryByTestId("utang-qa-update-balance")).not.toBeInTheDocument();
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
      ledgerOwnerUserIdentityId: null,
      isLedgerOwner: true,
      shareOutcome: "Private",
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
      ledgerOwnerUserIdentityId: meId,
      isLedgerOwner: true,
      shareOutcome: "Shared",
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
        confirmationSource: "None",
        wasAutoSynced: false,
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
          confirmationSource: "None",
          wasAutoSynced: false,
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
      ledgerOwnerUserIdentityId: meId,
      isLedgerOwner: true,
      shareOutcome: "Shared",
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
          confirmationSource: "None",
          wasAutoSynced: false,
        },
      };
    });

    renderPath(`/personal/utang/relationships/${sharedRelationshipId}`);
    await user.click(await screen.findByTestId("utang-settle"));
    await user.type(screen.getByTestId("utang-settle-note"), "Paid in full");
    await user.click(screen.getByTestId("utang-settle-confirm"));
    expect(await screen.findByTestId("utang-settle-awaiting")).toBeInTheDocument();
    expect(settlePersonalDebtRelationship).toHaveBeenCalled();
  });
});

describe("Personal Utang anti-spam helpers", () => {
  const t = (key: keyof typeof en) => en[key];

  it("projects the balance after the typed entry", () => {
    expect(projectUtangBalance(1000, "Payment", "250.00", "")).toBe(750);
    expect(projectUtangBalance(1000, "Loan", "250.50", "")).toBe(1250.5);
    expect(projectUtangBalance(1000, "Adjustment", "999", "-300")).toBe(700);
    expect(projectUtangBalance(1000, "Adjustment", "", "")).toBe(1000);
    expect(projectUtangBalance(1000, "Payment", "", "")).toBe(1000);
  });

  it("parses signed balance corrections", () => {
    expect(parseAdjustmentDelta("+500")).toBe(500);
    expect(parseAdjustmentDelta("-1,250.50")).toBe(-1250.5);
    expect(parseAdjustmentDelta(" 75 ")).toBe(75);
    expect(parseAdjustmentDelta("")).toBeNull();
    expect(parseAdjustmentDelta("0")).toBeNull();
    expect(parseAdjustmentDelta("-0.00")).toBeNull();
    expect(parseAdjustmentDelta("abc")).toBeNull();
    expect(parseAdjustmentDelta("1.234")).toBeNull();
  });

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
