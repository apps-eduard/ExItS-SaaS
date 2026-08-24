import { useState } from "react";
import { Link } from "react-router-dom";
import { CheckCircle2, MonitorSmartphone, ShoppingCart, X } from "lucide-react";
import type { PosCashierShiftDto } from "@/api/pos/pos-shifts-client";
import type { CheckoutShiftReadiness } from "@/features/shifts/checkout-readiness";
import type { MidSessionSellBlock } from "@/features/sell/sell-readiness";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type SellReadinessStripProps = {
  continuedOffline: boolean;
  offlineShiftNumber: string | null;
  midSessionBlock: MidSessionSellBlock["kind"];
  hasOpenShift: boolean;
  currentShift: PosCashierShiftDto | null;
  readiness: CheckoutShiftReadiness;
  variant?: "banner" | "inline";
};

export function SellReadinessStrip({
  continuedOffline,
  offlineShiftNumber,
  midSessionBlock,
  hasOpenShift,
  currentShift,
  readiness,
  variant = "banner",
}: SellReadinessStripProps) {
  const { t } = useI18n();
  const inline = variant === "inline";
  const [viewOnlyDismissed, setViewOnlyDismissed] = useState(false);

  if (continuedOffline) {
    return offlineShiftNumber ? (
      <p
        data-testid="sell-offline-shift-chip"
        className={cn(
          "sell-readiness-strip sell-readiness-strip--muted m-0 shrink-0 text-[length:var(--exits-text-xs)] text-muted",
          !inline && "sell-readiness-strip--banner sell-readiness-strip--banner-muted mb-3",
          inline && "sell-readiness-strip--inline",
        )}
      >
        {t("offline.shiftContinued").replace("{shift}", offlineShiftNumber)}
      </p>
    ) : null;
  }

  if (midSessionBlock === "device_lost" && !viewOnlyDismissed) {
    return (
      <div
        role="status"
        data-testid="sell-view-only-banner"
        className={cn(
          "sell-readiness-strip sell-readiness-strip--warning",
          inline
            ? "sell-readiness-strip--inline sell-readiness-strip--inline-warning flex min-w-0 flex-wrap items-center gap-2"
            : "sell-readiness-strip--banner sell-readiness-strip--banner-warning mb-3 flex shrink-0 flex-wrap items-center justify-between gap-3",
        )}
      >
        <p
          className={cn(
            "m-0 flex min-w-0 items-center gap-2 font-medium",
            inline ? "text-[length:var(--exits-text-xs)]" : "flex-1 text-[length:var(--exits-text-sm)]",
          )}
        >
          <MonitorSmartphone className="size-4 shrink-0" aria-hidden />
          <span>{inline ? t("sell.viewOnlyShort") : t("sell.viewOnlyBanner")}</span>
        </p>
        <div className="flex shrink-0 items-center gap-1.5">
          <Button
            asChild
            variant="default"
            className={cn("shrink-0", inline ? "min-h-8 px-2.5 text-[length:var(--exits-text-xs)]" : "min-h-10")}
            data-testid="sell-view-only-register"
          >
            <Link to="/devices/register?from=sell">{t("sell.readiness.registerDevice")}</Link>
          </Button>
          <button
            type="button"
            data-testid="sell-view-only-close"
            className="sell-readiness-strip__close"
            aria-label={t("sell.info.close")}
            onClick={() => setViewOnlyDismissed(true)}
          >
            <X className="size-4" aria-hidden />
          </button>
        </div>
      </div>
    );
  }

  if (midSessionBlock === "shift_lost" || (!hasOpenShift && midSessionBlock !== "none")) {
    return (
      <div
        data-testid="sell-shift-banner"
        className={cn(
          "sell-readiness-strip sell-readiness-strip--warning",
          inline
            ? "sell-readiness-strip--inline sell-readiness-strip--inline-warning inline-flex max-w-full flex-wrap items-center gap-2"
            : "sell-readiness-strip--banner sell-readiness-strip--banner-warning mb-3 flex max-w-full shrink-0 flex-wrap items-center justify-between gap-3",
        )}
      >
        <p className="m-0 flex min-w-0 flex-1 items-center gap-2 text-[length:var(--exits-text-sm)] font-medium">
          <ShoppingCart className="size-4 shrink-0" aria-hidden />
          <span>{inline ? t("sell.shiftClosedShort") : t("sell.shiftClosedBanner")}</span>
        </p>
        <Button
          asChild
          variant="default"
          className={cn("shrink-0", inline ? "min-h-8 px-2 text-[length:var(--exits-text-xs)]" : "min-h-10")}
          data-testid="sell-banner-open-shift"
        >
          <Link to="/shifts/open?from=sell">{t("shift.openTitle")}</Link>
        </Button>
      </div>
    );
  }

  if (hasOpenShift && currentShift) {
    const ready = readiness.moneyPostReady === true;
    if (!inline && ready) {
      return null;
    }

    const registerLabel = currentShift.registerCode
      ? `${currentShift.registerCode}${currentShift.registerName ? ` · ${currentShift.registerName}` : ""}`
      : t("shift.noRegisterOnShift");

    return (
      <p
        data-testid="sell-shift-chip"
        className={cn(
          "sell-readiness-strip m-0 flex min-w-0 flex-wrap items-center gap-1.5 text-[length:var(--exits-text-xs)]",
          !inline && "sell-readiness-strip--banner sell-readiness-strip--banner-ready mb-3 shrink-0",
          inline && "sell-readiness-strip--inline sell-readiness-strip--inline-ready",
          ready ? "text-[var(--exits-success)]" : "text-muted",
        )}
      >
        <CheckCircle2 className="size-3.5 shrink-0" aria-hidden />
        <span className="truncate">
          {inline
            ? `${currentShift.shiftNumber} · ${registerLabel}`
            : t("sell.shiftOpenBanner")
                .replace("{shift}", currentShift.shiftNumber)
                .replace("{register}", registerLabel)}
        </span>
      </p>
    );
  }

  return null;
}
