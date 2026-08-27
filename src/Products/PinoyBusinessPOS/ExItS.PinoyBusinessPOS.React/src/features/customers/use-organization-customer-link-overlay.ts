import { useQuery } from "@tanstack/react-query";
import { loadOrganizationCustomerLinkOverlay } from "@/api/platform/organization-customer-links-client";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  EMPTY_CUSTOMER_LIST_CONNECTION_OVERLAY,
  type CustomerListConnectionOverlay,
} from "@/features/customers/customer-list-connection";

export function useOrganizationCustomerLinkOverlay(
  organizationId: string | null | undefined,
): CustomerListConnectionOverlay {
  const online = useBrowserOnline();
  const org = organizationId?.trim() || "";

  const query = useQuery({
    queryKey: ["customers", "organization-link-overlay", org],
    enabled: Boolean(org) && online,
    queryFn: ({ signal }) => loadOrganizationCustomerLinkOverlay(org, signal),
  });

  return query.data ?? EMPTY_CUSTOMER_LIST_CONNECTION_OVERLAY;
}
