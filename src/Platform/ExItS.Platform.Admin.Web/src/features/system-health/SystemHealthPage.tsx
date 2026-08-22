import { useQuery } from "@tanstack/react-query";
import { Activity, Cpu, HardDrive, MemoryStick, RefreshCw, Timer } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import {
  getSystemHealth,
  type SystemHealthSnapshot,
  type SystemHealthStatus,
} from "@/api/ops/system-health-client";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import {
  formatBytesPair,
  formatCpuPercent,
  formatDuration,
  formatLatency,
  formatRatioPercent,
} from "@/features/system-health/system-health-format";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";
import type { MessageKey } from "@/lib/i18n/messages";
import type { Language } from "@/lib/preferences/ui-preferences";

const SERVICE_LABELS: Record<string, MessageKey> = {
  "platform-api": "systemHealth.service.platformApi",
  "pos-api": "systemHealth.service.posApi",
  "platform-database": "systemHealth.service.platformDatabase",
  "pos-database": "systemHealth.service.posDatabase",
};

const STATUS_LABELS: Record<SystemHealthStatus, MessageKey> = {
  Healthy: "systemHealth.status.Healthy",
  Degraded: "systemHealth.status.Degraded",
  Unhealthy: "systemHealth.status.Unhealthy",
  Unavailable: "systemHealth.status.Unavailable",
  Unknown: "systemHealth.status.Unknown",
  NotAvailable: "systemHealth.status.NotAvailable",
};

function statusTone(status: SystemHealthStatus): "success" | "warning" | "danger" | "neutral" {
  if (status === "Healthy") {
    return "success";
  }
  if (status === "Degraded") {
    return "warning";
  }
  if (status === "Unhealthy") {
    return "danger";
  }
  return "neutral";
}

function formatCheckedAt(value: string, language: Language): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

const systemHealthQueryKey = ["operations", "system-health"] as const;

export function SystemHealthPage() {
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewPortfolio);

  const query = useQuery({
    queryKey: systemHealthQueryKey,
    enabled: canView,
    refetchInterval: 30_000,
    queryFn: ({ signal }) => getSystemHealth(env.platformApiBaseUrl, signal),
  });

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <DashboardWidgetSkeleton rows={4} />
      </section>
    );
  }

  if (!canView) {
    return <ShellNotFoundPage />;
  }

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load system health" })
    : null;

  return (
    <section className="grid min-w-0 gap-5">
      <PageHeader
        title={t("systemHealth.title")}
        description={t("systemHealth.description")}
        actions={
          <Button
            type="button"
            variant="outline"
            onClick={() => void query.refetch()}
            disabled={query.isFetching}
          >
            <RefreshCw aria-hidden="true" size={16} />
            {t("systemHealth.refresh")}
          </Button>
        }
      />

      {query.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("systemHealth.loading")}>
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState diagnostic={diagnostic} headingLevel="h2" onRetry={() => void query.refetch()} />
      ) : null}

      {query.data ? (
        <SystemHealthBody snapshot={query.data} t={t} language={language} />
      ) : null}
    </section>
  );
}

function SystemHealthBody({
  snapshot,
  t,
  language,
}: {
  snapshot: SystemHealthSnapshot;
  t: (key: MessageKey) => string;
  language: Language;
}) {
  const host = snapshot.host;
  return (
    <div className="grid min-w-0 gap-5">
      <article className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
        <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
          {t("systemHealth.overall.title")}
        </h2>
        <div className="mt-2">
          <StatusIndicator
            tone={statusTone(snapshot.overallStatus)}
            label={t(STATUS_LABELS[snapshot.overallStatus])}
          />
        </div>
      </article>

      <article className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
        <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
          {t("systemHealth.resources.title")}
        </h2>
        <ul className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <ResourceMetric
            icon={Cpu}
            label={t("systemHealth.resources.cpu")}
            value={formatCpuPercent(host.cpuPercent)}
          />
          <ResourceMetric
            icon={MemoryStick}
            label={t("systemHealth.resources.memory")}
            value={formatBytesPair(host.memoryUsedBytes, host.memoryTotalBytes)}
            detail={formatRatioPercent(host.memoryUsedBytes, host.memoryTotalBytes)}
          />
          <ResourceMetric
            icon={HardDrive}
            label={t("systemHealth.resources.storage")}
            value={formatBytesPair(host.storageUsedBytes, host.storageTotalBytes)}
            detail={formatRatioPercent(host.storageUsedBytes, host.storageTotalBytes)}
          />
          <ResourceMetric
            icon={Timer}
            label={t("systemHealth.resources.uptime")}
            value={formatDuration(host.uptimeSeconds)}
          />
        </ul>
      </article>

      <article className="min-w-0 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
        <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
          {t("systemHealth.services.title")}
        </h2>
        <div className="mt-3 min-w-0">
          <AdminTable
            caption={t("systemHealth.services.caption")}
            empty={t("systemHealth.services.empty")}
            rows={snapshot.services.map((service) => ({ ...service, id: service.name }))}
            columns={[
              {
                id: "service",
                header: t("systemHealth.services.column.service"),
                cell: (row) => t(SERVICE_LABELS[row.name] ?? "systemHealth.services.unknown"),
              },
              {
                id: "status",
                header: t("systemHealth.services.column.status"),
                cell: (row) => (
                  <StatusIndicator
                    tone={statusTone(row.status)}
                    label={t(STATUS_LABELS[row.status] ?? "systemHealth.status.Unknown")}
                  />
                ),
              },
              {
                id: "latency",
                header: t("systemHealth.services.column.latency"),
                align: "right",
                cell: (row) => formatLatency(row.latencyMs),
              },
              {
                id: "checked",
                header: t("systemHealth.services.column.checked"),
                cell: (row) => formatCheckedAt(row.checkedAtUtc, language),
              },
            ]}
          />
        </div>
      </article>

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
        <article className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
          <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
            {t("systemHealth.build.title")}
          </h2>
          <dl className="mt-3 grid grid-cols-[8rem_minmax(0,1fr)] gap-x-3 gap-y-2 text-[length:var(--exits-text-sm)]">
            <dt className="text-muted">{t("systemHealth.build.environment")}</dt>
            <dd className="min-w-0 break-words">{snapshot.build.environment}</dd>
            <dt className="text-muted">{t("systemHealth.build.version")}</dt>
            <dd className="min-w-0 break-words">
              {snapshot.build.applicationVersion ?? t("systemHealth.value.unavailable")}
            </dd>
            <dt className="text-muted">{t("systemHealth.build.commit")}</dt>
            <dd className="min-w-0 break-all font-mono">
              {snapshot.build.commitSha ?? t("systemHealth.value.unavailable")}
            </dd>
          </dl>
        </article>

        <article className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
          <h2 className="flex items-center gap-2 text-[length:var(--exits-text-sm)] font-semibold">
            <Activity aria-hidden="true" size={16} />
            {t("systemHealth.backup.title")}
          </h2>
          <dl className="mt-3 grid grid-cols-[8rem_minmax(0,1fr)] gap-x-3 gap-y-2 text-[length:var(--exits-text-sm)]">
            <dt className="text-muted">{t("systemHealth.backup.status")}</dt>
            <dd>
              <StatusIndicator
                tone={statusTone(snapshot.backup.status)}
                label={t(STATUS_LABELS[snapshot.backup.status] ?? "systemHealth.status.NotAvailable")}
              />
            </dd>
            <dt className="text-muted">{t("systemHealth.backup.lastSuccessful")}</dt>
            <dd className="min-w-0 break-words">
              {snapshot.backup.lastSuccessfulAtUtc
                ? formatCheckedAt(snapshot.backup.lastSuccessfulAtUtc, language)
                : t("systemHealth.value.unavailable")}
            </dd>
            <dt className="text-muted">{t("systemHealth.backup.age")}</dt>
            <dd>{formatDuration(snapshot.backup.ageSeconds)}</dd>
          </dl>
          {snapshot.backup.status === "NotAvailable" ? (
            <p className="mt-3 text-[length:var(--exits-text-sm)] text-muted">
              {t("systemHealth.backup.unavailableHint")}
            </p>
          ) : null}
        </article>
      </div>
    </div>
  );
}

function ResourceMetric({
  icon: Icon,
  label,
  value,
  detail,
}: {
  icon: typeof Cpu;
  label: string;
  value: string;
  detail?: string;
}) {
  return (
    <li className="min-w-0 rounded-[var(--exits-density-radius)] border border-border/80 bg-surface-muted/40 px-3 py-2">
      <div className="flex items-center gap-2 text-muted">
        <Icon aria-hidden="true" size={16} />
        <span className="text-[length:var(--exits-text-sm)]">{label}</span>
      </div>
      <p className="mt-1 break-words text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
        {value}
      </p>
      {detail ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted tabular-nums">{detail}</p>
      ) : null}
    </li>
  );
}
