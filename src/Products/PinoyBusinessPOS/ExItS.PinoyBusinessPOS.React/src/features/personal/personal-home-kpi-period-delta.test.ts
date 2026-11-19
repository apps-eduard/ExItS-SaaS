import { describe, expect, it } from "vitest";
import type {
  PersonalDebtRelationshipSummaryDto,
  PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import {
  buildPersonalHomeKpiPeriodOpening,
  buildPersonalHomeKpiSeriesValues,
  type PersonalHomeKpiRelationshipHistory,
} from "@/features/personal/personal-home-kpi-balance-history";
import {
  personalHomeKpiPeriodDeltaTone,
  personalHomeKpiPeriodDeltaView,
} from "@/features/personal/personal-home-kpi-period-delta";

const RELATIONSHIP_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const USER_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

function rel(
  partial: Partial<PersonalDebtRelationshipSummaryDto> &
    Pick<PersonalDebtRelationshipSummaryDto, "perspective" | "isLedgerOwner">,
): PersonalDebtRelationshipSummaryDto {
  return {
    id: partial.id ?? RELATIONSHIP_ID,
    perspective: partial.perspective,
    creditorUserIdentityId: null,
    creditorContactId: null,
    debtorUserIdentityId: null,
    debtorContactId: null,
    currencyCode: partial.currencyCode ?? "PHP",
    currentBalance: partial.currentBalance ?? 0,
    dueDateUtc: null,
    status: partial.status ?? "Active",
    version: 1,
    updatedAtUtc: "2026-01-15T12:00:00.000Z",
    isSharedLedger: partial.isSharedLedger ?? false,
    isPrivate: partial.isPrivate ?? true,
    ledgerOwnerUserIdentityId: null,
    isLedgerOwner: partial.isLedgerOwner,
    shareOutcome: null,
  };
}

function entry(
  partial: Partial<PersonalUtangEntryDto> &
    Pick<PersonalUtangEntryDto, "createdAtUtc" | "signedDelta" | "balanceAfter">,
): PersonalUtangEntryDto {
  return {
    id: partial.id ?? "cccccccc-cccc-cccc-cccc-cccccccccccc",
    relationshipId: RELATIONSHIP_ID,
    entryType: partial.entryType ?? "Loan",
    amount: partial.amount ?? Math.abs(partial.signedDelta),
    signedDelta: partial.signedDelta,
    balanceAfter: partial.balanceAfter,
    notes: null,
    dueDateUtc: null,
    createdByUserIdentityId: USER_ID,
    createdAtUtc: partial.createdAtUtc,
    status: partial.status ?? "Confirmed",
    resolvedByUserIdentityId: partial.resolvedByUserIdentityId ?? null,
    resolvedAtUtc: partial.resolvedAtUtc ?? null,
    disputeReason: null,
    canConfirm: false,
    canDispute: false,
    canCancel: false,
    affectsBalance: partial.affectsBalance ?? partial.status !== "Pending",
    isSharedLedger: false,
    intent: "Regular",
    settlementBalanceSnapshot: null,
    isSettlement: false,
    confirmationSource: "None",
    wasAutoSynced: false,
  };
}

function bundle(
  relationship: PersonalDebtRelationshipSummaryDto,
  entries: PersonalUtangEntryDto[] | undefined,
  opts?: { historyError?: boolean; historyLoaded?: boolean },
): PersonalHomeKpiRelationshipHistory {
  return {
    relationship,
    seriesKey:
      relationship.isLedgerOwner !== false
        ? relationship.perspective !== "Borrowed"
          ? "my-record-lent"
          : "my-record-owe"
        : relationship.perspective !== "Borrowed"
          ? "shared-lent"
          : "shared-owe",
    entries,
    historyError: opts?.historyError ?? false,
    historyLoaded: opts?.historyLoaded ?? true,
  };
}

describe("personalHomeKpiPeriodDeltaView", () => {
  it("hides when the series is missing", () => {
    expect(personalHomeKpiPeriodDeltaView(null, "lent")).toEqual({ visible: false });
    expect(personalHomeKpiPeriodDeltaView([], "owe")).toEqual({ visible: false });
  });

  it("hides when opening is zero and closing is positive", () => {
    const view = personalHomeKpiPeriodDeltaView([0, 40, 80], "lent", 0);
    expect(view).toEqual({ visible: false });
  });

  it("shows unchanged at zero percent", () => {
    expect(personalHomeKpiPeriodDeltaView([120, 120, 120], "lent", 120)).toEqual({
      visible: true,
      label: "→ 0.0%",
      tone: "neutral",
    });
  });

  it("formats increasing and decreasing percentages from opening and closing", () => {
    expect(personalHomeKpiPeriodDeltaView([100, 150], "lent", 100)).toEqual({
      visible: true,
      label: "↑ +50.0%",
      tone: "good",
    });
    expect(personalHomeKpiPeriodDeltaView([200, 150], "lent", 200)).toEqual({
      visible: true,
      label: "↓ -25.0%",
      tone: "caution",
    });
  });

  it("shows flat zero percent when both opening and closing are zero with complete history", () => {
    expect(personalHomeKpiPeriodDeltaView([0, 0], "lent", 0)).toEqual({
      visible: true,
      label: "→ 0.0%",
      tone: "neutral",
    });
  });

  it("inverts tone meaning for I Owe", () => {
    expect(personalHomeKpiPeriodDeltaView([50, 100], "owe", 50)).toEqual({
      visible: true,
      label: "↑ +100.0%",
      tone: "caution",
    });
    expect(personalHomeKpiPeriodDeltaView([100, 40], "owe", 100)).toEqual({
      visible: true,
      label: "↓ -60.0%",
      tone: "good",
    });
  });

  it("hides when history is incomplete (null series from builder)", () => {
    const relationship = rel({ perspective: "Lent", isLedgerOwner: true, currentBalance: 90 });
    const buildInput = {
      bucket: "my-record-lent" as const,
      relationships: [bundle(relationship, [], { historyLoaded: true })],
      fromDate: "2026-01-01",
      toDate: "2026-01-03",
      headlineAmount: 90,
    };
    const values = buildPersonalHomeKpiSeriesValues(buildInput);
    const opening = buildPersonalHomeKpiPeriodOpening(buildInput);
    expect(values).toBeNull();
    expect(opening).toBeNull();
    expect(personalHomeKpiPeriodDeltaView(values, "lent", opening)).toEqual({ visible: false });
  });

  it("derives delta from shared-with-me series without mixing buckets", () => {
    const shared = rel({
      id: "ffffffff-ffff-ffff-ffff-ffffffffffff",
      perspective: "Borrowed",
      isLedgerOwner: false,
      currentBalance: 60,
    });
    const buildInput = {
      bucket: "shared-owe" as const,
      relationships: [
        bundle(shared, [
          entry({
            createdAtUtc: "2026-01-01T08:00:00.000Z",
            signedDelta: 40,
            balanceAfter: 40,
          }),
          entry({
            id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
            createdAtUtc: "2026-01-03T08:00:00.000Z",
            signedDelta: 20,
            balanceAfter: 60,
          }),
        ]),
      ],
      fromDate: "2026-01-01",
      toDate: "2026-01-03",
      headlineAmount: 60,
    };
    const values = buildPersonalHomeKpiSeriesValues(buildInput);
    const opening = buildPersonalHomeKpiPeriodOpening(buildInput);
    expect(values).toEqual([40, 40, 60]);
    expect(opening).toBe(0);
    expect(personalHomeKpiPeriodDeltaView(values, "owe", opening)).toEqual({ visible: false });
  });

  it("uses period opening before the range when debt predates the chart", () => {
    const relationship = rel({ perspective: "Lent", isLedgerOwner: true, currentBalance: 100 });
    const history = [
      entry({
        createdAtUtc: "2025-12-01T10:00:00.000Z",
        signedDelta: 100,
        balanceAfter: 100,
      }),
    ];
    const buildInput = {
      bucket: "my-record-lent" as const,
      relationships: [bundle(relationship, history)],
      fromDate: "2026-01-01",
      toDate: "2026-01-03",
      headlineAmount: 100,
    };
    const values = buildPersonalHomeKpiSeriesValues(buildInput);
    const opening = buildPersonalHomeKpiPeriodOpening(buildInput);
    expect(opening).toBe(100);
    expect(values).toEqual([100, 100, 100]);
    expect(personalHomeKpiPeriodDeltaView(values, "lent", opening)).toEqual({
      visible: true,
      label: "→ 0.0%",
      tone: "neutral",
    });
  });

  it("shows percent when period opening is positive even if the first spark point is still zero", () => {
    const relationship = rel({ perspective: "Lent", isLedgerOwner: true, currentBalance: 150 });
    const history = [
      entry({
        createdAtUtc: "2025-12-01T10:00:00.000Z",
        signedDelta: 100,
        balanceAfter: 100,
      }),
      entry({
        id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        createdAtUtc: "2026-01-02T10:00:00.000Z",
        signedDelta: 50,
        balanceAfter: 150,
      }),
    ];
    const buildInput = {
      bucket: "my-record-lent" as const,
      relationships: [bundle(relationship, history)],
      fromDate: "2026-01-01",
      toDate: "2026-01-02",
      headlineAmount: 150,
    };
    const values = buildPersonalHomeKpiSeriesValues(buildInput);
    const opening = buildPersonalHomeKpiPeriodOpening(buildInput);
    expect(values).toEqual([100, 150]);
    expect(opening).toBe(100);
    expect(personalHomeKpiPeriodDeltaView(values, "lent", opening)).toEqual({
      visible: true,
      label: "↑ +50.0%",
      tone: "good",
    });
  });
});

describe("personalHomeKpiPeriodDeltaTone", () => {
  it("maps lent and owe directions", () => {
    expect(personalHomeKpiPeriodDeltaTone("lent", "up")).toBe("good");
    expect(personalHomeKpiPeriodDeltaTone("lent", "down")).toBe("caution");
    expect(personalHomeKpiPeriodDeltaTone("owe", "up")).toBe("caution");
    expect(personalHomeKpiPeriodDeltaTone("owe", "down")).toBe("good");
    expect(personalHomeKpiPeriodDeltaTone("owe", "flat")).toBe("neutral");
  });
});