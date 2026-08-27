import { useEffect, useMemo } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getCatalogImportJob } from "@/api/pos/pos-catalog-import-client";
import {
  isImportJobActive,
  isImportJobTerminal,
} from "@/api/pos/pos-catalog-import-types";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { OnlineRequiredCard } from "@/components/exits/OnlineRequiredCard";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { ONLINE_REQUIRED_CODES } from "@/offline/online-required";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

export function CatalogImportJobPage() {
  const { t } = useI18n();
  const { jobId } = useParams<{ jobId: string }>();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const workspace = usePosWorkspaceScope();

  const jobQuery = useQuery({
    queryKey: ["catalog-import", "job", workspace?.organizationId, jobId],
    enabled: online && Boolean(workspace) && Boolean(jobId),
    queryFn: ({ signal }) => getCatalogImportJob(workspace!, jobId!, signal),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      if (!status || isImportJobTerminal(status)) return false;
      if (isImportJobActive(status)) return 2000;
      return 5000;
    },
  });

  useEffect(() => {
    if (!jobId) {
      navigate("/catalog/templates", { replace: true });
    }
  }, [jobId, navigate]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!online) {
    return (
      <div className="flex flex-col gap-4" data-testid="catalog-import-job-page">
        <PageHeader title={t("catalogImport.progressTitle")} />
        <OnlineRequiredCard code={ONLINE_REQUIRED_CODES.CatalogImport} />
        <Button asChild variant="ghost" className="min-h-11 self-start">
          <Link to="/catalog">{t("catalogImport.backToProducts")}</Link>
        </Button>
      </div>
    );
  }

  const job = jobQuery.data;
  const terminal = job ? isImportJobTerminal(job.status) : false;
  const active = job ? isImportJobActive(job.status) : false;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="catalog-import-job-page">
      <PageHeader
        title={t("catalogImport.progressTitle")}
        description={t("catalogImport.progressLede")}
      />

      {jobQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {jobQuery.isError ? (
        <ErrorState title={t("error.title")} detail={(jobQuery.error as Error).message} />
      ) : null}

      {job ? (
        <Card className="flex flex-col gap-3 p-4" data-testid="catalog-import-job-card">
          <div className="flex flex-wrap items-center gap-2">
            <StatusChip>{job.status}</StatusChip>
            {job.currentStage ? (
              <span className="text-[length:var(--exits-text-sm)] text-muted">
                {job.currentStage}
              </span>
            ) : null}
          </div>
          <dl className="m-0 grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("catalogImport.processed")}
              </dt>
              <dd className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
                {job.processedCount}/{job.totalCount}
              </dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("catalogImport.imported")}
              </dt>
              <dd className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
                {job.importedCount}
              </dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("catalogImport.skipped")}
              </dt>
              <dd className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
                {job.skippedCount}
              </dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("catalogImport.failed")}
              </dt>
              <dd className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
                {job.failedCount}
              </dd>
            </div>
          </dl>
          {job.errorSummary ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-danger">{job.errorSummary}</p>
          ) : null}
          {active ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("catalogImport.progressBackground")}
            </p>
          ) : null}
          {terminal && job.status.toLowerCase() === "completed" ? (
            <p className="m-0 text-[length:var(--exits-text-sm)]" role="status">
              {t("catalogImport.progressDone")}
            </p>
          ) : null}
        </Card>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          variant="ghost"
          className="min-h-11"
          data-testid="catalog-import-job-refresh"
          onClick={() => void jobQuery.refetch()}
          disabled={jobQuery.isFetching}
        >
          {t("catalogImport.refresh")}
        </Button>
        <Button asChild className="min-h-11">
          <Link to="/catalog">{t("catalogImport.goToProducts")}</Link>
        </Button>
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/catalog">{t("catalogImport.reviewLocal")}</Link>
        </Button>
      </div>
    </div>
  );
}
