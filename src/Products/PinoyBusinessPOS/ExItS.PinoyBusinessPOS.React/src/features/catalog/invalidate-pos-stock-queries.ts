import type { QueryClient } from "@tanstack/react-query";

/** Refresh catalog/sell stock after inventory-impacting mutations. */
export async function invalidatePosStockQueries(queryClient: QueryClient): Promise<void> {
  await queryClient.invalidateQueries({ queryKey: ["pos-catalog-browse"] });
  await queryClient.invalidateQueries({ queryKey: ["pos-sell-stock-hint"] });
  await queryClient.invalidateQueries({ queryKey: ["catalog"] });
  await queryClient.invalidateQueries({ queryKey: ["inventory"] });
  await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
  await queryClient.invalidateQueries({ queryKey: ["business-customers", "commerce-readiness"] });
  await queryClient.invalidateQueries({ queryKey: ["shell", "needs-attention"] });
}
