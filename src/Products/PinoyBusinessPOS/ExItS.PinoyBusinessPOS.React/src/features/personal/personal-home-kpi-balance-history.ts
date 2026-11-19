import type {
  PersonalDebtRelationshipSummaryDto,
  PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import {
  endOfUtcDateMs,
  enumerateUtcDateKeys,
  startOfUtcPeriodOpeningMs,
} from "@/features/personal/personal-home-kpi-date-range";

export type PersonalHomeKpiSeriesKey =
  | "my-record-lent"
  | "my-record-owe"
  | "shared-lent"
  | "shared-owe";

export const PERSONAL_HOME_KPI_SERIES_KEYS: readonly PersonalHomeKpiSeriesKey[] = [
  "my-record-lent",
  "my-record-owe",
  "shared-lent",
  "shared-owe",
] as const;

export function classifyPersonalHomeKpiSeries(
  row: Pick<PersonalDebtRelationshipSummaryDto, "perspective" | "isLedgerOwner">,
): PersonalHomeKpiSeriesKey {
  const lent = row.perspective !== "Borrowed";
  const mine = row.isLedgerOwner !== false;
  if (mine && lent) return "my-record-lent";
  if (mine && !lent) return "my-record-owe";
  if (!mine && lent) return "shared-lent";
  return "shared-owe";
}

/** Mirrors {@link isActiveUtangAccount} — status Active plus balance or due date still tracked. */
export function isActivePersonalHomeKpiRelationship(
  row: Pick<PersonalDebtRelationshipSummaryDto, "status" | "currentBalance" | "dueDateUtc">,
): boolean {
  if (row.status.toLowerCase() !== "active") {
    return false;
  }
  return row.currentBalance > 0 || Boolean(row.dueDateUtc);
}

export function countActivePersonalHomeKpiRelationships(
  relationships: ReadonlyArray<PersonalDebtRelationshipSummaryDto>,
): Record<PersonalHomeKpiSeriesKey, number> {
  const counts: Record<PersonalHomeKpiSeriesKey, number> = {
    "my-record-lent": 0,
    "my-record-owe": 0,
    "shared-lent": 0,
    "shared-owe": 0,
  };
  for (const row of relationships) {
    if (!isActivePersonalHomeKpiRelationship(row)) {
      continue;
    }
    counts[classifyPersonalHomeKpiSeries(row)] += 1;
  }
  return counts;
}

export function summarizePersonalYourRecordCounts(
  counts: Record<PersonalHomeKpiSeriesKey, number>,
): { myRecords: number; shared: number } {
  return {
    myRecords: counts["my-record-lent"] + counts["my-record-owe"],
    shared: counts["shared-lent"] + counts["shared-owe"],
  };
}

export function mergePersonalUtangRelationships(
  lent: ReadonlyArray<PersonalDebtRelationshipSummaryDto>,
  borrowed: ReadonlyArray<PersonalDebtRelationshipSummaryDto>,
): PersonalDebtRelationshipSummaryDto[] {
  const byId = new Map<string, PersonalDebtRelationshipSummaryDto>();
  for (const row of lent) {
    byId.set(row.id, row);
  }
  for (const row of borrowed) {
    byId.set(row.id, row);
  }
  return [...byId.values()];
}

/** Mirror dashboard display: confirmed balanceAfter through time, plus pending signed deltas until resolved. */
export function utangEffectiveBalanceAt(
  entries: ReadonlyArray<PersonalUtangEntryDto>,
  atMs: number,
): number {
  const sorted = [...entries].sort(
    (a, b) =>
      Date.parse(a.createdAtUtc) - Date.parse(b.createdAtUtc) || a.id.localeCompare(b.id),
  );

  let confirmed = 0;
  let hasConfirmed = false;

  for (const entry of sorted) {
    if (Date.parse(entry.createdAtUtc) > atMs) {
      break;
    }
    if (entry.status.toLowerCase() === "confirmed" && entry.affectsBalance !== false) {
      confirmed = entry.balanceAfter;
      hasConfirmed = true;
    }
  }

  let pending = 0;
  for (const entry of sorted) {
    if (Date.parse(entry.createdAtUtc) > atMs) {
      continue;
    }
    if (entry.status.toLowerCase() !== "pending") {
      continue;
    }
    const resolved = entry.resolvedAtUtc ? Date.parse(entry.resolvedAtUtc) : null;
    if (resolved !== null && Number.isFinite(resolved) && resolved <= atMs) {
      continue;
    }
    pending += entry.signedDelta;
  }

  return (hasConfirmed ? confirmed : 0) + pending;
}

export type PersonalHomeKpiRelationshipHistory = {
  relationship: PersonalDebtRelationshipSummaryDto;
  seriesKey: PersonalHomeKpiSeriesKey;
  entries: PersonalUtangEntryDto[] | undefined;
  historyError: boolean;
  historyLoaded: boolean;
};

export function relationshipHistoryReconstructible(
  relationship: Pick<PersonalDebtRelationshipSummaryDto, "currentBalance">,
  row: Pick<PersonalHomeKpiRelationshipHistory, "entries" | "historyError" | "historyLoaded">,
): boolean {
  if (row.historyError || !row.historyLoaded) {
    return false;
  }
  if ((row.entries?.length ?? 0) > 0) {
    return true;
  }
  return relationship.currentBalance === 0;
}

type PersonalHomeKpiSeriesBuildInput = {
  bucket: PersonalHomeKpiSeriesKey;
  relationships: ReadonlyArray<PersonalHomeKpiRelationshipHistory>;
  fromDate: string;
  toDate: string;
  headlineAmount: number;
};

function resolvePersonalHomeKpiBucketRows(
  input: PersonalHomeKpiSeriesBuildInput,
): PersonalHomeKpiRelationshipHistory[] | null {
  const bucketRows = input.relationships.filter((row) => row.seriesKey === input.bucket);
  const dayKeys = enumerateUtcDateKeys(input.fromDate, input.toDate);
  if (dayKeys.length === 0) {
    return null;
  }

  if (bucketRows.length === 0) {
    if (input.headlineAmount === 0) {
      return [];
    }
    return null;
  }

  const currencies = new Set(bucketRows.map((row) => row.relationship.currencyCode));
  if (currencies.size > 1) {
    return null;
  }

  for (const row of bucketRows) {
    if (!relationshipHistoryReconstructible(row.relationship, row)) {
      return null;
    }
  }

  return bucketRows;
}

function sumBucketBalanceAt(
  bucketRows: ReadonlyArray<PersonalHomeKpiRelationshipHistory>,
  atMs: number,
): number {
  return bucketRows.reduce((sum, row) => {
    return sum + utangEffectiveBalanceAt(row.entries ?? [], atMs);
  }, 0);
}

/** Outstanding total at the instant before the range start (period opening), from history only. */
export function buildPersonalHomeKpiPeriodOpening(
  input: PersonalHomeKpiSeriesBuildInput,
): number | null {
  const bucketRows = resolvePersonalHomeKpiBucketRows(input);
  if (bucketRows === null) {
    return null;
  }
  if (bucketRows.length === 0) {
    return 0;
  }
  const openingMs = startOfUtcPeriodOpeningMs(input.fromDate);
  if (openingMs === null) {
    return null;
  }
  return sumBucketBalanceAt(bucketRows, openingMs);
}

export function buildPersonalHomeKpiSeriesValues(
  input: PersonalHomeKpiSeriesBuildInput,
): number[] | null {
  const dayKeys = enumerateUtcDateKeys(input.fromDate, input.toDate);
  if (dayKeys.length === 0) {
    return null;
  }

  const bucketRows = resolvePersonalHomeKpiBucketRows(input);
  if (bucketRows === null) {
    return null;
  }
  if (bucketRows.length === 0) {
    return dayKeys.map(() => 0);
  }

  return dayKeys.map((dayKey) => {
    const atMs = endOfUtcDateMs(dayKey);
    return sumBucketBalanceAt(bucketRows, atMs);
  });
}