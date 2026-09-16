import {
  CircleAlert,
  CircleCheck,
  Package,
  Store,
  Truck,
  type LucideIcon,
} from "lucide-react";
import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { StatusChip } from "@/components/exits/StatusChip";
import { Switch } from "@/components/ui/switch";
import {
  deliveryEnablementLabel,
  filterRedundantReasonCodes,
  missingRequirementMessageKey,
  orderingEnablementLabel,
  pickupEnablementLabel,
  reasonCodeMessageKey,
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
  const orderingLabel = orderingEnablementLabel(readiness);
  const pickupLabel = pickupEnablementLabel(readiness);
  const deliveryLabel = deliveryEnablementLabel(readiness);
  const missingRequirements = readiness.missingRequirements;
  const extraReasonCodes = filterRedundantReasonCodes(
    missingRequirements,
    readiness.reasonCodes,
  );
  const setupComplete = missingRequirements.length === 0;

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
            hint={pickup.hintKey ? t(pickup.hintKey) : null}
            t={t}
            onCheckedChange={(next) => {
              if (next && pickup.enableBlocked) return;
              onTogglePickup(next);
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
            hint={delivery.hintKey ? t(delivery.hintKey) : null}
            t={t}
            onCheckedChange={(next) => {
              if (next && delivery.enableBlocked) return;
              onToggleDelivery(next);
            }}
          />
          <ChannelCard
            testId="ordering-progress"
            statusTestId="ordering-status"
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
                  onResumeOrders();
                  return;
                }
                if (!readiness.customerOrderingEnabled) {
                  onEnableOrdering();
                }
                return;
              }
              if (readiness.customerOrderingEnabled && !readiness.onlineOrdersPaused) {
                onPauseOrders();
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
        <div className="branch-readiness__ready" data-testid="branch-missing-none" role="status">
          <CircleCheck className="branch-readiness__ready-icon" aria-hidden />
          <p className="m-0 text-[length:var(--exits-text-sm)]">{t("branches.missingNone")}</p>
        </div>
      )}

      {extraReasonCodes.length > 0 ? (
        <div
          className="branch-readiness__checklist branch-readiness__checklist--secondary"
          data-testid="branch-reason-codes"
        >
          <p className="branch-readiness__checklist-title m-0">
            {t("branches.enablementGapsTitle")}
          </p>
          <ul className="branch-readiness__items m-0 list-none p-0">
            {extraReasonCodes.map((code) => (
              <li key={code} className="branch-readiness__item branch-readiness__item--muted">
                <span className="branch-readiness__item-dot" aria-hidden />
                <span>{t(reasonCodeMessageKey(code))}</span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </section>
  );
}
