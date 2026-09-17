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
  return item.status;
}

function reservationDeepLink(item: PosInventoryReservationItemDto): string | null {
  if (item.sourceType === "ConnectedPurchaseOrder" && item.connectedPurchaseOrderId) {
    return `/purchasing/incoming-orders/${item.connectedPurchaseOrderId}`;
  }
  return null;
}

function ReservationRow({
  item,
  unitOfMeasure,
}: {
  item: PosInventoryReservationItemDto;
  unitOfMeasure: string;
}) {
  const { t } = useI18n();
  const deepLink = reservationDeepLink(item);
  const reference = item.referenceNumber?.trim() || item.connectedPurchaseOrderId;

  return (
    <li
      className="flex flex-col gap-1.5 rounded-lg border border-border bg-[var(--exits-surface)] p-3"
      data-testid={`inventory-reservation-row-${item.reservationId}`}
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

      {item.counterpartyName?.trim() ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{item.counterpartyName}</p>
      ) : null}

      <div className="flex flex-wrap gap-x-3 gap-y-1 text-[length:var(--exits-text-sm)] text-muted">
        <span>{reservationTypeLabel(item, t)}</span>
        <span>{reservationStatusLabel(item, t)}</span>
        {item.branchName?.trim() ? <span>{item.branchName}</span> : null}
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
          {t("inventory.viewPurchaseOrder")}
        </AppLinkWithReturn>
      ) : null}
    </li>
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
    return <LoadingState label={t("inventory.reservationsLoading")} />;
  }

  if (error && !data) {
    return <ErrorState title={t("error.title")} detail={error.message} error={error} />;
  }

  if (!data) return null;

  return (
    <div className="flex flex-col gap-4" data-testid="inventory-reservations-body">
      <dl className="m-0 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1.5 text-[length:var(--exits-text-sm)]">
        <dt className="text-muted">{t("inventory.onHand")}</dt>
        <dd className="m-0 justify-self-end tabular-nums font-medium" data-testid="inventory-reservations-on-hand">
          {formatInventoryQty(data.onHandQuantity)} {data.unitOfMeasure}
        </dd>
        <dt className="text-muted">{t("inventory.reserved")}</dt>
        <dd className="m-0 justify-self-end tabular-nums font-medium" data-testid="inventory-reservations-reserved">
          {formatInventoryQty(data.reservedQuantity)} {data.unitOfMeasure}
        </dd>
        <dt className="font-semibold">{t("inventory.available")}</dt>
        <dd
          className="m-0 justify-self-end tabular-nums font-semibold"
          data-testid="inventory-reservations-available"
        >
          {formatInventoryQty(data.availableQuantity)} {data.unitOfMeasure}
        </dd>
      </dl>

      {data.reservations.length === 0 ? (
        <EmptyState
          align="center"
          icon={<Lock className="size-5" strokeWidth={1.75} />}
          title={t("inventory.reservationsEmpty")}
          detail={t("inventory.reservationsEmptyDetail")}
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="inventory-reservations-list">
          {data.reservations.map((item) => (
            <ReservationRow key={item.reservationId} item={item} unitOfMeasure={data.unitOfMeasure} />
          ))}
        </ul>
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

  const title = t("inventory.reservationsTitle");
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
