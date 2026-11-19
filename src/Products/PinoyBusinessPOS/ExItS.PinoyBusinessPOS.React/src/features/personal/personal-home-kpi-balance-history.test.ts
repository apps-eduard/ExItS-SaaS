import { describe, expect, it } from "vitest";
import type {
  PersonalDebtRelationshipSummaryDto,
  PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import {
  buildPersonalHomeKpiPeriodOpening,
  buildPersonalHomeKpiSeriesValues,
  classifyPersonalHomeKpiSeries,
  countActivePersonalHomeKpiRelationships,
  summarizePersonalYourRecordCounts,
  isActivePersonalHomeKpiRelationship,
  relationshipHistoryReconstructible,
  utangEffectiveBalanceAt,
  type PersonalHomeKpiRelationshipHistory,
  type PersonalHomeKpiSeriesKey,
} from "@/features/personal/personal-home-kpi-balance-history";
import { resolvePersonalHomeKpiDatePreset } from "@/features/personal/personal-home-kpi-date-range";

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

function entry(partial: Partial<PersonalUtangEntryDto> & Pick<PersonalUtangEntryDto, "createdAtUtc" | "signedDelta" | "balanceAfter">): PersonalUtangEntryDto {
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
    seriesKey: classifyPersonalHomeKpiSeries(relationship),
    entries,
    historyError: opts?.historyError ?? false,
    historyLoaded: opts?.historyLoaded ?? true,
  };
}

describe("personal home KPI balance history", () => {
  it("classifies ownership and perspective into four series keys", () => {
    expect(classifyPersonalHomeKpiSeries({ perspective: "Lent", isLedgerOwner: true })).toBe(
      "my-record-lent",
    );
    expect(classifyPersonalHomeKpiSeries({ perspective: "Borrowed", isLedgerOwner: true })).toBe(
      "my-record-owe",
    );
    expect(classifyPersonalHomeKpiSeries({ perspective: "Lent", isLedgerOwner: false })).toBe(
      "shared-lent",
    );
    expect(classifyPersonalHomeKpiSeries({ perspective: "Borrowed", isLedgerOwner: false })).toBe(
      "shared-owe",
    );
  });

  it("counts unique active relationships per series without pending-only rows", () => {
    const activeOwnedLent = rel({
      perspective: "Lent",
      isLedgerOwner: true,
      currentBalance: 40,
    });
    const closedShared = rel({
      id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
      perspective: "Borrowed",
      isLedgerOwner: false,
      currentBalance: 20,
      status: "Closed",
    });
    const zeroBalanceNoDue = rel({
      id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
      perspective: "Lent",
      isLedgerOwner: true,
      currentBalance: 0,
    });
    const sharedOweActive = rel({
      id: "ffffffff-ffff-ffff-ffff-ffffffffffff",
      perspective: "Borrowed",
      isLedgerOwner: false,
      currentBalance: 15,
    });

    expect(isActivePersonalHomeKpiRelationship(activeOwnedLent)).toBe(true);
    expect(isActivePersonalHomeKpiRelationship(closedShared)).toBe(false);
    expect(isActivePersonalHomeKpiRelationship(zeroBalanceNoDue)).toBe(false);

    const buckets = countActivePersonalHomeKpiRelationships([
      activeOwnedLent,
      closedShared,
      zeroBalanceNoDue,
      sharedOweActive,
    ]);
    expect(buckets).toEqual({
      "my-record-lent": 1,
      "my-record-owe": 0,
      "shared-lent": 0,
      "shared-owe": 1,
    });
    expect(summarizePersonalYourRecordCounts(buckets)).toEqual({ myRecords: 1, shared: 1 });
  });

  it("renders a flat zero line when balance stays zero", () => {
    const relationship = rel({ perspective: "Lent", isLedgerOwner: true, currentBalance: 0 });
    const values = buildPersonalHomeKpiSeriesValues({
      bucket: "my-record-lent",
      relationships: [bundle(relationship, [])],
      fromDate: "2026-01-01",
      toDate: "2026-01-03",
      headlineAmount: 0,
    });
    expect(values).toEqual([0, 0, 0]);
  });

  it("declines to zero when repayments post in history", () => {
    const relationship = rel({ perspective: "Lent", isLedgerOwner: true, currentBalance: 0 });
    const history = [
      entry({
        id: "11111111-1111-1111-1111-111111111111",
        createdAtUtc: "2026-01-01T10:00:00.000Z",
        signedDelta: 100,
        balanceAfter: 100,
      }),
      entry({
        id: "22222222-2222-2222-2222-222222222222",
        createdAtUtc: "2026-01-02T10:00:00.000Z",
        signedDelta: -100,
        balanceAfter: 0,
        entryType: "Payment",
      }),
    ];
    const values = buildPersonalHomeKpiSeriesValues({
      bucket: "my-record-lent",
      relationships: [bundle(relationship, history)],
      fromDate: "2026-01-01",
      toDate: "2026-01-02",
      headlineAmount: 0,
    });
    expect(values).toEqual([100, 0]);
  });

  it("carries forward debt before the selected range", () => {
    const relationship = rel({ perspective: "Borrowed", isLedgerOwner: true, currentBalance: 80 });
    const history = [
      entry({
        createdAtUtc: "2025-12-01T10:00:00.000Z",
        signedDelta: 80,
        balanceAfter: 80,
      }),
    ];
    const buildInput = {
      bucket: "my-record-owe" as const,
      relationships: [bundle(relationship, history)],
      fromDate: "2026-01-01",
      toDate: "2026-01-02",
      headlineAmount: 80,
    };
    const values = buildPersonalHomeKpiSeriesValues(buildInput);
    const opening = buildPersonalHomeKpiPeriodOpening(buildInput);
    expect(values).toEqual([80, 80]);
    expect(opening).toBe(80);
  });

  it("includes pending signed deltas until resolved", () => {
    const at = Date.parse("2026-01-01T23:59:59.999Z");
    const history = [
      entry({
        createdAtUtc: "2026-01-01T08:00:00.000Z",
        signedDelta: 50,
        balanceAfter: 50,
      }),
      entry({
        id: "33333333-3333-3333-3333-333333333333",
        createdAtUtc: "2026-01-01T12:00:00.000Z",
        signedDelta: 20,
        balanceAfter: 50,
        status: "Pending",
        affectsBalance: false,
        resolvedAtUtc: null,
      }),
    ];
    expect(utangEffectiveBalanceAt(history, at)).toBe(70);

    const afterResolve = utangEffectiveBalanceAt(
      [
        history[0]!,
        {
          ...history[1]!,
          resolvedAtUtc: "2026-01-02T09:00:00.000Z",
        },
        entry({
          id: "44444444-4444-4444-4444-444444444444",
          createdAtUtc: "2026-01-02T09:00:00.000Z",
          signedDelta: 20,
          balanceAfter: 70,
          status: "Confirmed",
        }),
      ],
      Date.parse("2026-01-02T23:59:59.999Z"),
    );
    expect(afterResolve).toBe(70);
  });

  it("hides series when multiple currencies share a bucket", () => {
    const php = rel({
      perspective: "Lent",
      isLedgerOwner: true,
      currencyCode: "PHP",
      currentBalance: 10,
    });
    const usd = rel({
      id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
      perspective: "Lent",
      isLedgerOwner: true,
      currencyCode: "USD",
      currentBalance: 5,
    });
    const values = buildPersonalHomeKpiSeriesValues({
      bucket: "my-record-lent",
      relationships: [
        bundle(php, [
          entry({ createdAtUtc: "2026-01-01T10:00:00.000Z", signedDelta: 10, balanceAfter: 10 }),
        ]),
        bundle(usd, [
          entry({
            id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
            createdAtUtc: "2026-01-01T11:00:00.000Z",
            signedDelta: 5,
            balanceAfter: 5,
          }),
        ]),
      ],
      fromDate: "2026-01-01",
      toDate: "2026-01-01",
      headlineAmount: 15,
    });
    expect(values).toBeNull();
  });

  it("respects shared ledger ownership buckets without double counting", () => {
    const owned = rel({ perspective: "Lent", isLedgerOwner: true, currentBalance: 30 });
    const shared = rel({
      id: "ffffffff-ffff-ffff-ffff-ffffffffffff",
      perspective: "Lent",
      isLedgerOwner: false,
      currentBalance: 30,
    });
    const ownedValues = buildPersonalHomeKpiSeriesValues({
      bucket: "my-record-lent",
      relationships: [
        bundle(owned, [
          entry({ createdAtUtc: "2026-01-01T10:00:00.000Z", signedDelta: 30, balanceAfter: 30 }),
        ]),
        bundle(shared, [
          entry({
            id: "10101010-1010-1010-1010-101010101010",
            createdAtUtc: "2026-01-01T11:00:00.000Z",
            signedDelta: 30,
            balanceAfter: 30,
          }),
        ]),
      ],
      fromDate: "2026-01-01",
      toDate: "2026-01-01",
      headlineAmount: 30,
    });
    const sharedValues = buildPersonalHomeKpiSeriesValues({
      bucket: "shared-lent",
      relationships: [
        bundle(owned, [
          entry({ createdAtUtc: "2026-01-01T10:00:00.000Z", signedDelta: 30, balanceAfter: 30 }),
        ]),
        bundle(shared, [
          entry({
            id: "10101010-1010-1010-1010-101010101010",
            createdAtUtc: "2026-01-01T11:00:00.000Z",
            signedDelta: 30,
            balanceAfter: 30,
          }),
        ]),
      ],
      fromDate: "2026-01-01",
      toDate: "2026-01-01",
      headlineAmount: 30,
    });
    expect(ownedValues).toEqual([30]);
    expect(sharedValues).toEqual([30]);
  });

  it("recomputes when the date preset range changes", () => {
    const relationship = rel({ perspective: "Lent", isLedgerOwner: true, currentBalance: 50 });
    const history = [
      entry({
        createdAtUtc: "2026-01-05T10:00:00.000Z",
        signedDelta: 50,
        balanceAfter: 50,
      }),
    ];
    const week = resolvePersonalHomeKpiDatePreset("thisWeek", new Date("2026-01-07T12:00:00.000Z"));
    const month = resolvePersonalHomeKpiDatePreset("thisMonth", new Date("2026-01-07T12:00:00.000Z"));
    const weekValues = buildPersonalHomeKpiSeriesValues({
      bucket: "my-record-lent",
      relationships: [bundle(relationship, history)],
      fromDate: week.fromDate,
      toDate: week.toDate,
      headlineAmount: 50,
    });
    const monthValues = buildPersonalHomeKpiSeriesValues({
      bucket: "my-record-lent",
      relationships: [bundle(relationship, history)],
      fromDate: month.fromDate,
      toDate: month.toDate,
      headlineAmount: 50,
    });
    expect(weekValues?.length).toBeGreaterThan(0);
    expect(monthValues?.length).toBeGreaterThan(weekValues!.length);
  });

  it("hides graph when history cannot be reconstructed for a non-zero headline", () => {
    const relationship = rel({ perspective: "Lent", isLedgerOwner: true, currentBalance: 120 });
    expect(
      relationshipHistoryReconstructible(relationship, {
        entries: [],
        historyError: false,
        historyLoaded: true,
      }),
    ).toBe(false);
    const values = buildPersonalHomeKpiSeriesValues({
      bucket: "my-record-lent" satisfies PersonalHomeKpiSeriesKey,
      relationships: [bundle(relationship, [], { historyLoaded: true })],
      fromDate: "2026-01-01",
      toDate: "2026-01-02",
      headlineAmount: 120,
    });
    expect(values).toBeNull();
  });
});