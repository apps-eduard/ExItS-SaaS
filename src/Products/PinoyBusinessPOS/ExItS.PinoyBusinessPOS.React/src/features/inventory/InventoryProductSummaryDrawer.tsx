import { useQuery } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { getEffectiveExpirySalePolicy } from "@/api/pos/pos-expiry-sale-policy-client";
import type { PosInventoryAccountDto } from "@/api/pos/pos-inventory-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { LoadingState } from "@/components/exits/LoadingState";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  formatInventoryQty,
  resolveAvailableQuantity,
  resolveExpiredQuantity,
  resolveInTransitInboundQuantity,
  resolveInTransitOutboundQuantity,
  resolvePendingReturnQuantity,
  resolveReservedQuantity,
  resolveSalePolicyBlockedQuantity,
  resolveStockRequestCommittedQuantity,
} from "@/features/inventory/inventory-reservation-display";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { AppLinkWithReturn } from "@/navigation/AppLinkWithReturn";

function qtyLabel(quantity: number, unitOfMeasure: string): string {
  const uom = unitOfMeasure.trim();
  return uom ? `${formatInventoryQty(quantity)} ${uom}` : formatInventoryQty(quantity);
}

function daysLabel(days: number, t: (key: MessageKey) => string): string {
  return t("inventory.productSummary.daysValue").replace("{days}", String(days));
}

function expirySaleSourceLabel(source: string, t: (key: MessageKey) => string): string {
  switch (source) {
    case "OrganizationDefault":
      return t("inventory.expirySalePolicy.sourceOrganizationDefault");
    case "OrganizationCategory":
      return t("inventory.expirySalePolicy.sourceOrganizationCategory");
    case "Branch":
      return t("inventory.expirySalePolicy.sourceBranch");
    case "BranchCategory":
      return t("inventory.expirySalePolicy.sourceBranchCategory");
    default:
      return source;
  }
}

function monitoringLabel(
  mode: string | null | undefined,
  t: (key: MessageKey) => string,
): string {
  switch (mode) {
    case "BranchDefault":
      return t("lowStockSettings.monitoringBranchDefault");
    case "Custom":
      return t("lowStockSettings.monitoringCustom");
    case "NotMonitored":
      return t("lowStockSettings.monitoringNotMonitored");
    default:
      return mode?.trim() || t("inventory.productSummary.notSet");
  }
}

function SummaryRow({
  label,
  value,
  testId,
  emphasize,
}: {
  label: string;
  value: string;
  testId?: string;
  emphasize?: boolean;
}) {
  return (
    <>
      <dt className="m-0 text-muted">{label}</dt>
      <dd
        className={
          emphasize
            ? "m-0 justify-self-end text-end font-semibold tabular-nums"
            : "m-0 justify-self-end text-end tabular-nums font-medium"
        }
        data-testid={testId}
      >
        {value}
      </dd>
    </>
  );
}

function SummarySection({
  title,
  testId,
  children,
}: {
  title: string;
  testId?: string;
  children: ReactNode;
}) {
  return (
    <Card
      as="section"
      treatment="bordered"
      padding="compact"
      className="flex min-w-0 flex-col gap-2 p-3"
      data-testid={testId}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
        {title}
      </h3>
      <dl className="m-0 grid grid-cols-[1fr_auto] gap-x-4 gap-y-1.5 text-[length:var(--exits-text-sm)]">
        {children}
      </dl>
    </Card>
  );
}

export function InventoryProductSummaryDrawer({
  open,
  onOpenChange,
  workspace,
  account,
  onOpenReservations,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  workspace: PosWorkspaceScope;
  account: PosInventoryAccountDto | null;
  onOpenReservations?: (product: { productId: string; name: string }) => void;
}) {
  const { t } = useI18n();
  const productId = account?.productId ?? "";
  const tracksExpiration = account?.isTracked === true && account.tracksExpiration === true;

  const salePolicyQuery = useQuery({
    queryKey: [
      "expiry-sale-policy",
      "effective",
      workspace.organizationId,
      workspace.branchId,
      account?.categoryId ?? "none",
      productId,
    ],
    enabled: open && tracksExpiration && Boolean(productId),
    queryFn: ({ signal }) =>
      getEffectiveExpirySalePolicy(workspace, account?.categoryId ?? null, signal),
  });

  if (!account) {
    return (
      <SideDrawer
        open={open}
        onClose={() => onOpenChange(false)}
        title={t("inventory.productSummary.title")}
        testId="inventory-product-summary-drawer"
        closeLabel={t("inventory.productSummary.close")}
        closeTestId="inventory-product-summary-drawer-close"
        panelClassName="exits-form-drawer__panel exits-form-drawer__panel--md"
      >
        <div />
      </SideDrawer>
    );
  }

  const uom = account.unitOfMeasure;
  const availableQty = resolveAvailableQuantity(account);
  const reservedQty = resolveReservedQuantity(account);
  const committedQty = resolveStockRequestCommittedQuantity(account);
  const pendingReturnQty = resolvePendingReturnQuantity(account);
  const inTransitOutQty = resolveInTransitOutboundQuantity(account);
  const inTransitInQty = resolveInTransitInboundQuantity(account);
  const expiredQty = tracksExpiration ? resolveExpiredQuantity(account) : 0;
  const saleBlockedQty = tracksExpiration ? resolveSalePolicyBlockedQuantity(account) : 0;
  const nearExpiryQty =
    tracksExpiration && account.nearExpiryQuantity != null && Number.isFinite(account.nearExpiryQuantity)
      ? Math.max(0, account.nearExpiryQuantity)
      : 0;
  const sellableQty =
    tracksExpiration && account.sellableQuantity != null && Number.isFinite(account.sellableQuantity)
      ? Math.max(0, account.sellableQuantity)
      : null;

  const dash = t("inventory.productSummary.notSet");
  const warningDays =
    tracksExpiration && account.expirationWarningDays != null && account.expirationWarningDays > 0
      ? daysLabel(account.expirationWarningDays, t)
      : tracksExpiration
        ? daysLabel(7, t)
        : dash;

  return (
    <SideDrawer
      open={open}
      onClose={() => onOpenChange(false)}
      title={t("inventory.productSummary.title")}
      description={account.name}
      testId="inventory-product-summary-drawer"
      closeLabel={t("inventory.productSummary.close")}
      closeTestId="inventory-product-summary-drawer-close"
      panelClassName="exits-form-drawer__panel exits-form-drawer__panel--md"
    >
      <div className="exits-form-drawer" data-testid="inventory-product-summary-drawer-content">
        <div className="exits-form-drawer__body flex flex-col gap-5">
          <SummarySection
            title={t("inventory.productSummary.sectionStock")}
            testId="inventory-product-summary-stock"
          >
            <SummaryRow
              label={t("inventory.onHand")}
              value={qtyLabel(account.onHandQuantity, uom)}
              testId="inventory-product-summary-on-hand"
            />
            <SummaryRow
              label={t("inventory.reserved")}
              value={qtyLabel(reservedQty, uom)}
              testId="inventory-product-summary-reserved"
            />
            <SummaryRow
              label={t("inventory.committed")}
              value={qtyLabel(committedQty, uom)}
              testId="inventory-product-summary-committed"
            />
            {pendingReturnQty > 0 ? (
              <SummaryRow
                label={t("inventory.productSummary.pendingReturn")}
                value={qtyLabel(pendingReturnQty, uom)}
                testId="inventory-product-summary-pending-return"
              />
            ) : null}
            {inTransitOutQty > 0 ? (
              <SummaryRow
                label={t("inventory.inTransitOutbound")}
                value={
                  account.inTransitOutboundBranchName?.trim()
                    ? `${qtyLabel(inTransitOutQty, uom)} → ${account.inTransitOutboundBranchName.trim()}`
                    : qtyLabel(inTransitOutQty, uom)
                }
                testId="inventory-product-summary-in-transit-out"
              />
            ) : null}
            {inTransitInQty > 0 ? (
              <SummaryRow
                label={t("inventory.inTransitInbound")}
                value={
                  account.inTransitInboundBranchName?.trim()
                    ? `${qtyLabel(inTransitInQty, uom)} ← ${account.inTransitInboundBranchName.trim()}`
                    : qtyLabel(inTransitInQty, uom)
                }
                testId="inventory-product-summary-in-transit-in"
              />
            ) : null}
            <SummaryRow
              label={t("inventory.available")}
              value={qtyLabel(availableQty, uom)}
              testId="inventory-product-summary-available"
              emphasize
            />
            {tracksExpiration && sellableQty != null ? (
              <SummaryRow
                label={t("inventory.sellable")}
                value={qtyLabel(sellableQty, uom)}
                testId="inventory-product-summary-sellable"
              />
            ) : null}
            {expiredQty > 0 ? (
              <SummaryRow
                label={t("inventory.expiredQty")}
                value={qtyLabel(expiredQty, uom)}
                testId="inventory-product-summary-expired"
              />
            ) : null}
            {nearExpiryQty > 0 ? (
              <SummaryRow
                label={t("inventory.nearExpiryQty")}
                value={qtyLabel(nearExpiryQty, uom)}
                testId="inventory-product-summary-near-expiry"
              />
            ) : null}
            {saleBlockedQty > 0 ? (
              <SummaryRow
                label={t("inventory.saleBlocked")}
                value={qtyLabel(saleBlockedQty, uom)}
                testId="inventory-product-summary-sale-blocked"
              />
            ) : null}
          </SummarySection>

          <SummarySection
            title={t("inventory.productSummary.sectionConfig")}
            testId="inventory-product-summary-config"
          >
            <SummaryRow
              label={t("inventory.col.tracking")}
              value={account.isTracked ? t("inventory.tracked") : t("inventory.notTracked")}
              testId="inventory-product-summary-tracking"
            />
            <SummaryRow
              label={t("inventory.productSummary.expirationTracking")}
              value={
                tracksExpiration
                  ? t("inventory.productSummary.expirationOn")
                  : t("inventory.productSummary.expirationOff")
              }
              testId="inventory-product-summary-expiration-tracking"
            />
            <SummaryRow
              label={t("inventory.nearExpiryWarningLabel")}
              value={warningDays}
              testId="inventory-product-summary-warning-days"
            />
            {tracksExpiration ? (
              salePolicyQuery.isLoading && !salePolicyQuery.data ? (
                <div className="col-span-2">
                  <LoadingState label={t("inventory.productSummary.loadingPolicy")} />
                </div>
              ) : (
                <>
                  <SummaryRow
                    label={t("inventory.productSummary.stopSellingDays")}
                    value={
                      salePolicyQuery.data
                        ? daysLabel(salePolicyQuery.data.stopSellingDaysBeforeExpiry, t)
                        : dash
                    }
                    testId="inventory-product-summary-stop-selling-days"
                  />
                  <SummaryRow
                    label={t("inventory.expirySalePolicy.effectiveSource")}
                    value={
                      salePolicyQuery.data
                        ? expirySaleSourceLabel(salePolicyQuery.data.source, t)
                        : dash
                    }
                    testId="inventory-product-summary-stop-selling-source"
                  />
                </>
              )
            ) : (
              <SummaryRow
                label={t("inventory.productSummary.stopSellingDays")}
                value={dash}
                testId="inventory-product-summary-stop-selling-days"
              />
            )}
            {account.isTracked ? (
              <>
                <SummaryRow
                  label={t("lowStockSettings.stockStatus")}
                  value={account.stockStatus?.trim() || dash}
                  testId="inventory-product-summary-stock-status"
                />
                <SummaryRow
                  label={t("lowStockSettings.monitoring")}
                  value={monitoringLabel(account.monitoringMode, t)}
                  testId="inventory-product-summary-monitoring"
                />
                <SummaryRow
                  label={t("lowStockSettings.lowStockAt")}
                  value={
                    account.reorderLevel != null && Number.isFinite(account.reorderLevel)
                      ? qtyLabel(account.reorderLevel, uom)
                      : dash
                  }
                  testId="inventory-product-summary-reorder-level"
                />
                <SummaryRow
                  label={t("lowStockSettings.reorderQty")}
                  value={
                    account.reorderQuantity != null && Number.isFinite(account.reorderQuantity)
                      ? qtyLabel(account.reorderQuantity, uom)
                      : dash
                  }
                  testId="inventory-product-summary-reorder-qty"
                />
              </>
            ) : null}
            {account.categoryName?.trim() ? (
              <SummaryRow
                label={t("lowStockSettings.category")}
                value={account.categoryName.trim()}
                testId="inventory-product-summary-category"
              />
            ) : null}
            {account.sku?.trim() ? (
              <SummaryRow
                label={t("catalog.sku")}
                value={account.sku.trim()}
                testId="inventory-product-summary-sku"
              />
            ) : null}
            <SummaryRow
              label={t("inventory.col.unit")}
              value={uom}
              testId="inventory-product-summary-unit"
            />
          </SummarySection>

          <div className="flex flex-col gap-2">
            {reservedQty > 0 || inTransitOutQty > 0 || inTransitInQty > 0 ? (
              <Button
                type="button"
                appearance="outline"
                data-testid="inventory-product-summary-view-commitments"
                onClick={() => {
                  onOpenReservations?.({
                    productId: account.productId,
                    name: account.name,
                  });
                  onOpenChange(false);
                }}
              >
                {t("inventory.viewReservations")}
              </Button>
            ) : null}
            <Button asChild type="button" intent="primary" appearance="outline">
              <AppLinkWithReturn
                to={`/inventory/${account.productId}`}
                data-testid="inventory-product-summary-view-details"
                onClick={() => onOpenChange(false)}
              >
                {t("inventory.viewInventoryDetails")}
              </AppLinkWithReturn>
            </Button>
          </div>
        </div>
      </div>
    </SideDrawer>
  );
}
