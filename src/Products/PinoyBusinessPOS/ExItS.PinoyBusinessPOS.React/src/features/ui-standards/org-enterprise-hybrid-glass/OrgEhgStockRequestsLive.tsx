import { StockRequestListPage } from "@/features/replenishment/StockRequestListPage";
import { OrgEhgLiveFrame } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgLiveFrame";

/** UI Standards · Stock Requests & Transfers — live replenishment APIs. */
export function OrgEhgStockRequestsLive() {
  return (
    <OrgEhgLiveFrame
      title="Stock Requests & Transfers"
      description="Outgoing and incoming requests with existing approval and transfer lifecycle."
    >
      <StockRequestListPage />
    </OrgEhgLiveFrame>
  );
}
