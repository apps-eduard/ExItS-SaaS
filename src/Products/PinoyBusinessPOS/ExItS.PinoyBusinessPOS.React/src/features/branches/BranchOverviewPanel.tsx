import { CircleAlert, CircleCheck } from "lucide-react";
import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { StatusChip } from "@/components/exits/StatusChip";
import { BranchFulfillmentSwitch } from "@/features/branches/BranchFulfillmentSwitch";
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
import { Button } from "@/components/ui/button";
import type { MessageKey } from "@/i18n/messages";

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

function enablementChannelKey(kind: "ordering" | "pickup" | "delivery"): MessageKey {
  if (kind === "ordering") return "branches.channel.ordering";
  if (kind === "pickup") return "branches.channel.pickup";
  return "branches.channel.delivery";
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

      <div className="branch-overview-progress" data-testid="branch-setup-progress">
        <div className="branch-overview-progress__item" data-testid="pickup-progress">
          <p className="branch-overview-progress__label m-0">
            {t("branches.channel.pickup")}
          </p>
          <p className="branch-overview-progress__value m-0">
            {t("branches.progress.of").replace(
              "{complete}",
              String(readiness.pickupSectionsComplete),
            ).replace("{total}", String(readiness.pickupSectionsTotal))}
          </p>
        </div>
        <div className="branch-overview-progress__item" data-testid="delivery-progress">
          <p className="branch-overview-progress__label m-0">
            {t("branches.channel.delivery")}
          </p>
          <p className="branch-overview-progress__value m-0">
            {t("branches.progress.of").replace(
              "{complete}",
              String(readiness.deliverySectionsComplete),
            ).replace("{total}", String(readiness.deliverySectionsTotal))}
          </p>
        </div>
      </div>

      <div className="branch-readiness__channels" role="list">
        {(
          [
            { kind: "ordering" as const, label: orderingLabel, testId: "ordering-status" },
            { kind: "pickup" as const, label: pickupLabel, testId: "pickup-status" },
            { kind: "delivery" as const, label: deliveryLabel, testId: "delivery-status" },
          ] as const
        ).map((channel) => (
          <div
            key={channel.kind}
            className="branch-readiness__channel"
            role="listitem"
            data-testid={channel.testId}
          >
            <span className="branch-readiness__channel-label">
              {t(enablementChannelKey(channel.kind))}
            </span>
            <StatusChip tone={enablementTone(channel.label)}>
              {t(enablementStatusWord(channel.label))}
            </StatusChip>
          </div>
        ))}
      </div>

      <div className="branch-overview-toggles" data-testid="branch-fulfillment-toggles">
        <BranchFulfillmentSwitch
          checked={pickup.checked}
          disabled={pickup.disabled}
          pending={busy}
          label={t("branches.channel.pickup")}
          hint={pickup.hintKey ? t(pickup.hintKey) : null}
          testId="overview-pickup-switch"
          onCheckedChange={(next) => {
            if (next && pickup.enableBlocked) return;
            onTogglePickup(next);
          }}
        />
        <BranchFulfillmentSwitch
          checked={delivery.checked}
          disabled={delivery.disabled}
          pending={busy}
          label={t("branches.channel.delivery")}
          hint={delivery.hintKey ? t(delivery.hintKey) : null}
          testId="overview-delivery-switch"
          onCheckedChange={(next) => {
            if (next && delivery.enableBlocked) return;
            onToggleDelivery(next);
          }}
        />
      </div>

      <div className="flex flex-wrap gap-2">
        {readiness.canUseCustomerOrdering &&
        !readiness.customerOrderingEnabled &&
        readiness.customerOrderingReady ? (
          <Button
            type="button"
            className="min-h-11"
            disabled={busy}
            onClick={onEnableOrdering}
            data-testid="enable-ordering"
          >
            {t("branches.enableOrdering")}
          </Button>
        ) : null}
        {readiness.customerOrderingEnabled && !readiness.onlineOrdersPaused ? (
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            disabled={busy}
            onClick={onPauseOrders}
            data-testid="pause-ordering"
          >
            {t("branches.pauseOrders")}
          </Button>
        ) : null}
        {readiness.onlineOrdersPaused ? (
          <Button
            type="button"
            className="min-h-11"
            disabled={busy}
            onClick={onResumeOrders}
            data-testid="resume-ordering"
          >
            {t("branches.resumeOrders")}
          </Button>
        ) : null}
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
