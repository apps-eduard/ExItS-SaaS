import { Link, useLocation } from "react-router-dom";
import { productsListHref } from "@/api/catalog/product-id";
import { PageHeader } from "@/components/exits/PageHeader";
import { usePreferences } from "@/hooks/use-preferences";

export const PRODUCTS_LIST_STATE_KEY = "productsListSearch";

export type ProductsLocationState = {
  [PRODUCTS_LIST_STATE_KEY]?: string;
};

export function ProductNotFoundPage() {
  const { t } = usePreferences();
  const location = useLocation();
  const state = (location.state as ProductsLocationState | null) ?? null;
  const backHref = productsListHref(state?.[PRODUCTS_LIST_STATE_KEY]);

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("products.detail.notFound.title")}
        description={t("products.detail.notFound.body")}
      />
      <p>
        <Link className="text-primary hover:underline" to={backHref}>
          {t("products.detail.notFound.back")}
        </Link>
      </p>
    </section>
  );
}
