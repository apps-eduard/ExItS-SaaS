import { Navigate, Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { isOrganizationContextLocked } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { workingExperienceRoute } from "@/workspace/working-experience";

export function HomePage() {
  const { t } = useI18n();
  const { session } = useSession();
  const { status, boundWorkspace } = useWorkspace();

  if (!boundWorkspace) {
    if (status === "loading" || status === "binding" || status === "idle" || status === "ready") {
      return <LoadingState label={t("session.loading")} />;
    }
    return <EmptyState title={t("home.emptyTitle")} detail={t("workspace.lede")} />;
  }

  return (
    <BoundHomeRedirect
      experienceRoute={workingExperienceRoute(boundWorkspace.experience)}
      canSwitchWorkspace={!isOrganizationContextLocked(session)}
    />
  );
}

function BoundHomeRedirect({
  experienceRoute,
  canSwitchWorkspace,
}: {
  experienceRoute: string;
  canSwitchWorkspace: boolean;
}) {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();

  if (experienceRoute !== "/" && boundWorkspace) {
    return <Navigate to={experienceRoute} replace />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <PageHeader
        title={t("home.title")}
        description={
          boundWorkspace!.branchName
            ? `${boundWorkspace!.organizationDisplayName} · ${boundWorkspace!.branchName}`
            : boundWorkspace!.organizationDisplayName
        }
      />
      <StatusChip tone="success">{t("home.badge")}</StatusChip>
      <Card>
        <p className="m-0 text-[length:var(--exits-text-md)]">{t("home.body")}</p>
        <p className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-muted">{t("home.scope")}</p>
      </Card>
      <EmptyState title={t("home.emptyTitle")} detail={t("home.emptyDetail")} />
      <div className="flex flex-wrap gap-2">
        {canSwitchWorkspace ? (
          <Button asChild variant="ghost">
            <Link to="/workspace">{t("workspace.switch")}</Link>
          </Button>
        ) : null}
        <Button asChild variant="ghost">
          <Link to="/settings/preferences">{t("preferences.title")}</Link>
        </Button>
      </div>
    </div>
  );
}
