import { Link } from "react-router-dom";
import { Plus } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { ProductsList } from "@/features/global-catalog/ProductsList";
import { useGlobalCatalogPageGate } from "@/features/global-catalog/use-global-catalog-page-gate";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";

export function GlobalProductsPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const gate = useGlobalCatalogPageGate();
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageGlobalProducts);

  if (gate.gate) {
    return gate.gate;
  }

  return (
    <section className="grid gap-4">
      <PageHeader
        title={t("nav.globalProducts")}
        description={t("globalCatalog.products.description")}
        actions={
          canManage ? (
            <Button asChild size="sm">
              <Link to="/admin/global-catalog/products/new">
                <Plus aria-hidden="true" className="mr-1.5 size-4" />
                {t("globalCatalog.products.create")}
              </Link>
            </Button>
          ) : null
        }
      />
      <ProductsList enabled={gate.canView} />
    </section>
  );
}
