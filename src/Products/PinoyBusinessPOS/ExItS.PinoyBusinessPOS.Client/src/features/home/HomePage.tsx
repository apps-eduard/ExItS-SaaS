import { Navigate, Link } from "react-router-dom";
import { resolveRoleHomeRoute } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function HomePage() {
  const { t } = useI18n();
  const { status, boundWorkspace, sessionGrant } = useWorkspace();

  if (!boundWorkspace) {
    if (status === "loading" || status === "binding" || status === "idle" || status === "ready") {
      return <LoadingState label={t("session.loading")} />;
    }
    return <EmptyState title={t("home.emptyTitle")} detail={t("workspace.lede")} />;
  }

  const roleHome = resolveRoleHomeRoute(sessionGrant);

  return <BoundHomeRedirect roleHome={roleHome} />;
}

function BoundHomeRedirect({ roleHome }: { roleHome: string }) {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();

  if (roleHome !== "/" && boundWorkspace) {
    return <Navigate to={roleHome} replace />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <PageHeader
        title={t("home.title")}
        description={`${boundWorkspace!.organizationDisplayName} · ${boundWorkspace!.branchName}`}
      />
      <StatusChip tone="success">{t("home.badge")}</StatusChip>
      <Card>
        <p className="m-0 text-[length:var(--exits-text-md)]">{t("home.body")}</p>
        <p className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-muted">{t("home.scope")}</p>
      </Card>
      <EmptyState title={t("home.emptyTitle")} detail={t("home.emptyDetail")} />
      <div className="flex flex-wrap gap-2">
        <Button asChild variant="ghost">
          <Link to="/workspace">{t("workspace.switch")}</Link>
        </Button>
        <Button asChild variant="ghost">
          <Link to="/settings/preferences">{t("preferences.title")}</Link>
        </Button>
      </div>
    </div>
  );
}
