import { useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import {
  parseOrganizationListSearchParams,
  sanitizeOrganizationListProduct,
} from "@/api/organizations/organization-list-query";
import { PageHeader } from "@/components/exits/PageHeader";
import { Skeleton } from "@/components/ui/skeleton";
import { OrganizationsList } from "@/features/organizations/OrganizationsList";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";

export function OrganizationsPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const [searchParams] = useSearchParams();
  const urlState = useMemo(() => parseOrganizationListSearchParams(searchParams), [searchParams]);
  const catalogQuery = useAuthorizedCatalogProductsQuery();
  const catalog = useMemo(
    () =>
      (catalogQuery.data?.items ?? []).map((item) => ({
        code: item.code,
        displayName: item.displayName,
      })),
    [catalogQuery.data],
  );
  const productRequested = Boolean(urlState.product);
  const awaitingCatalog = productRequested && catalogQuery.isPending;
  const catalogUnavailable = productRequested && catalogQuery.isError;
  const selectedProduct = catalogQuery.isSuccess
    ? sanitizeOrganizationListProduct(urlState.product, catalog)
    : null;
  const invalidProduct = productRequested && catalogQuery.isSuccess && selectedProduct == null;
  const canList =
    authorization.status === "loaded" &&
    authorization.hasAnyPermission([
      PLATFORM_PERMISSIONS.viewPortfolio,
      PLATFORM_PERMISSIONS.manageOrganizations,
    ]);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canList) {
    return <ShellNotFoundPage />;
  }

  const title = selectedProduct
    ? `${t("nav.organizations")} / ${selectedProduct.displayName || selectedProduct.code}`
    : t("nav.organizations");

  return (
    <section className="grid gap-4">
      <PageHeader
        title={title}
        description={
          selectedProduct ? t("organizations.product.description") : t("organizations.description")
        }
      />
      {invalidProduct ? (
        <p
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted"
          role="status"
        >
          {t("organizations.product.invalid")}
        </p>
      ) : null}
      {catalogUnavailable ? (
        <p
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted"
          role="status"
        >
          {t("organizations.product.catalogUnavailable")}
        </p>
      ) : null}
      {awaitingCatalog ? (
        <div aria-busy="true" aria-label={t("organizations.loading")}>
          <Skeleton className="h-24 w-full" />
        </div>
      ) : (
        <OrganizationsList
          enabled={canList && !invalidProduct && !catalogUnavailable}
          catalog={catalog}
          catalogLoading={catalogQuery.isPending}
          selectedProduct={selectedProduct}
        />
      )}
    </section>
  );
}
