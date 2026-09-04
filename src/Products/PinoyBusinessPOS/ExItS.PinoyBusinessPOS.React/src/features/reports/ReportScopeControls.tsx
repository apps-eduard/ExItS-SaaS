import { useId, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { listOrganizationBranches } from "@/api/platform/platform-auth-client";
import { cn } from "@/lib/cn";
import { useI18n } from "@/i18n/I18nProvider";
import type {
  ReportBranchScopeSelection,
  ReportScopeMode,
} from "@/features/reports/report-branch-scope";

type ReportScopeControlsProps = {
  scopeMode: ReportScopeMode;
  organizationId: string;
  currentBranchId: string | null | undefined;
  currentBranchName: string | null | undefined;
  selection: ReportBranchScopeSelection;
  onSelectionChange: (next: ReportBranchScopeSelection) => void;
  allowAllBranches: boolean;
  loading?: boolean;
  /** Compact select for dashboard toolbar (no helper copy). */
  compact?: boolean;
};

export function ReportScopeControls({
  scopeMode,
  organizationId,
  currentBranchId,
  currentBranchName,
  selection,
  onSelectionChange,
  allowAllBranches,
  loading = false,
  compact = false,
}: ReportScopeControlsProps) {
  const { t } = useI18n();
  const selectId = useId();

  const branchesQuery = useQuery({
    queryKey: ["report-scope-branches", organizationId],
    enabled: scopeMode === "branch" && Boolean(organizationId),
    queryFn: async () => {
      const result = await listOrganizationBranches(organizationId);
      if (!result.ok) {
        throw new Error("branches");
      }
      return result.branches.filter((b) => b.status.toLowerCase() !== "inactive");
    },
    staleTime: 60_000,
  });

  const branches = branchesQuery.data ?? [];
  const singleBranch = branches.length <= 1;

  const displayName = useMemo(() => {
    if (scopeMode === "organization_only") {
      return t("reports.scope.allBranches");
    }
    if (selection.mode === "all") {
      return t("reports.scope.allBranches");
    }
    if (selection.mode === "branch") {
      const named = branches.find((b) => b.id === selection.branchId);
      return named?.name ?? selection.branchId;
    }
    return currentBranchName ?? t("reports.scope.currentBranch");
  }, [scopeMode, selection, branches, currentBranchName, t]);

  if (scopeMode === "organization_only") {
    return (
      <div
        className={cn("flex min-w-0 flex-col gap-1.5", compact && "dashboard-scope-compact")}
        data-testid="report-scope-org-only"
      >
        {!compact ? (
          <span className="text-[length:var(--exits-text-sm)] font-semibold">
            {t("reports.scope.label")}
          </span>
        ) : null}
        <span
          className="text-[length:var(--exits-text-sm)]"
          data-testid="report-scope-value"
        >
          {t("reports.scope.allBranches")}
        </span>
        {!compact ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("reports.scope.orgOnlyNote")}
          </p>
        ) : null}
      </div>
    );
  }

  if (singleBranch && !allowAllBranches) {
    return (
      <div
        className={cn("flex min-w-0 flex-col gap-1.5", compact && "dashboard-scope-compact")}
        data-testid="report-scope-single"
      >
        {!compact ? (
          <span className="text-[length:var(--exits-text-sm)] font-semibold">
            {t("reports.scope.label")}
          </span>
        ) : null}
        <span className="text-[length:var(--exits-text-sm)]" data-testid="report-scope-value">
          {displayName}
        </span>
      </div>
    );
  }

  const selectValue =
    selection.mode === "all"
      ? "__all__"
      : selection.mode === "branch"
        ? selection.branchId
        : currentBranchId
          ? `__current__:${currentBranchId}`
          : "__current__";

  return (
    <div
      className={cn(
        "flex min-w-0 flex-col gap-1.5",
        compact && "dashboard-scope-compact",
      )}
      data-testid="report-branch-filter"
    >
      <div className="flex min-w-0 flex-col gap-1.5" data-testid="report-scope-controls">
        {!compact ? (
          <label
            htmlFor={selectId}
            className="text-[length:var(--exits-text-sm)] font-semibold"
          >
            {t("reports.scope.label")}
          </label>
        ) : (
          <label htmlFor={selectId} className="sr-only">
            {t("reports.scope.label")}
          </label>
        )}
        <select
          id={selectId}
          className={cn(
            "catalog-form-select max-w-full",
            compact ? "dashboard-toolbar__select" : "",
          )}
          data-testid="report-scope-select"
          disabled={loading || branchesQuery.isLoading}
          value={selectValue}
          onChange={(event) => {
            const value = event.target.value;
            if (value === "__all__") {
              onSelectionChange({ mode: "all" });
              return;
            }
            if (value.startsWith("__current__")) {
              onSelectionChange({ mode: "current" });
              return;
            }
            onSelectionChange({ mode: "branch", branchId: value });
          }}
        >
          <option value={currentBranchId ? `__current__:${currentBranchId}` : "__current__"}>
            {t("reports.scope.currentBranch")}
            {currentBranchName ? ` — ${currentBranchName}` : ""}
          </option>
          {allowAllBranches ? (
            <option value="__all__">{t("reports.scope.allBranches")}</option>
          ) : null}
          {!singleBranch
            ? branches
                .filter((b) => b.id !== currentBranchId)
                .map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))
            : null}
        </select>
        {!compact ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="report-scope-value"
          >
            {displayName}
          </p>
        ) : (
          <span className="sr-only" data-testid="report-scope-value">
            {displayName}
          </span>
        )}
      </div>
    </div>
  );
}
