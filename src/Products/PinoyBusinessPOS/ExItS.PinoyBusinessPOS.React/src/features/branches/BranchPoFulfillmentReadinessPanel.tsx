import { CircleAlert, CircleCheck } from "lucide-react";
import { Link } from "react-router-dom";
import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { Notice } from "@/components/exits/Notice";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import {
  buildPoFulfillmentReadinessView,
  buildSupplierReadinessSummary,
  type PoFulfillmentMethodLine,
  type SupplierReadinessSummaryItem,
} from "@/features/branches/po-fulfillment-readiness";
import { branchFulfillmentEditPath } from "@/features/branches/branch-setup-tabs";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

export type BranchPoFulfillmentReadinessPanelProps = {
  branchId: string;
  readiness: BranchFulfillmentReadinessDto;
  t: (key: MessageKey) => string;
  /** Supplier summary flags — null means unknown / treat as needs attention. */
  catalogOk?: boolean | null;
  paymentsOk?: boolean | null;
  contactOk?: boolean | null;
};

function channelTone(method: PoFulfillmentMethodLine): "neutral" | "success" | "warning" {
  if (!method.enabled) return "neutral";
  return method.ready ? "success" : "warning";
}

function channelStatusLabel(
  method: PoFulfillmentMethodLine,
  t: (key: MessageKey) => string,
): string {
  if (!method.enabled) return t("branches.poFulfillment.methodOff");
  if (method.ready) return t("branches.poFulfillment.methodReady");
  return t("branches.poFulfillment.methodSetup");
}

function ChannelStatusChip({
  method,
  t,
}: {
  method: PoFulfillmentMethodLine;
  t: (key: MessageKey) => string;
}) {
  const channelLabel = t(
    method.channel === "pickup" ? "branches.channel.pickup" : "branches.channel.delivery",
  );
  return (
    <StatusChip
      tone={channelTone(method)}
      shape="soft"
      appearance="outline"
      className={cn(!method.enabled && "opacity-80")}
      data-testid={`branch-po-fulfillment-method-${method.channel}`}
      data-enabled={method.enabled ? "true" : "false"}
    >
      {channelLabel} · {channelStatusLabel(method, t)} · {method.complete}/{method.total}
    </StatusChip>
  );
}

function SummaryChip({
  item,
  t,
}: {
  item: SupplierReadinessSummaryItem;
  t: (key: MessageKey) => string;
}) {
  return (
    <Link
      to={item.href}
      className="branch-po-supplier-summary__chip no-underline"
      data-testid={`po-supplier-summary-${item.key}`}
    >
      <span className="branch-po-supplier-summary__label">{t(item.labelKey)}</span>
      <StatusChip
        tone={item.ok ? "success" : "warning"}
        shape="soft"
        appearance="outline"
        icon={
          item.ok ? (
            <CircleCheck className="size-3.5" aria-hidden />
          ) : (
            <CircleAlert className="size-3.5" aria-hidden />
          )
        }
      >
        {item.ok
          ? t("branches.poFulfillment.methodReady")
          : t("branches.poFulfillment.methodSetup")}
      </StatusChip>
    </Link>
  );
}

/**
 * Purchase Order Fulfillment readiness — sits above Pickup/Delivery configuration.
 */
export function BranchPoFulfillmentReadinessPanel({
  branchId,
  readiness,
  t,
  catalogOk = null,
  paymentsOk = null,
  contactOk = null,
}: BranchPoFulfillmentReadinessPanelProps) {
  const view = buildPoFulfillmentReadinessView(readiness);
  const summary = buildSupplierReadinessSummary({
    branchId,
    fulfillmentReady: view.ready,
    catalogOk,
    paymentsOk,
    contactOk,
  });

  return (
    <section
      id="po-fulfillment-readiness"
      className="catalog-form-section exits-animate-panel branch-po-fulfillment gap-3"
      data-testid="branch-po-fulfillment-panel"
      data-ready={view.ready ? "true" : "false"}
    >
      <div className="branch-po-fulfillment__header flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0 flex-1">
          <h2 className="catalog-form-section__title m-0">
            {t("branches.poFulfillment.title")}
          </h2>
          {!view.noMethodEnabled ? (
            <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
              {t(view.ledeKey)}
            </p>
          ) : null}
        </div>
        <StatusChip
          tone={view.ready ? "success" : "warning"}
          appearance="outline"
          data-testid="branch-po-fulfillment-status"
        >
          {t(view.statusKey)}
        </StatusChip>
      </div>

      {view.noMethodEnabled ? (
        <Notice
          tone="info"
          className="w-full shrink-0"
          title={t("branches.poFulfillment.whyRequiredTitle")}
          testId="branch-po-fulfillment-why-required"
        >
          {t("branches.poFulfillment.whyRequiredBody")}
        </Notice>
      ) : null}

      {view.ready ? (
        <div
          className="branch-po-fulfillment__channels flex flex-wrap gap-2"
          data-testid="branch-po-fulfillment-methods"
        >
          {view.methods.map((method) => (
            <ChannelStatusChip key={method.channel} method={method} t={t} />
          ))}
        </div>
      ) : (
        <>
          {view.checklist.length > 0 ? (
            <ul
              className="branch-po-fulfillment__checklist m-0 flex list-none flex-col gap-1.5 p-0"
              data-testid="branch-po-fulfillment-checklist"
            >
              {view.checklist.map((item) => (
                <li
                  key={item.id}
                  className="branch-po-fulfillment__item flex items-start gap-2 text-[length:var(--exits-text-sm)]"
                  data-testid={`branch-po-fulfillment-item-${item.id}`}
                  data-done={item.done ? "true" : "false"}
                >
                  {item.done ? (
                    <CircleCheck
                      className="mt-0.5 size-4 shrink-0 text-[color:var(--exits-success)]"
                      aria-hidden
                    />
                  ) : (
                    <CircleAlert
                      className="mt-0.5 size-4 shrink-0 text-[color:var(--exits-warning)]"
                      aria-hidden
                    />
                  )}
                  <span className="min-w-0">
                    {t(item.labelKey)}
                    {item.progressLabel ? (
                      <span className="text-muted"> · {item.progressLabel}</span>
                    ) : null}
                  </span>
                </li>
              ))}
            </ul>
          ) : null}

          {!readiness.branchDetailsComplete ? (
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="outline"
                asChild
                data-testid="branch-po-fulfillment-open-details"
              >
                <Link to={branchFulfillmentEditPath(branchId, "details")}>
                  {t("branches.poFulfillment.openDetails")}
                </Link>
              </Button>
            </div>
          ) : null}
        </>
      )}

      <div
        className="branch-po-supplier-summary flex flex-col gap-2 border-t border-border pt-3"
        data-testid="branch-po-supplier-summary"
      >
        <div className="flex flex-wrap items-center justify-between gap-2">
          <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
            {t("branches.poFulfillment.summaryTitle")}
          </p>
          <Link
            to="/customers?kind=businesses"
            className="text-[length:var(--exits-text-sm)] font-medium underline underline-offset-2"
            data-testid="branch-po-supplier-summary-view"
          >
            {t("branches.poFulfillment.viewSupplierReadiness")}
          </Link>
        </div>
        <div className="branch-po-supplier-summary__chips">
          {summary.map((item) => (
            <SummaryChip key={item.key} item={item} t={t} />
          ))}
        </div>
      </div>
    </section>
  );
}
