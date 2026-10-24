import { useEffect, useState } from "react";
import { CalendarClock } from "lucide-react";
import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import type { MessageKey } from "@/i18n/messages";

type BranchScheduledOrdersSettingsPanelProps = {
  readiness: BranchFulfillmentReadinessDto;
  busy: boolean;
  canManage: boolean;
  t: (key: MessageKey) => string;
  onSave: (payload: {
    acceptScheduledOrdersWhileClosed: boolean;
    minPreparationLeadMinutes: number;
    maxAdvanceScheduleDays: number;
  }) => void | Promise<void>;
};

export function BranchScheduledOrdersSettingsPanel({
  readiness,
  busy,
  canManage,
  t,
  onSave,
}: BranchScheduledOrdersSettingsPanelProps) {
  const [acceptScheduled, setAcceptScheduled] = useState(readiness.acceptScheduledOrdersWhileClosed);
  const [leadMinutes, setLeadMinutes] = useState(String(readiness.minPreparationLeadMinutes));
  const [advanceDays, setAdvanceDays] = useState(String(readiness.maxAdvanceScheduleDays));

  useEffect(() => {
    setAcceptScheduled(readiness.acceptScheduledOrdersWhileClosed);
    setLeadMinutes(String(readiness.minPreparationLeadMinutes));
    setAdvanceDays(String(readiness.maxAdvanceScheduleDays));
  }, [
    readiness.acceptScheduledOrdersWhileClosed,
    readiness.minPreparationLeadMinutes,
    readiness.maxAdvanceScheduleDays,
  ]);

  const dirty =
    acceptScheduled !== readiness.acceptScheduledOrdersWhileClosed
    || Math.max(0, Math.trunc(Number(leadMinutes) || 0)) !== readiness.minPreparationLeadMinutes
    || Math.max(0, Math.trunc(Number(advanceDays) || 0)) !== readiness.maxAdvanceScheduleDays;

  return (
    <section
      className="branch-scheduled-orders-panel personal-qa-glass personal-qa-glass--info flex flex-col gap-3 p-4"
      data-testid="branch-scheduled-orders-settings"
      data-shell="soft-glass"
    >
      <div className="flex items-start gap-3">
        <span className="personal-qa-glass__icon" aria-hidden>
          <CalendarClock className="size-4" strokeWidth={1.75} />
        </span>
        <div className="min-w-0 flex-1">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.scheduledOrdersTitle")}
          </h3>
          <p className="m-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
            {t("branches.scheduledOrdersLede")}
          </p>
        </div>
      </div>

      <div className="flex items-center justify-between gap-3">
        <label className="min-w-0 flex-1 text-[length:var(--exits-text-sm)]" htmlFor="branch-accept-scheduled-closed">
          {t("branches.acceptScheduledWhileClosed")}
        </label>
        <Switch
          id="branch-accept-scheduled-closed"
          checked={acceptScheduled}
          disabled={!canManage || busy}
          data-testid="branch-accept-scheduled-closed"
          onCheckedChange={setAcceptScheduled}
        />
      </div>
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
        {t("branches.acceptScheduledWhileClosedHint")}
      </p>

      <div className="grid gap-3 sm:grid-cols-2">
        <label className="pc-field">
          <span className="pc-field__label">{t("branches.minPreparationLeadMinutes")}</span>
          <input
            className="pc-field__control"
            type="number"
            min={0}
            step={1}
            disabled={!canManage || busy}
            value={leadMinutes}
            data-testid="branch-min-prep-lead"
            onChange={(event) => setLeadMinutes(event.target.value)}
          />
        </label>
        <label className="pc-field">
          <span className="pc-field__label">{t("branches.maxAdvanceScheduleDays")}</span>
          <input
            className="pc-field__control"
            type="number"
            min={0}
            step={1}
            disabled={!canManage || busy}
            value={advanceDays}
            data-testid="branch-max-advance-days"
            onChange={(event) => setAdvanceDays(event.target.value)}
          />
        </label>
      </div>

      <Button
        type="button"
        intent="primary"
        appearance="solid"
        size="default"
        className="w-fit min-w-[8rem]"
        disabled={!canManage || busy || !dirty}
        data-testid="branch-save-scheduled-orders"
        onClick={() =>
          void onSave({
            acceptScheduledOrdersWhileClosed: acceptScheduled,
            minPreparationLeadMinutes: Math.max(0, Math.trunc(Number(leadMinutes) || 0)),
            maxAdvanceScheduleDays: Math.max(0, Math.trunc(Number(advanceDays) || 0)),
          })
        }
      >
        {t("branches.saveScheduledOrders")}
      </Button>
    </section>
  );
}
