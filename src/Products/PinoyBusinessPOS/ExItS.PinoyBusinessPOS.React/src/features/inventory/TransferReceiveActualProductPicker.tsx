import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { listCatalogProducts } from "@/api/pos/pos-catalog-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";

type TransferReceiveActualProductPickerProps = {
  workspace: PosWorkspaceScope;
  excludeProductId: string;
  onSelect: (product: { productId: string; name: string }) => void;
  testId?: string;
};

export function TransferReceiveActualProductPicker({
  workspace,
  excludeProductId,
  onSelect,
  testId = "transfer-receive-actual-product-picker",
}: TransferReceiveActualProductPickerProps) {
  const { t } = useI18n();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(timer);
  }, [search]);

  const query = useQuery({
    queryKey: [
      "transfer-receive-actual-product",
      workspace.organizationId,
      workspace.branchId,
      debounced,
    ],
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace,
        {
          page: 1,
          pageSize: 8,
          search: debounced || undefined,
        },
        signal,
      ),
  });

  const items = (query.data?.items ?? []).filter(
    (p) => p.productId.toLowerCase() !== excludeProductId.toLowerCase(),
  );

  return (
    <div className="flex flex-col gap-2" data-testid={testId}>
      <input
        className="exits-input"
        type="search"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder={t("transfer.searchProducts")}
        data-testid={`${testId}-search`}
      />
      <ul className="m-0 max-h-40 list-none overflow-y-auto rounded-md border border-border p-0">
        {items.map((product) => (
          <li key={product.productId} className="border-b border-border last:border-b-0">
            <Button
              type="button"
              appearance="ghost"
              className="h-auto w-full justify-start px-3 py-2 text-start font-normal"
              data-testid={`${testId}-option-${product.productId}`}
              onClick={() => {
                if (product.productId.toLowerCase() === excludeProductId.toLowerCase()) {
                  return;
                }
                onSelect({
                  productId: product.productId,
                  name: product.name?.trim() || product.productId.slice(0, 8),
                });
              }}
            >
              {product.name}
              {product.sku ? (
                <span className="ms-2 text-[length:var(--exits-text-xs)] text-muted">
                  {product.sku}
                </span>
              ) : null}
            </Button>
          </li>
        ))}
        {!query.isLoading && items.length === 0 ? (
          <li className="px-3 py-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("transfer.itemsEmpty")}
          </li>
        ) : null}
      </ul>
    </div>
  );
}
