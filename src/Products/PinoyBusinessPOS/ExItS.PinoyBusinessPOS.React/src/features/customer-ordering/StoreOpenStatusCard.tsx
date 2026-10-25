import { useEffect, useState, type ReactNode } from "react";
import { PreferredInfoCard } from "@/features/customer-ordering/PreferredChoiceCard";
import {
  formatRemainingDuration,
  resolveStoreOpenStatus,
} from "@/features/customer-ordering/store-open-status";
import type { PickupHoursDay } from "@/features/customer-ordering/pickup-time-bounds";
import { useI18n } from "@/i18n/I18nProvider";

function remainingCaption(
  template: string,
  duration: string,
): ReactNode {
  const marker = "{duration}";
  const at = template.indexOf(marker);
  if (at < 0) {
    return (
      <>
        <span className="personal-preferred-period__time">{duration}</span>
        {` ${template}`}
      </>
    );
  }
  return (
    <>
      {template.slice(0, at)}
      <span className="personal-preferred-period__time">{duration}</span>
      {template.slice(at + marker.length)}
    </>
  );
}

/** Soft Glass open/closed status after Store Hours on Personal checkout. */
export function StoreOpenStatusCard({
  operatingHours,
  timeZoneId,
  testId = "checkout-store-open-status",
}: {
  operatingHours?: PickupHoursDay[] | null;
  timeZoneId?: string | null;
  testId?: string;
}) {
  const { t } = useI18n();
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const tick = () => setNow(new Date());
    tick();
    const id = window.setInterval(tick, 15_000);
    return () => window.clearInterval(id);
  }, []);

  const status = resolveStoreOpenStatus({
    now,
    timeZoneId,
    operatingHours,
  });

  if (status.kind === "unknown") {
    return null;
  }

  if (status.kind === "open24") {
    return (
      <PreferredInfoCard
        title={t("orders.storeStatusOpen")}
        value={
          <span className="personal-preferred-period__time">
            {t("orders.storeStatusOpen24Hours")}
          </span>
        }
        testId={testId}
        tone="success"
      />
    );
  }

  if (status.kind === "open") {
    return (
      <PreferredInfoCard
        title={t("orders.storeStatusOpen")}
        value={remainingCaption(
          t("orders.storeStatusRemainingToClose"),
          formatRemainingDuration(status.minutesUntilClose),
        )}
        testId={testId}
        tone="success"
      />
    );
  }

  return (
    <PreferredInfoCard
      title={t("orders.storeStatusClosed")}
      value={
        status.minutesUntilOpen != null
          ? remainingCaption(
              t("orders.storeStatusRemainingToOpen"),
              formatRemainingDuration(status.minutesUntilOpen),
            )
          : t("orders.storeStatusClosedNoOpen")
      }
      testId={testId}
      tone="danger"
    />
  );
}
