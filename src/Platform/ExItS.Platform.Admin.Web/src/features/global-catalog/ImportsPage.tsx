import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PageHeader } from "@/components/exits/PageHeader";
import { ImportUploadPanel } from "@/features/global-catalog/ImportUploadPanel";
import { ImportsList } from "@/features/global-catalog/ImportsList";
import { useGlobalCatalogPageGate } from "@/features/global-catalog/use-global-catalog-page-gate";
import { usePreferences } from "@/hooks/use-preferences";

export function ImportsPage() {
  const { t } = usePreferences();
  const gate = useGlobalCatalogPageGate(PLATFORM_PERMISSIONS.importGlobalProducts);

  if (gate.gate) {
    return gate.gate;
  }

  return (
    <section className="grid gap-4">
      <PageHeader
        title={t("nav.imports")}
        description={t("globalCatalog.imports.description")}
      />
      <ImportUploadPanel enabled={gate.canView} />
      <ImportsList enabled={gate.canView} />
    </section>
  );
}
