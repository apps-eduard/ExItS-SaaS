import { useQuery } from "@tanstack/react-query";
import {
  getInventoryProductReservations,
  type PosInventoryReservationItemDto,
  type PosInventoryReservationsDto,
} from "@/api/pos/pos-inventory-client";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { LoadingState } from "@/components/exits/LoadingState";
import { ErrorState } from "@/components/exits/ErrorState";
import { EmptyState } from "@/components/exits/EmptyState";
import { AppLinkWithReturn } from "@/navigation/AppLinkWithReturn";
import { formatInventoryQty } from "@/features/inventory/inventory-reservation-display";
import { useI18n } from "@/i18n/I18nProvider";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { Lock } from "lucide-react";

function formatReservationWhen(iso: string | null | undefined): string {
  if (!iso) return "—";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "—";
  return date.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

export function isTransferCommitmentItem(item: PosInventoryReservationItemDto): boolean {
  return (
    item.sourceType === "InventoryTransfer" ||
    item.reservationType === "TransferOutbound" ||
    item.reservationType === "TransferInbound"
  );
}

/** Prefer DTO totals when present; otherwise sum transfer commitment rows (keeps summary aligned with list). */
export function resolveInTransitCommitmentTotals(data: PosInventoryReservationsDto): {
  outbound: number;
  inbound: number;
} {
  const fromItemsOutbound = data.reservations
    .filter((item) => item.reservationType === "TransferOutbound")
    .reduce((sum, item) => sum + (Number(item.reservedQuantity) || 0), 0);
  const fromItemsInbound = data.reservations
    .filter((item) => item.reservationType === "TransferInbound")
    .reduce((sum, item) => sum + (Number(item.reservedQuantity) || 0), 0);

  const dtoOutbound = data.inTransitOutboundQuantity;
  const dtoInbound = data.inTransitInboundQuantity;

  return {
    outbound:
      dtoOutbound != null && Number.isFinite(dtoOutbound) && dtoOutbound > 0
        ? dtoOutbound
        : fromItemsOutbound,
    inbound:
      dtoInbound != null && Number.isFinite(dtoInbound) && dtoInbound > 0
        ? dtoInbound
        : fromItemsInbound,
  };
}

/**
 * Summary Reserved mirrors the badge the user clicked.
 * Prefer true sellable holds (DTO / CPO rows). When those are 0 but outbound
 * transfer commitments exist, surface that qty under Reserved so the chip
 * (e.g. 3 → Iloilo) matches the drawer summary.
 * Available stays authoritative — do not recompute as onHand − reserved.
 */
export function resolveReservedCommitmentTotal(data: PosInventoryReservationsDto): number {
  const fromReservationItems = data.reservations
    .filter((item) => !isTransferCommitmentItem(item))
    .reduce((sum, item) => sum + (Number(item.reservedQuantity) || 0), 0);
  const dtoReserved = data.reservedQuantity;
  const reserved =
    dtoReserved != null && Number.isFinite(dtoReserved) && dtoReserved > 0
      ? dtoReserved
      : fromReservationItems;

  if (reserved > 0) {
    return reserved;
  }

  return resolveInTransitCommitmentTotals(data).outbound;
}

export function resolveTransferRouteLabel(
  item: PosInventoryReservationItemDto,
  t: (key: string) => string,
): string | null {
  if (!isTransferCommitmentItem(item)) {
    return null;
  }
  const peer = item.counterpartyName?.trim() || t("inventory.inTransitBranchFallback");
  const here = item.branchName?.trim() || t("inventory.thisBranch");
  const from = item.reservationType === "TransferInbound" ? peer : here;
  const to = item.reservationType === "TransferInbound" ? here : peer;
  return t("inventory.transferRoute").replace("{from}", from).replace("{to}", to);
}

function reservationTypeLabel(
  item: PosInventoryReservationItemDto,
  t: (key: string) => string,
): string {
  if (item.reservationType === "TemporaryProposal") {
    return t("inventory.reservationTypeTemporary");
  }
  if (item.reservationType === "ConfirmedOrder") {
    return t("inventory.reservationTypeConfirmed");
  }
  if (item.reservationType === "TransferOutbound") {
    return t("inventory.reservationTypeTransferOutbound");
  }
  if (item.reservationType === "TransferInbound") {
    return t("inventory.reservationTypeTransferInbound");
  }
  return item.reservationType;
}

function reservationStatusLabel(
  item: PosInventoryReservationItemDto,
  t: (key: string) => string,
): string {
  if (item.status === "Temporary") {
    return t("inventory.reservationStatusTemporary");
  }
  if (item.status === "Confirmed") {
    return t("inventory.reservationStatusConfirmed");
  }
  if (item.status === "InTransit") {
    return t("inventory.reservationStatusInTransit");
  }
  return item.status;
}

function reservationDeepLink(item: PosInventoryReservationItemDto): string | null {
  if (item.sourceType === "InventoryTransfer" && item.inventoryTransferId) {
    return `/inventory/transfers/${item.inventoryTransferId}`;
  }
  if (item.sourceType === "ConnectedPurchaseOrder" && item.connectedPurchaseOrderId) {
    return `/purchasing/incoming-orders/${item.connectedPurchaseOrderId}`;
  }
  return null;
}

function CommitmentRow({
  item,
  unitOfMeasure,
}: {
  item: PosInventoryReservationItemDto;
  unitOfMeasure: string;
}) {
  const { t } = useI18n();
  const deepLink = reservationDeepLink(item);
  const reference = item.referenceNumber?.trim() || item.connectedPurchaseOrderId;
  const isTransfer = isTransferCommitmentItem(item);
  const route = resolveTransferRouteLabel(item, t);
  const typeStatus = `${reservationTypeLabel(item, t)} · ${reservationStatusLabel(item, t)}`;

  return (
    <li
      className="flex flex-col gap-1.5 rounded-lg border border-border bg-[var(--exits-surface)] p-3"
      data-testid={`inventory-reservation-row-${item.reservationId}`}
      data-commitment-kind={isTransfer ? "transfer" : "reservation"}
    >
      <div className="flex min-w-0 flex-wrap items-start justify-between gap-2">
        {deepLink ? (
          <AppLinkWithReturn
            to={deepLink}
            className="min-w-0 truncate font-semibold text-primary no-underline hover:underline"
            data-testid={`inventory-reservation-ref-${item.reservationId}`}
          >
            {reference}
          </AppLinkWithReturn>
        ) : (
          <span className="min-w-0 truncate font-semibold">{reference}</span>
        )}
        <span className="shrink-0 tabular-nums font-semibold">
          {formatInventoryQty(item.reservedQuantity)} {unitOfMeasure}
        </span>
      </div>

      {isTransfer && route ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid={`inventory-reservation-route-${item.reservationId}`}
        >
          {route}
        </p>
      ) : item.counterpartyName?.trim() ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{item.counterpartyName}</p>
      ) : null}

      <div className="flex flex-wrap gap-x-3 gap-y-1 text-[length:var(--exits-text-sm)] text-muted">
        <span>{typeStatus}</span>
      </div>

      {item.expiresAtUtc ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("inventory.reservationExpires").replace(
            "{when}",
            formatReservationWhen(item.expiresAtUtc),
          )}
        </p>
      ) : null}

      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
        {t("inventory.reservationCreated").replace(
          "{when}",
          formatReservationWhen(item.createdAtUtc),
        )}
      </p>

      {deepLink ? (
        <AppLinkWithReturn
          to={deepLink}
          className="mt-1 inline-flex w-fit text-[length:var(--exits-text-sm)] font-semibold text-primary no-underline hover:underline"
          data-testid={`inventory-reservation-view-${item.reservationId}`}
        >
          {item.sourceType === "InventoryTransfer"
            ? t("inventory.viewTransfer")
            : t("inventory.viewPurchaseOrder")}
        </AppLinkWithReturn>
      ) : null}
    </li>
  );
}

function SummaryQty({
  label,
  quantity,
  unitOfMeasure,
  testId,
  emphasize = false,
}: {
  label: string;
  quantity: number;
  unitOfMeasure: string;
  testId: string;
  emphasize?: boolean;
}) {
  return (
    <>
      <dt className={emphasize ? "font-semibold" : "text-muted"}>{label}</dt>
      <dd
        className={
          emphasize
            ? "m-0 justify-self-end tabular-nums font-semibold"
            : "m-0 justify-self-end tabular-nums font-medium"
        }
        data-testid={testId}
      >
        {formatInventoryQty(quantity)} {unitOfMeasure}
      </dd>
    </>
  );
}

function ReservationsBody({
  data,
  loading,
  error,
}: {
  data: PosInventoryReservationsDto | undefined;
  loading: boolean;
  error: Error | null;
}) {
  const { t } = useI18n();

  if (loading && !data) {
    return <LoadingState label={t("inventory.commitmentsLoading")} />;
  }

  if (error && !data) {
    return <ErrorState title={t("error.title")} detail={error.message} error={error} />;
  }

  if (!data) return null;

  const reservationItems = data.reservations.filter((item) => !isTransferCommitmentItem(item));
  const transferItems = data.reservations.filter((item) => isTransferCommitmentItem(item));
  const { outbound: inTransitOut, inbound: inTransitIn } = resolveInTransitCommitmentTotals(data);
  const reservedTotal = resolveReservedCommitmentTotal(data);
  const hasAnyItems = reservationItems.length > 0 || transferItems.length > 0;

  return (
    <div className="flex flex-col gap-4" data-testid="inventory-reservations-body">
      <dl className="m-0 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1.5 text-[length:var(--exits-text-sm)]">
        <SummaryQty
          label={t("inventory.onHand")}
          quantity={data.onHandQuantity}
          unitOfMeasure={data.unitOfMeasure}
          testId="inventory-reservations-on-hand"
        />
        <SummaryQty
          label={t("inventory.reserved")}
          quantity={reservedTotal}
          unitOfMeasure={data.unitOfMeasure}
          testId="inventory-reservations-reserved"
        />
        <SummaryQty
          label={t("inventory.inTransitOutbound")}
          quantity={inTransitOut}
          unitOfMeasure={data.unitOfMeasure}
          testId="inventory-reservations-in-transit-out"
        />
        {inTransitIn > 0 ? (
          <SummaryQty
            label={t("inventory.inTransitInbound")}
            quantity={inTransitIn}
            unitOfMeasure={data.unitOfMeasure}
            testId="inventory-reservations-in-transit-in"
          />
        ) : null}
        <SummaryQty
          label={t("inventory.available")}
          quantity={data.availableQuantity}
          unitOfMeasure={data.unitOfMeasure}
          testId="inventory-reservations-available"
          emphasize
        />
      </dl>

      {!hasAnyItems ? (
        <EmptyState
          align="center"
          icon={<Lock className="size-5" strokeWidth={1.75} />}
          title={t("inventory.commitmentsEmpty")}
          detail={t("inventory.commitmentsEmptyDetail")}
        />
      ) : (
        <div className="flex flex-col gap-4">
          {reservationItems.length > 0 ? (
            <section data-testid="inventory-commitments-reservations-group">
              <h3 className="m-0 mb-2 text-[length:var(--exits-text-xs)] font-bold uppercase tracking-wide text-muted">
                {t("inventory.commitmentsGroupReservations")}
              </h3>
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {reservationItems.map((item) => (
                  <CommitmentRow
                    key={item.reservationId}
                    item={item}
                    unitOfMeasure={data.unitOfMeasure}
                  />
                ))}
              </ul>
            </section>
          ) : null}

          {transferItems.length > 0 ? (
            <section data-testid="inventory-commitments-in-transit-group">
              <h3 className="m-0 mb-2 text-[length:var(--exits-text-xs)] font-bold uppercase tracking-wide text-muted">
                {t("inventory.commitmentsGroupInTransit")}
              </h3>
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {transferItems.map((item) => (
                  <CommitmentRow
                    key={item.reservationId}
                    item={item}
                    unitOfMeasure={data.unitOfMeasure}
                  />
                ))}
              </ul>
            </section>
          ) : null}
        </div>
      )}
    </div>
  );
}

export function InventoryReservationsDrawer({
  open,
  onOpenChange,
  workspace,
  productId,
  productNameFallback,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  workspace: PosWorkspaceScope;
  productId: string;
  productNameFallback?: string;
}) {
  const { t } = useI18n();
  const query = useQuery({
    queryKey: ["inventory", "reservations", workspace.organizationId, workspace.branchId, productId],
    enabled: open && Boolean(productId),
    queryFn: ({ signal }) => getInventoryProductReservations(workspace, productId, signal),
  });

  const title = t("inventory.commitmentsTitle");
  const description =
    query.data?.productName?.trim() || productNameFallback?.trim() || undefined;

  return (
    <SideDrawer
      open={open}
      onClose={() => onOpenChange(false)}
      title={title}
      description={description}
      testId="inventory-reservations-drawer"
      closeLabel={t("inventory.reservationsClose")}
      closeTestId="inventory-reservations-drawer-close"
      panelClassName="exits-form-drawer__panel exits-form-drawer__panel--md"
    >
      <div className="exits-form-drawer" data-testid="inventory-reservations-drawer-content">
        <div className="exits-form-drawer__body">
          <ReservationsBody
            data={query.data}
            loading={query.isLoading}
            error={query.error as Error | null}
          />
        </div>
      </div>
    </SideDrawer>
  );
}
