import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PageHeader } from "@/components/exits/PageHeader";
import { Skeleton } from "@/components/ui/skeleton";
import { ImportUploadPanel } from "@/features/global-catalog/ImportUploadPanel";
import { ImportsList } from "@/features/global-catalog/ImportsList";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";

export function ImportsPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canImport =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.importGlobalProducts);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canImport) {
    return <ShellNotFoundPage />;
  }

  return (
    <section className="grid gap-4">
      <PageHeader
        title={t("nav.imports")}
        description={t("globalCatalog.imports.description")}
      />
      <ImportUploadPanel enabled={canImport} />
      <ImportsList enabled={canImport} />
    </section>
  );
}
