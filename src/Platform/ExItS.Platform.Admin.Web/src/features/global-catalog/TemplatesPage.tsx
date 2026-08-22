import { Link } from "react-router-dom";
import { Plus } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { TemplatesList } from "@/features/global-catalog/TemplatesList";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";

export function TemplatesPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewGlobalCatalog);
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageCatalogTemplates);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canView) {
    return <ShellNotFoundPage />;
  }

  return (
    <section className="grid gap-4">
      <PageHeader
        title={t("nav.templates")}
        description={t("globalCatalog.templates.description")}
        actions={
          canManage ? (
            <Button asChild size="sm">
              <Link to="/admin/global-catalog/templates/new">
                <Plus aria-hidden="true" className="mr-1.5 size-4" />
                {t("globalCatalog.templates.create")}
              </Link>
            </Button>
          ) : null
        }
      />
      <TemplatesList enabled={canView} />
    </section>
  );
}
