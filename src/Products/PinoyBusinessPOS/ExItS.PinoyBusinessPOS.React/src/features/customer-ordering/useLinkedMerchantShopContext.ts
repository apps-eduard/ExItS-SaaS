import { useQuery, useQueryClient, type InfiniteData } from "@tanstack/react-query";
import {
  getLinkedMerchantOrderingCapability,
  listLinkedMerchants,
  type LinkedMerchantDto,
  type LinkedMerchantPagedResult,
} from "@/api/platform/linked-merchants-client";

export type LinkedMerchantShopContext = {
  organizationDisplayName: string;
  customerDisplayName: string | null;
  businessCustomerId: string | null;
  statementTo: string | null;
};

function toContext(merchant: LinkedMerchantDto): LinkedMerchantShopContext {
  const statementTo =
    merchant.businessCustomerId && merchant.organizationId
      ? `/personal/linked-merchants/${merchant.organizationId}/${merchant.businessCustomerId}`
      : null;

  return {
    organizationDisplayName: merchant.organizationDisplayName,
    customerDisplayName: merchant.customerDisplayName || null,
    businessCustomerId: merchant.businessCustomerId || null,
    statementTo,
  };
}

export function useLinkedMerchantShopContext(organizationId: string, enabled: boolean) {
  const queryClient = useQueryClient();

  return useQuery({
    queryKey: ["personal", "linked-merchant-shop-context", organizationId],
    enabled: Boolean(organizationId) && enabled,
    staleTime: 60_000,
    queryFn: async ({ signal }): Promise<LinkedMerchantShopContext> => {
      const cached = queryClient.getQueryData<InfiniteData<LinkedMerchantPagedResult>>([
        "personal",
        "linked-merchants",
      ]);
      const fromCache = cached?.pages
        .flatMap((page) => page.items)
        .find((merchant) => merchant.organizationId === organizationId);

      if (fromCache) {
        return toContext(fromCache);
      }

      const page = await listLinkedMerchants(1, 50, signal);
      const fromList = page.items.find((merchant) => merchant.organizationId === organizationId);
      if (fromList) {
        return toContext(fromList);
      }

      const capability = await getLinkedMerchantOrderingCapability(organizationId, signal);
      const name = capability.organizationDisplayName.trim();
      return {
        organizationDisplayName: name || organizationId,
        customerDisplayName: null,
        businessCustomerId: null,
        statementTo: null,
      };
    },
  });
}
