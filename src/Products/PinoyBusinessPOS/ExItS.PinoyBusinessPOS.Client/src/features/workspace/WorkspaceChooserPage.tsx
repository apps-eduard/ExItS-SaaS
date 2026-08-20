import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { resolveRoleHomeRoute } from "@/access/pos-capabilities";
import { getPosSessionGrant } from "@/api/platform/pos-session-grant";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { workspaceBindFailureTitleKey } from "@/workspace/workspace-bind-error";
import type { MessageKey } from "@/i18n/messages";

export function WorkspaceChooserPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { status, workspaces, accessDeniedDetail, bindFailureKind, bindWorkspace } = useWorkspace();
  const [expandedOrgId, setExpandedOrgId] = useState<string | null>(null);
  const [bindingId, setBindingId] = useState<string | null>(null);
  const [localErrorKey, setLocalErrorKey] = useState<MessageKey | null>(null);

  if (status === "loading" || status === "binding") {
    return <LoadingState label={t("workspace.loading")} />;
  }

  if (status === "error") {
    return <ErrorState title={t("error.title")} detail={t("workspace.loadError")} />;
  }

  if (workspaces.length === 0) {
    return (
      <div className="flex min-w-0 flex-col gap-4">
        <PageHeader title={t("workspace.title")} description={t("workspace.lede")} />
        <EmptyState title={t("noLocation.title")} detail={t("noLocation.detail")} />
      </div>
    );
  }

  async function selectBranch(organizationId: string, branchId: string) {
    setBindingId(`${organizationId}:${branchId}`);
    setLocalErrorKey(null);
    const ok = await bindWorkspace(organizationId, branchId);
    setBindingId(null);
    if (!ok) {
      setLocalErrorKey("accessDenied.generic");
      return;
    }
    setLocalErrorKey(null);
    navigate(resolveRoleHomeRoute(getPosSessionGrant()), { replace: true });
  }

  const failureTitleKey = bindFailureKind
    ? workspaceBindFailureTitleKey(bindFailureKind)
    : "accessDenied.title";
  const failureDetailKey = (accessDeniedDetail as MessageKey | null) ?? localErrorKey;

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("workspace.title")} description={t("workspace.lede")} />
      {failureDetailKey ? (
        <ErrorState title={t(failureTitleKey)} detail={t(failureDetailKey)} />
      ) : null}
      <div className="flex flex-col gap-3" role="list">
        {workspaces.map((organization) => {
          const expanded = expandedOrgId === organization.organizationId;
          return (
            <Card key={organization.organizationId} className="p-0">
              <div role="listitem">
                <button
                  type="button"
                  className="flex w-full items-center justify-between gap-3 border-0 bg-transparent px-4 py-3 text-left"
                  onClick={() => setExpandedOrgId(expanded ? null : organization.organizationId)}
                >
                  <span className="min-w-0">
                    <span className="block truncate text-[length:var(--exits-text-md)] font-semibold">
                      {organization.displayName}
                    </span>
                    <span className="block text-[length:var(--exits-text-sm)] text-muted">
                      {organization.branches.length} {t("workspace.branchesLabel")}
                    </span>
                  </span>
                  <span aria-hidden="true">{expanded ? "⌄" : "›"}</span>
                </button>
                {expanded ? (
                  <ul className="m-0 list-none border-t border-border px-2 py-2" role="list">
                    {organization.branches.map((branch) => {
                      const key = `${organization.organizationId}:${branch.branchId}`;
                      return (
                        <li key={branch.branchId} role="listitem">
                          <Button
                            variant="ghost"
                            className="h-auto w-full justify-start px-2 py-3 text-left"
                            disabled={bindingId === key}
                            onClick={() =>
                              void selectBranch(organization.organizationId, branch.branchId)
                            }
                          >
                            <span className="min-w-0">
                              <span className="block truncate font-semibold">{branch.name}</span>
                              <span className="block truncate text-[length:var(--exits-text-sm)] text-muted">
                                {branch.secondaryLine}
                              </span>
                            </span>
                          </Button>
                        </li>
                      );
                    })}
                  </ul>
                ) : null}
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
