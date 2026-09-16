import { useState } from "react";
import {
  CircleAlert,
  CircleCheck,
  Package,
  Store,
  Truck,
  type LucideIcon,
} from "lucide-react";
import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { StatusChip } from "@/components/exits/StatusChip";
import { Switch } from "@/components/ui/switch";
import {
  deliveryEnablementLabel,
  missingRequirementMessageKey,
  orderingEnablementLabel,
  pickupEnablementLabel,
  type EnablementLabel,
} from "@/features/branches/branch-readiness-labels";
import { resolveFulfillmentToggle } from "@/features/branches/fulfillment-toggle";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

function enablementTone(label: EnablementLabel): "success" | "warning" | "info" | "danger" {
  if (label === "enabled") return "success";
  if (label === "paused" || label === "notReady") return "warning";
  return "info";
}

function enablementStatusWord(label: EnablementLabel): MessageKey {
  if (label === "enabled") return "branches.status.enabled";
  if (label === "paused") return "branches.status.paused";
  if (label === "notReady") return "branches.status.notReady";
  return "branches.status.disabled";
}

type BranchOverviewPanelProps = {
  readiness: BranchFulfillmentReadinessDto;
  busy: boolean;
  t: (key: MessageKey) => string;
  onTogglePickup: (enabled: boolean) => void;
  onToggleDelivery: (enabled: boolean) => void;
  onEnableOrdering: () => void;
  onPauseOrders: () => void;
  onResumeOrders: () => void;
};

type ChannelCardProps = {
  testId: string;
  statusTestId: string;
  title: string;
  progress?: string;
  statusLabel: EnablementLabel;
  Icon: LucideIcon;
  switchId: string;
  switchTestId: string;
  checked: boolean;
  disabled: boolean;
  busy: boolean;
  hint?: string | null;
  className?: string;
  t: (key: MessageKey) => string;
  onCheckedChange: (next: boolean) => void;
};

type ToggleConfirm =
  | { kind: "pickup"; next: boolean }
  | { kind: "delivery"; next: boolean }
  | { kind: "ordering"; action: "enable" | "pause" | "resume" };

function ChannelCard({
  testId,
  statusTestId,
  title,
  progress,
  statusLabel,
  Icon,
  switchId,
  switchTestId,
  checked,
  disabled,
  busy,
  hint,
  className,
  t,
  onCheckedChange,
}: ChannelCardProps) {
  const titleId = `${switchId}-label`;
  return (
    <div
      className={cn("branch-overview-progress__item", className)}
      data-testid={testId}
    >
      <div className="branch-overview-progress__top">
        <div className="branch-overview-progress__identity">
          <span className="branch-overview-progress__icon" aria-hidden>
            <Icon className="size-4" strokeWidth={1.75} />
          </span>
          <div className="min-w-0">
            <p className="branch-overview-progress__label m-0" id={titleId}>
              {title}
            </p>
            {progress ? (
              <p className="branch-overview-progress__value m-0">{progress}</p>
            ) : null}
          </div>
        </div>
        <div className="branch-overview-progress__controls">
          <Switch
            id={switchId}
            checked={checked}
            disabled={disabled || busy}
            aria-busy={busy || undefined}
            aria-labelledby={titleId}
            data-testid={switchTestId}
            onCheckedChange={onCheckedChange}
          />
          <div data-testid={statusTestId}>
            <StatusChip tone={enablementTone(statusLabel)} shape="soft" appearance="outline">
              {t(enablementStatusWord(statusLabel))}
            </StatusChip>
          </div>
        </div>
      </div>
      {hint ? (
        <p className="branch-overview-progress__hint m-0" data-testid={`${switchTestId}-hint`}>
          {hint}
        </p>
      ) : null}
    </div>
  );
}

function confirmCopy(
  confirm: ToggleConfirm,
  t: (key: MessageKey) => string,
): {
  variant: "info" | "warning";
  title: string;
  description: string;
  confirmLabel: string;
} {
  if (confirm.kind === "pickup") {
    return confirm.next
      ? {
          variant: "info",
          title: t("branches.confirm.enablePickupTitle"),
          description: t("branches.confirm.enablePickupDetail"),
          confirmLabel: t("branches.confirm.enablePickupConfirm"),
        }
      : {
          variant: "warning",
          title: t("branches.confirm.disablePickupTitle"),
          description: t("branches.confirm.disablePickupDetail"),
          confirmLabel: t("branches.confirm.disablePickupConfirm"),
        };
  }
  if (confirm.kind === "delivery") {
    return confirm.next
      ? {
          variant: "info",
          title: t("branches.confirm.enableDeliveryTitle"),
          description: t("branches.confirm.enableDeliveryDetail"),
          confirmLabel: t("branches.confirm.enableDeliveryConfirm"),
        }
      : {
          variant: "warning",
          title: t("branches.confirm.disableDeliveryTitle"),
          description: t("branches.confirm.disableDeliveryDetail"),
          confirmLabel: t("branches.confirm.disableDeliveryConfirm"),
        };
  }
  if (confirm.action === "enable") {
    return {
      variant: "info",
      title: t("branches.confirm.enableOrderingTitle"),
      description: t("branches.confirm.enableOrderingDetail"),
      confirmLabel: t("branches.confirm.enableOrderingConfirm"),
    };
  }
  if (confirm.action === "resume") {
    return {
      variant: "info",
      title: t("branches.confirm.resumeOrderingTitle"),
      description: t("branches.confirm.resumeOrderingDetail"),
      confirmLabel: t("branches.confirm.resumeOrderingConfirm"),
    };
  }
  return {
    variant: "warning",
    title: t("branches.confirm.pauseOrderingTitle"),
    description: t("branches.confirm.pauseOrderingDetail"),
    confirmLabel: t("branches.confirm.pauseOrderingConfirm"),
  };
}

export function BranchOverviewPanel({
  readiness,
  busy,
  t,
  onTogglePickup,
  onToggleDelivery,
  onEnableOrdering,
  onPauseOrders,
  onResumeOrders,
}: BranchOverviewPanelProps) {
  const [toggleConfirm, setToggleConfirm] = useState<ToggleConfirm | null>(null);
  const orderingLabel = orderingEnablementLabel(readiness);
  const pickupLabel = pickupEnablementLabel(readiness);
  const deliveryLabel = deliveryEnablementLabel(readiness);
  const missingRequirements = readiness.missingRequirements;
  const setupComplete = missingRequirements.length === 0;
  const noFulfillmentMethod =
    !readiness.pickupEnabled && !readiness.deliveryEnabled;
  const configCompleteButMethodsOff = setupComplete && noFulfillmentMethod;

  const pickup = resolveFulfillmentToggle({
    channel: "pickup",
    enabled: readiness.pickupEnabled,
    ready: readiness.pickupReady,
    canUseDelivery: readiness.canUseDelivery,
    pending: busy,
  });
  const delivery = resolveFulfillmentToggle({
    channel: "delivery",
    enabled: readiness.deliveryEnabled,
    ready: readiness.deliveryReady,
    canUseDelivery: readiness.canUseDelivery,
    pending: busy,
  });

  const orderingChecked =
    readiness.customerOrderingEnabled && !readiness.onlineOrdersPaused;
  const orderingCanEnable =
    readiness.canUseCustomerOrdering &&
    readiness.customerOrderingReady &&
    !readiness.customerOrderingEnabled;
  const orderingCanPauseResume = readiness.customerOrderingEnabled;
  const orderingDisabled =
    busy ||
    (!orderingCanPauseResume && !orderingCanEnable) ||
    !readiness.canUseCustomerOrdering;

  const dialogCopy = toggleConfirm ? confirmCopy(toggleConfirm, t) : null;

  function applyToggleConfirm(confirm: ToggleConfirm) {
    if (confirm.kind === "pickup") {
      onTogglePickup(confirm.next);
      return;
    }
    if (confirm.kind === "delivery") {
      onToggleDelivery(confirm.next);
      return;
    }
    if (confirm.action === "enable") {
      onEnableOrdering();
      return;
    }
    if (confirm.action === "resume") {
      onResumeOrders();
      return;
    }
    onPauseOrders();
  }

  return (
    <section
      className="catalog-form-section exits-animate-panel branch-readiness gap-3"
      data-testid="branch-readiness-panel"
    >
      <div className="branch-readiness__header">
        <h2 className="catalog-form-section__title">{t("branches.readinessTitle")}</h2>
        {readiness.storeStatusMessage ? (
          <p className="branch-readiness__store-status m-0 text-[length:var(--exits-text-sm)] text-muted">
            {readiness.storeStatusMessage}
          </p>
        ) : null}
      </div>

      <div
        id="branch-fulfillment-toggles"
        data-testid="branch-fulfillment-toggles"
      >
        <div className="branch-overview-progress" data-testid="branch-setup-progress">
          <ChannelCard
            testId="pickup-progress"
            statusTestId="pickup-status"
            title={t("branches.channel.pickup")}
            progress={t("branches.progress.of")
              .replace("{complete}", String(readiness.pickupSectionsComplete))
              .replace("{total}", String(readiness.pickupSectionsTotal))}
            statusLabel={pickupLabel}
            Icon={Package}
            switchId="overview-pickup-switch"
            switchTestId="overview-pickup-switch"
            checked={pickup.checked}
            disabled={pickup.disabled}
            busy={busy}
            hint={
              pickup.hintKey
                ? t(pickup.hintKey)
                : t("branches.poFulfillment.helper.pickup")
            }
            t={t}
            onCheckedChange={(next) => {
              if (next && pickup.enableBlocked) return;
              setToggleConfirm({ kind: "pickup", next });
            }}
          />
          <ChannelCard
            testId="delivery-progress"
            statusTestId="delivery-status"
            title={t("branches.channel.delivery")}
            progress={t("branches.progress.of")
              .replace("{complete}", String(readiness.deliverySectionsComplete))
              .replace("{total}", String(readiness.deliverySectionsTotal))}
            statusLabel={deliveryLabel}
            Icon={Truck}
            switchId="overview-delivery-switch"
            switchTestId="overview-delivery-switch"
            checked={delivery.checked}
            disabled={delivery.disabled}
            busy={busy}
            hint={
              delivery.hintKey
                ? t(delivery.hintKey)
                : t("branches.poFulfillment.helper.delivery")
            }
            t={t}
            onCheckedChange={(next) => {
              if (next && delivery.enableBlocked) return;
              setToggleConfirm({ kind: "delivery", next });
            }}
          />
          <ChannelCard
            testId="ordering-progress"
            statusTestId="ordering-status"
            className="branch-overview-progress__item--ordering"
            title={t("branches.channel.ordering")}
            statusLabel={orderingLabel}
            Icon={Store}
            switchId="overview-ordering-switch"
            switchTestId="overview-ordering-switch"
            checked={orderingChecked}
            disabled={orderingDisabled}
            busy={busy}
            hint={
              !readiness.canUseCustomerOrdering
                ? null
                : !readiness.customerOrderingReady && !readiness.customerOrderingEnabled
                  ? t("branches.toggle.completeSetupFirst")
                  : null
            }
            t={t}
            onCheckedChange={(next) => {
              if (next) {
                if (readiness.onlineOrdersPaused) {
                  setToggleConfirm({ kind: "ordering", action: "resume" });
                  return;
                }
                if (!readiness.customerOrderingEnabled) {
                  setToggleConfirm({ kind: "ordering", action: "enable" });
                }
                return;
              }
              if (readiness.customerOrderingEnabled && !readiness.onlineOrdersPaused) {
                setToggleConfirm({ kind: "ordering", action: "pause" });
              }
            }}
          />
        </div>
      </div>

      {!setupComplete ? (
        <div className="branch-readiness__checklist" data-testid="branch-missing-requirements">
          <p className="branch-readiness__checklist-title m-0">{t("branches.setupGapsTitle")}</p>
          <p className="branch-readiness__checklist-lede m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("branches.setupGapsLede")}
          </p>
          <ul className="branch-readiness__items m-0 list-none p-0">
            {missingRequirements.map((code) => (
              <li key={code} className="branch-readiness__item">
                <CircleAlert className="branch-readiness__item-icon" aria-hidden />
                <span>{t(missingRequirementMessageKey(code))}</span>
              </li>
            ))}
          </ul>
        </div>
      ) : (
        <div
          className={
            configCompleteButMethodsOff
              ? "branch-readiness__checklist branch-readiness__checklist--secondary"
              : "branch-readiness__ready"
          }
          data-testid="branch-missing-none"
          role="status"
        >
          {configCompleteButMethodsOff ? (
            <CircleAlert className="branch-readiness__item-icon" aria-hidden />
          ) : (
            <CircleCheck className="branch-readiness__ready-icon" aria-hidden />
          )}
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {configCompleteButMethodsOff
              ? t("branches.configuredButMethodsOff")
              : t("branches.missingNone")}
          </p>
        </div>
      )}

      <ConfirmActionDialog
        open={toggleConfirm != null}
        variant={dialogCopy?.variant ?? "default"}
        title={dialogCopy?.title ?? ""}
        description={dialogCopy?.description ?? ""}
        confirmLabel={dialogCopy?.confirmLabel ?? ""}
        cancelLabel={t("branches.cancel")}
        pending={busy}
        testId="branch-readiness-toggle-confirm"
        onCancel={() => {
          if (!busy) {
            setToggleConfirm(null);
          }
        }}
        onConfirm={() => {
          if (!toggleConfirm || busy) {
            return;
          }
          const intent = toggleConfirm;
          setToggleConfirm(null);
          applyToggleConfirm(intent);
        }}
      />
    </section>
  );
}
