import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageSuppliers } from "@/access/pos-capabilities";
import {
  approveConnection,
  declineConnection,
  listRelationships,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ConnectedIncomingRequestsPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [sharePrompt, setSharePrompt] = useState<{
    relationshipId: string;
    name: string;
  } | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManageSuppliers(sessionGrant);

  const query = useQuery({
    queryKey: ["connected-suppliers", "incoming", workspace?.organizationId],
    enabled: Boolean(workspace),
    queryFn: async ({ signal }) => {
      const rows = await listRelationships(workspace!, "supplier", signal);
      return rows.filter((row) => row.status.toLowerCase() === "pending");
    },
  });

  async function respond(relationshipId: string, accept: boolean, name: string) {
    if (!workspace || !allowManage || busyId) {
      return;
    }
    setBusyId(relationshipId);
    setActionError(null);
    try {
      if (accept) {
        await approveConnection(workspace, relationshipId);
        setSharePrompt({ relationshipId, name });
      } else {
        await declineConnection(workspace, relationshipId);
      }
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
    } catch (err) {
      setActionError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("connected.respondFailed"),
      );
    } finally {
      setBusyId(null);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="connected-incoming-page">
      <PageHeader title={t("connected.incomingTitle")} description={t("connected.incomingHelp")} />
      {actionError ? (
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {actionError}
          </p>
        </Card>
      ) : null}
      {sharePrompt ? (
        <Card data-testid="connected-share-prompt">
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {t("connected.sharePromptTitle")}
          </h2>
          <p className="mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("connected.sharePromptHelp").replace("{name}", sharePrompt.name)}
          </p>
          <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("connected.exposableNotSharedNote")}
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button
              type="button"
              className="min-h-11"
              data-testid="connected-share-now"
              onClick={() =>
                navigate(
                  `/suppliers/connected/buyers/${sharePrompt.relationshipId}/shared-products`,
                )
              }
            >
              {t("connected.shareProductsNow")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              data-testid="connected-share-later"
              onClick={() => setSharePrompt(null)}
            >
              {t("connected.notNow")}
            </Button>
          </div>
        </Card>
      ) : null}
      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            query.error instanceof PosApiError
              ? (query.error.problem.detail ?? query.error.message)
              : t("connected.loadFailed")
          }
        />
      ) : null}
      {query.isSuccess && query.data.length === 0 ? (
        <EmptyState title={t("connected.noIncoming")} detail={t("connected.noIncomingHelp")} />
      ) : null}
      <ul className="m-0 grid list-none gap-2 p-0" data-testid="connected-incoming-list">
        {query.data?.map((item) => {
          const name = item.counterpartyDisplayName?.trim() || t("connected.requestingBusiness");
          return (
            <li key={item.relationshipId}>
              <Card>
                <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">{name}</h2>
                {item.counterpartyPublicOrganizationId ? (
                  <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {item.counterpartyPublicOrganizationId}
                  </p>
                ) : null}
                <p className="mt-2 text-[length:var(--exits-text-sm)]">
                  {t("connected.incomingMessage").replace("{name}", name)}
                </p>
                {allowManage ? (
                  <div className="mt-3 flex flex-wrap gap-2">
                    <Button
                      type="button"
                      className="min-h-11"
                      data-testid={`connected-approve-${item.relationshipId}`}
                      disabled={busyId === item.relationshipId}
                      onClick={() => void respond(item.relationshipId, true, name)}
                    >
                      {t("connected.accept")}
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      className="min-h-11"
                      data-testid={`connected-decline-${item.relationshipId}`}
                      disabled={busyId === item.relationshipId}
                      onClick={() => void respond(item.relationshipId, false, name)}
                    >
                      {t("connected.decline")}
                    </Button>
                  </div>
                ) : null}
              </Card>
            </li>
          );
        })}
      </ul>
      <Button asChild variant="ghost" className="min-h-11 self-start">
        <Link to="/suppliers">{t("connected.backToSuppliers")}</Link>
      </Button>
    </div>
  );
}
