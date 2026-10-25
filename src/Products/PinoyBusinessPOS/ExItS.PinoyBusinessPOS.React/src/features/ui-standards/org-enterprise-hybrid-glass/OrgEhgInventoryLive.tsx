import { InventoryListPage } from "@/features/inventory/InventoryListPage";
import { OrgEhgLiveFrame } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgLiveFrame";

/** UI Standards · Inventory & Warehouse — live POS inventory. */
export function OrgEhgInventoryLive() {
  return (
    <OrgEhgLiveFrame
      title="Inventory & Warehouse"
      description="On-hand, reserved, committed, and available quantities from existing inventory APIs."
    >
      <InventoryListPage />
    </OrgEhgLiveFrame>
  );
}
