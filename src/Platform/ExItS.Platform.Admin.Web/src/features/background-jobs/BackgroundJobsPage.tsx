import { useMemo, useState, type FormEvent } from "react";

import { Link, useSearchParams } from "react-router-dom";

import { useQuery } from "@tanstack/react-query";

import {

  BACKGROUND_JOBS_PAGE_SIZE,

  BACKGROUND_JOB_STATUSES,

  listBackgroundJobs,

  type PlatformBackgroundJob,

} from "@/api/ops/background-jobs-client";

import { isPlatformForbidden } from "@/api/platform-http-status";

import { AdminTable } from "@/components/exits/AdminTable";

import { ErrorState } from "@/components/exits/ErrorState";

import { ForbiddenState } from "@/components/exits/ForbiddenState";

import { PageHeader } from "@/components/exits/PageHeader";

import { StatusIndicator } from "@/components/exits/StatusIndicator";

import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";

import { Button } from "@/components/ui/button";

import { Input } from "@/components/ui/input";

import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";

import { useAuthorization } from "@/hooks/use-authorization";

import { usePreferences } from "@/hooks/use-preferences";

import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

import { env } from "@/lib/env";

import type { MessageKey } from "@/lib/i18n/messages";



const controlClass =

  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";



type JobsUrlState = {

  status: string;

  search: string;

  page: number;

};



function parseJobsSearchParams(params: URLSearchParams): JobsUrlState {

  const pageRaw = Number(params.get("page") ?? "1");

  return {

    status: params.get("status")?.trim() ?? "",

    search: params.get("search")?.trim() ?? "",

    page: Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1,

  };

}



function jobsSearchParams(state: JobsUrlState): URLSearchParams {

  const params = new URLSearchParams();

  if (state.status) {

    params.set("status", state.status);

  }

  if (state.search) {

    params.set("search", state.search);

  }

  if (state.page > 1) {

    params.set("page", String(state.page));

  }

  return params;

}



const STATUS_TONES: Record<string, "success" | "warning" | "danger" | "neutral"> = {

  Completed: "success",

  CompletedWithWarnings: "warning",

  Failed: "danger",

  Processing: "neutral",

  Queued: "neutral",

  Validated: "neutral",

};



function progressLabel(job: PlatformBackgroundJob): string {

  if (job.processedCount == null && job.totalCount == null) {

    return "—";

  }

  if (job.totalCount != null) {

    return `${job.processedCount ?? 0} / ${job.totalCount}`;

  }

  return String(job.processedCount ?? 0);

}



function jobDetailHref(job: PlatformBackgroundJob): string | null {

  if (job.source === "catalog-import") {

    return `/admin/global-catalog/imports/${job.id}`;

  }

  return null;

}



export function BackgroundJobsPage() {

  const { t } = usePreferences();

  const authorization = useAuthorization();

  const [searchParams, setSearchParams] = useSearchParams();

  const state = useMemo(() => parseJobsSearchParams(searchParams), [searchParams]);

  const [statusDraft, setStatusDraft] = useState(state.status);

  const [searchDraft, setSearchDraft] = useState(state.search);

  const canView =

    authorization.status === "loaded" && authorization.isPlatformAdministrator;



  const jobsQuery = useQuery({

    queryKey: ["background-jobs", state.status, state.search, state.page],

    enabled: canView,

    queryFn: ({ signal }) =>

      listBackgroundJobs(env.platformApiBaseUrl, {

        status: state.status || undefined,

        search: state.search || undefined,

        page: state.page,

        pageSize: BACKGROUND_JOBS_PAGE_SIZE,

        signal,

      }),

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



  function replaceState(patch: Partial<JobsUrlState>) {

    const current = parseJobsSearchParams(new URLSearchParams(window.location.search));

    setSearchParams(jobsSearchParams({ ...current, ...patch }), { replace: true });

  }



  function onFilterSubmit(event: FormEvent) {

    event.preventDefault();

    replaceState({ status: statusDraft.trim(), search: searchDraft.trim(), page: 1 });

  }



  const diagnostic = jobsQuery.error

    ? normalizeDiagnosticError({ error: jobsQuery.error, operation: "Load background jobs" })

    : null;

  const forbidden = jobsQuery.error ? isPlatformForbidden(jobsQuery.error) : false;

  const totalPages = jobsQuery.data

    ? Math.max(1, Math.ceil(jobsQuery.data.totalCount / BACKGROUND_JOBS_PAGE_SIZE))

    : 1;



  return (

    <section className="grid min-w-0 gap-4">

      <PageHeader title={t("backgroundJobs.title")} description={t("backgroundJobs.description")} />



      <p className="text-[length:var(--exits-text-sm)] text-muted">{t("backgroundJobs.message")}</p>



      <form

        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-[minmax(10rem,14rem)_minmax(0,1fr)_auto_auto]"

        onSubmit={onFilterSubmit}

      >

        <label

          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"

          htmlFor="background-jobs-status"

        >

          {t("backgroundJobs.filters.status")}

          <select

            id="background-jobs-status"

            className={controlClass}

            value={statusDraft}

            onChange={(event) => setStatusDraft(event.target.value)}

          >

            <option value="">{t("backgroundJobs.filters.status.all")}</option>

            {BACKGROUND_JOB_STATUSES.map((status) => (

              <option key={status} value={status}>

                {t(`backgroundJobs.status.${status}` as MessageKey)}

              </option>

            ))}

          </select>

        </label>

        <label

          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"

          htmlFor="background-jobs-search"

        >

          {t("backgroundJobs.filters.search")}

          <Input

            id="background-jobs-search"

            value={searchDraft}

            onChange={(event) => setSearchDraft(event.target.value)}

            placeholder={t("backgroundJobs.filters.searchPlaceholder")}

            autoComplete="off"

          />

        </label>

        <div className="flex items-end gap-2">

          <Button type="submit">{t("backgroundJobs.filters.apply")}</Button>

          <Button type="button" variant="outline" onClick={() => void jobsQuery.refetch()}>

            {t("backgroundJobs.filters.refresh")}

          </Button>

        </div>

      </form>



      {jobsQuery.isPending ? (

        <div role="status" aria-busy="true" aria-label={t("backgroundJobs.loading")}>

          <DashboardWidgetSkeleton rows={6} />

        </div>

      ) : null}



      {forbidden ? <ForbiddenState /> : null}



      {jobsQuery.isError && diagnostic && !forbidden ? (

        <ErrorState diagnostic={diagnostic} headingLevel="h2" onRetry={() => void jobsQuery.refetch()} />

      ) : null}



      {jobsQuery.isSuccess ? (

        <div className="min-w-0 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">

          <AdminTable

            caption={t("backgroundJobs.table.caption")}

            empty={t("backgroundJobs.table.empty")}

            rows={jobsQuery.data.items.map((job) => ({ ...job, id: job.id }))}

            columns={[

              {

                id: "name",

                header: t("backgroundJobs.column.name"),

                cell: (row) => row.displayName || row.id,

              },

              {

                id: "source",

                header: t("backgroundJobs.column.source"),

                cell: () => t("backgroundJobs.source.catalogImport"),

              },

              {

                id: "status",

                header: t("backgroundJobs.column.status"),

                cell: (row) => (

                  <StatusIndicator

                    tone={STATUS_TONES[row.status] ?? "neutral"}

                    label={t(`backgroundJobs.status.${row.status}` as MessageKey)}

                  />

                ),

              },

              {

                id: "progress",

                header: t("backgroundJobs.column.progress"),

                cell: (row) => progressLabel(row),

              },

              {

                id: "stage",

                header: t("backgroundJobs.column.stage"),

                cell: (row) => row.currentStage ?? t("backgroundJobs.value.unavailable"),

              },

              {

                id: "requested",

                header: t("backgroundJobs.column.requestedAt"),

                cell: (row) => row.requestedAtUtc ?? t("backgroundJobs.value.unavailable"),

              },

              {

                id: "actions",

                header: t("backgroundJobs.column.actions"),

                cell: (row) => {

                  const href = jobDetailHref(row);

                  return href ? (

                    <Link className="text-primary hover:underline" to={href}>

                      {t("backgroundJobs.link.detail")}

                    </Link>

                  ) : (

                    t("backgroundJobs.value.unavailable")

                  );

                },

              },

            ]}

          />

          {totalPages > 1 ? (

            <div className="mt-3 flex flex-wrap items-center justify-between gap-2">

              <p className="text-[length:var(--exits-text-sm)] text-muted">

                {t("backgroundJobs.pagination.page")

                  .replace("{page}", String(state.page))

                  .replace("{totalPages}", String(totalPages))}

              </p>

              <div className="flex gap-2">

                <Button

                  type="button"

                  variant="outline"

                  disabled={state.page <= 1}

                  onClick={() => replaceState({ page: state.page - 1 })}

                >

                  {t("backgroundJobs.pagination.previous")}

                </Button>

                <Button

                  type="button"

                  variant="outline"

                  disabled={state.page >= totalPages}

                  onClick={() => replaceState({ page: state.page + 1 })}

                >

                  {t("backgroundJobs.pagination.next")}

                </Button>

              </div>

            </div>

          ) : null}

        </div>

      ) : null}

    </section>

  );

}


