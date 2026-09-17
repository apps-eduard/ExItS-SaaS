import type { ReactNode } from "react";
import type {
  PosGoodsReceiptDto,
  PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { LoadingState } from "@/components/exits/LoadingState";
import { PurchaseOrderActivityTimeline } from "@/features/purchasing/PurchaseOrderActivityTimeline";
import type { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useI18n } from "@/i18n/I18nProvider";

export type PurchaseOrderTimelineDrawerProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  po: PosPurchaseOrderDto;
  receipts: readonly PosGoodsReceiptDto[];
  resolveActor: ReturnType<typeof useActorDirectory>["resolve"];
  isResolving: boolean;
  receiptsLoading?: boolean;
  renderReceiptDetail?: (receiptId: string) => ReactNode;
};

/**
 * Read-only PO activity timeline in the canonical SideDrawer shell
 * (full-width sheet on mobile, right drawer on desktop via FormDrawer panel sizes).
 */
export function PurchaseOrderTimelineDrawer({
  open,
  onOpenChange,
  po,
  receipts,
  resolveActor,
  isResolving,
  receiptsLoading = false,
  renderReceiptDetail,
}: PurchaseOrderTimelineDrawerProps) {
  const { t } = useI18n();

  return (
    <SideDrawer
      open={open}
      onClose={() => onOpenChange(false)}
      title={t("purchasing.timelineTitle")}
      description={po.poNumber?.trim() || undefined}
      testId="po-timeline-drawer"
      closeLabel={t("purchasing.timelineClose")}
      closeTestId="po-timeline-drawer-close"
      panelClassName="exits-form-drawer__panel exits-form-drawer__panel--md"
    >
      <div className="exits-form-drawer" data-testid="po-timeline-drawer-content">
        <div className="exits-form-drawer__body">
          {receiptsLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
          {!receiptsLoading ? (
            <PurchaseOrderActivityTimeline
              po={po}
              receipts={receipts}
              resolveActor={resolveActor}
              isResolving={isResolving}
              renderReceiptDetail={renderReceiptDetail}
            />
          ) : null}
        </div>
      </div>
    </SideDrawer>
  );
}
