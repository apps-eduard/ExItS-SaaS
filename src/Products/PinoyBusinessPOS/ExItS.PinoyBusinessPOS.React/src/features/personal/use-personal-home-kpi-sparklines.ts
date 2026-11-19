import { useMemo } from "react";
import { useQueries, useQuery } from "@tanstack/react-query";
import type { PersonalDashboardDto } from "@/api/platform/personal-dashboard-client";
import {
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalUtangHistory,
} from "@/api/platform/personal-utang-client";
import type { ReportDateRangeValue } from "@/features/reports/report-date-range";
import {
  buildPersonalHomeKpiPeriodOpening,
  buildPersonalHomeKpiSeriesValues,
  classifyPersonalHomeKpiSeries,
  countActivePersonalHomeKpiRelationships,
  mergePersonalUtangRelationships,
  PERSONAL_HOME_KPI_SERIES_KEYS,
  type PersonalHomeKpiRelationshipHistory,
  type PersonalHomeKpiSeriesKey,
} from "@/features/personal/personal-home-kpi-balance-history";
import { useBrowserOnline } from "@/connectivity/browser-online";

function headlineForSeries(
  dashboard: PersonalDashboardDto,
  key: PersonalHomeKpiSeriesKey,
): number {
  switch (key) {
    case "my-record-lent":
      return dashboard.totalLentBalance;
    case "my-record-owe":
      return dashboard.totalBorrowedBalance;
    case "shared-lent":
      return dashboard.sharedWithMeLentBalance ?? 0;
    case "shared-owe":
      return dashboard.sharedWithMeBorrowedBalance ?? 0;
    default:
      return 0;
  }
}

export function usePersonalHomeKpiSparklines(
  dashboard: PersonalDashboardDto,
  range: ReportDateRangeValue,
): {
  series: Record<PersonalHomeKpiSeriesKey, number[] | null>;
  periodOpening: Record<PersonalHomeKpiSeriesKey, number | null>;
  activeCounts: Record<PersonalHomeKpiSeriesKey, number>;
  loading: boolean;
} {
  const online = useBrowserOnline();

  const lentQuery = useQuery({
    queryKey: ["personal", "utang", "lent"],
    queryFn: ({ signal }) => listLentRelationships(signal),
    enabled: online,
  });

  const borrowedQuery = useQuery({
    queryKey: ["personal", "utang", "owe"],
    queryFn: ({ signal }) => listBorrowedRelationships(signal),
    enabled: online,
  });

  const relationships = useMemo(() => {
    if (!lentQuery.data || !borrowedQuery.data) {
      return [];
    }
    return mergePersonalUtangRelationships(lentQuery.data, borrowedQuery.data);
  }, [borrowedQuery.data, lentQuery.data]);

  const historyQueries = useQueries({
    queries: relationships.map((relationship) => ({
      queryKey: ["personal", "utang", "history", relationship.id],
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        listPersonalUtangHistory(relationship.id, signal),
      enabled: online && relationships.length > 0,
      staleTime: 5 * 60 * 1000,
    })),
  });

  const historyBundle = useMemo((): PersonalHomeKpiRelationshipHistory[] => {
    return relationships.map((relationship, index) => {
      const query = historyQueries[index];
      return {
        relationship,
        seriesKey: classifyPersonalHomeKpiSeries(relationship),
        entries: query?.data,
        historyError: query?.isError ?? false,
        historyLoaded: Boolean(query?.isSuccess || query?.isError),
      };
    });
  }, [historyQueries, relationships]);

  const { series, periodOpening } = useMemo(() => {
    const values = {} as Record<PersonalHomeKpiSeriesKey, number[] | null>;
    const openings = {} as Record<PersonalHomeKpiSeriesKey, number | null>;
    for (const key of PERSONAL_HOME_KPI_SERIES_KEYS) {
      const buildInput = {
        bucket: key,
        relationships: historyBundle,
        fromDate: range.fromDate,
        toDate: range.toDate,
        headlineAmount: headlineForSeries(dashboard, key),
      };
      values[key] = buildPersonalHomeKpiSeriesValues(buildInput);
      openings[key] = buildPersonalHomeKpiPeriodOpening(buildInput);
    }
    return { series: values, periodOpening: openings };
  }, [dashboard, historyBundle, range.fromDate, range.toDate]);

  const activeCounts = useMemo(
    () => countActivePersonalHomeKpiRelationships(relationships),
    [relationships],
  );

  const loading =
    lentQuery.isLoading ||
    borrowedQuery.isLoading ||
    historyQueries.some((query) => query.isLoading);

  return { series, periodOpening, activeCounts, loading };
}