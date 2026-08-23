import { Package } from "lucide-react";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import {
  formatQuantityDisplay,
  resolveAddFlow,
  resolveStockHint,
} from "@/cart/sell-cart-helpers";
import { useCatalogProductImageUrl } from "@/features/sell/use-catalog-product-image";
import { useI18n } from "@/i18n/I18nProvider";

type SellProductCardProps = {
  product: PosCatalogProductDto;
  workspace: PosWorkspaceScope | null;
  onAdd: (product: PosCatalogProductDto) => void;
};

export function SellProductCard({ product, workspace, onAdd }: SellProductCardProps) {
  const { t } = useI18n();
  const imageUrl = useCatalogProductImageUrl(
    workspace,
    product.productId,
    product.hasImage === true,
    product.imageVersion,
  );
  const hint = resolveStockHint({
    isTracked: product.isTracked,
    onHandQuantity: product.onHandQuantity,
    unitOfMeasure: product.unitOfMeasure,
    tracksExpiration: product.tracksExpiration,
    sellableQuantity: undefined,
  });
  const flow = resolveAddFlow(product);

  return (
    <button
      type="button"
      data-testid={`sell-product-${product.productId}`}
      className="sell-product-card"
      onClick={() => onAdd(product)}
    >
      <div className="sell-product-card__media">
        {imageUrl ? (
          <img
            src={imageUrl}
            alt=""
            className="sell-product-card__image"
            loading="lazy"
            decoding="async"
          />
        ) : (
          <span className="sell-product-card__placeholder" aria-hidden>
            <Package className="size-7 opacity-50" strokeWidth={1.5} />
          </span>
        )}
      </div>
      <div className="sell-product-card__body">
        <span className="sell-product-card__name">{product.name}</span>
        <MoneyDisplay
          amount={product.sellingPrice}
          className="sell-product-card__price"
          testId={`sell-product-price-${product.productId}`}
        />
        <div className="sell-product-card__meta">
          {hint ? (
            <span
              data-testid={`sell-product-stock-${product.productId}`}
              className="sell-product-card__stock"
            >
              {t("sell.stockOnHand")
                .replace("{qty}", formatQuantityDisplay(hint.quantity))
                .replace("{unit}", hint.unitOfMeasure)}
            </span>
          ) : null}
          {flow.kind === "weight" ||
          flow.kind === "customQuantity" ||
          flow.kind === "unitSelector" ? (
            <span className="sell-product-card__flow">
              {flow.kind === "weight"
                ? t("sell.tileByWeight")
                : flow.kind === "customQuantity"
                  ? t("sell.tileCustomQty")
                  : t("sell.tileChooseUnit")}
            </span>
          ) : (
            <span className="sell-product-card__unit">{product.unitOfMeasure}</span>
          )}
        </div>
      </div>
    </button>
  );
}
