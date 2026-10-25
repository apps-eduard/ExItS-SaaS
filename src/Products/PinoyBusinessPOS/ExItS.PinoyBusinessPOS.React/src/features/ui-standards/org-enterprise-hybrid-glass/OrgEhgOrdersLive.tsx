import { SellerOrdersPage } from "@/features/customer-ordering/SellerOrdersPage";
import { OrgEhgLiveFrame } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgLiveFrame";

/**
 * UI Standards · Customer Orders & Payments — live seller order workflows.
 * Detail / mutation links use existing /orders routes (same session and APIs).
 */
export function OrgEhgOrdersLive() {
  return (
    <OrgEhgLiveFrame
      title="Customer Orders & Payments"
      description="Seller list and authorized status/payment transitions via existing POS customer-order APIs."
    >
      <SellerOrdersPage />
    </OrgEhgLiveFrame>
  );
}
