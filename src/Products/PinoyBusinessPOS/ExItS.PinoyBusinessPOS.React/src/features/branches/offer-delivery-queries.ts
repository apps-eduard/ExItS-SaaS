import type { QueryClient } from "@tanstack/react-query";

/** Canonical React Query key for OrganizationFulfillmentSettings.OfferDelivery. */
export function organizationOfferDeliveryQueryKey(organizationId: string | null | undefined) {
  return ["organization-fulfillment-settings", organizationId ?? ""] as const;
}

/**
 * Invalidate every surface that projects Offer Delivery / effective Delivery.
 * Convenience UIs must all refresh from the same canonical setting.
 */
export async function invalidateOrganizationOfferDeliveryQueries(
  queryClient: QueryClient,
  organizationId?: string | null,
): Promise<void> {
  const tasks: Array<Promise<unknown>> = [
    queryClient.invalidateQueries({ queryKey: ["organization-fulfillment-settings"] }),
    queryClient.invalidateQueries({ queryKey: ["org-fulfillment-settings"] }),
    queryClient.invalidateQueries({ queryKey: ["connected-commerce", "offer-delivery"] }),
    queryClient.invalidateQueries({ queryKey: ["connected-commerce"] }),
    queryClient.invalidateQueries({ queryKey: ["branch-management-summary"] }),
    queryClient.invalidateQueries({ queryKey: ["branch-fulfillment-list"] }),
    queryClient.invalidateQueries({ queryKey: ["branch-fulfillment-detail"] }),
    queryClient.invalidateQueries({ queryKey: ["business-customers"] }),
    queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "commerce-readiness"] }),
    queryClient.invalidateQueries({ queryKey: ["shell", "needs-attention"] }),
  ];
  if (organizationId) {
    tasks.push(
      queryClient.invalidateQueries({
        queryKey: organizationOfferDeliveryQueryKey(organizationId),
      }),
    );
  }
  await Promise.all(tasks);
}

/** Deep-link for the full organization Offer Delivery setting. */
export const OFFER_DELIVERY_SETTINGS_PATH = "/org/connected-commerce?tab=fulfillment";
