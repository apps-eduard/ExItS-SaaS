import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { listQuotations } from "@/api/pos/pos-quotations-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { formatPeso } from "@/lib/format-money";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { canCreateSale } from "@/access/pos-capabilities";

export function QuotationsListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canCreate = canCreateSale(sessionGrant, boundWorkspace?.branchType);

  const workspace = boundWorkspace?.organizationId
    ? {
        organizationId: boundWorkspace.organizationId,
        branchId: boundWorkspace.branchId ?? undefined,
      }
    : null;

  const query = useQuery({
    queryKey: ["quotations", workspace?.organizationId],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) => listQuotations(workspace!, { page: 1, pageSize: 50 }, signal),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!online) {
    return (
      <div className="exits-page p-4" data-testid="quotations-list-page">
        <PageHeader title={t("quotations.title")} />
        <ErrorState title={t("offline.internetRequiredTitle")} detail={t("quotations.offline")} />
      </div>
    );
  }

  if (query.isLoading) {
    return <LoadingState label={t("quotations.loading")} />;
  }

  if (query.isError) {
    return (
      <div className="exits-page p-4" data-testid="quotations-list-page">
        <PageHeader title={t("quotations.title")} />
        <ErrorState title={t("quotations.loadFailed")} detail={String(query.error)} />
      </div>
    );
  }

  const items = query.data?.items ?? [];

  return (
    <div className="exits-page flex flex-col gap-3 p-4" data-testid="quotations-list-page">
      <PageHeader
        title={t("quotations.title")}
        description={t("quotations.lede")}
        trailing={
          canCreate ? (
            <Button asChild data-testid="quotations-new">
              <Link to="/quotations/new">{t("quotations.new")}</Link>
            </Button>
          ) : null
        }
      />

      {items.length === 0 ? (
        <p className="m-0 text-sm text-muted" data-testid="quotations-empty">
          {t("quotations.empty")}
        </p>
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="quotations-list">
          {items.map((q) => (
            <li key={q.quotationId}>
              <Link
                to={`/quotations/${q.quotationId}`}
                className="flex flex-col gap-0.5 rounded-md border border-border px-3 py-2 no-underline"
                data-testid={`quotation-row-${q.quotationId}`}
              >
                <span className="font-medium text-foreground">
                  {q.quotationNumber ?? t("quotations.draftLabel")}
                </span>
                <span className="text-sm text-muted">
                  {q.customerDisplayName} · {q.status} · {formatPeso(q.subtotal)}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
