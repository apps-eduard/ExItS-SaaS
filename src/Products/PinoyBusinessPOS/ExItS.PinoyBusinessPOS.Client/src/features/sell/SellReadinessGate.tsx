import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { MonitorSmartphone, ShoppingCart } from "lucide-react";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { OnlineRequiredCard } from "@/components/exits/OnlineRequiredCard";
import { PageHeader } from "@/components/exits/PageHeader";
import { ONLINE_REQUIRED_CODES } from "@/offline/online-required";
import { SellFloorPage } from "@/features/sell/SellFloorPage";
import {
  evaluateSellEntryReadiness,
  type SellEntryReadiness,
} from "@/features/sell/sell-readiness";
import { useSellOfflineReadiness } from "@/features/sell/use-sell-offline-readiness";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Device → Shift → Sell gate. Does not replace server money-post authority.
 * Unregistered endpoints may browse Sell in view-only mode; execution stays blocked.
 */
export function SellReadinessGate() {
  const { t } = useI18n();
  const { boundWorkspace, posDevice, sessionGrant, deviceEnforcementEnabled } = useWorkspace();
  const { readiness } = useShiftContext();
  const sellReadiness = useSellOfflineReadiness();
  const canManageDevices = hasOrganizationManagementAuthority(sessionGrant);
  const entry: SellEntryReadiness = sellReadiness.fromSnapshot
    ? {
        kind: "ready",
        deviceReady: true,
        shiftReady: true,
        moneyPostReady: sellReadiness.moneyPostReady,
      }
    : evaluateSellEntryReadiness({
        posDevice,
        shiftReadiness: readiness,
        allowViewOnlyWithoutDevice: true,
        deviceEnforcementEnabled,
      });
  const branchLabel = boundWorkspace?.branchName ?? t("devices.branchFallback");
  const [sellEntered, setSellEntered] = useState(false);

  useEffect(() => {
    if (entry.kind === "ready" || entry.kind === "view_only") {
      setSellEntered(true);
    }
  }, [entry.kind]);

  if (sellEntered) {
    return <SellFloorPage />;
  }

  if (entry.kind === "loading") {
    return <LoadingSkeleton label={t("sell.readiness.loading")} />;
  }

  if (entry.kind === "device_required") {
    const revoked = posDevice.registrationStatus === "revoked";
    return (
      <div
        className="flex min-w-0 flex-col gap-4"
        data-testid="sell-readiness-device"
        data-device-state={revoked ? "revoked" : "unregistered"}
      >
        <PageHeader
          title={revoked ? t("sell.readiness.deviceRevokedTitle") : t("sell.readiness.deviceTitle")}
          description={(revoked
            ? t("sell.readiness.deviceRevokedDetail")
            : t("sell.readiness.deviceDetail")
          ).replace("{branch}", branchLabel)}
        />
        <Card className="flex flex-col gap-3 p-4">
          <div className="flex items-start gap-3">
            <MonitorSmartphone className="mt-0.5 size-6 shrink-0 text-primary" aria-hidden />
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="sell-readiness-device-help"
            >
              {revoked ? t("sell.readiness.deviceRevokedHelp") : t("sell.readiness.deviceHelp")}
            </p>
          </div>
          {sellReadiness.online ? (
            <Button asChild className="min-h-11" data-testid="sell-readiness-register">
              <Link to="/devices/register?from=sell">{t("sell.readiness.registerDevice")}</Link>
            </Button>
          ) : (
            <OnlineRequiredCard
              testId="sell-readiness-offline-required"
              code={ONLINE_REQUIRED_CODES.DeviceRegister}
            />
          )}
          {canManageDevices && sellReadiness.online ? (
            <Button
              asChild
              variant="ghost"
              className="min-h-11"
              data-testid="sell-readiness-manage-devices"
            >
              <Link to="/org/devices">{t("sell.readiness.manageDevices")}</Link>
            </Button>
          ) : null}
        </Card>
      </div>
    );
  }

  if (entry.kind === "shift_required") {
    return (
      <div className="flex min-w-0 flex-col gap-4" data-testid="sell-readiness-shift">
        <PageHeader
          title={t("sell.readiness.shiftTitle")}
          description={t("sell.readiness.shiftDetail")}
        />
        <Card className="flex flex-col gap-3 p-4">
          <div className="flex items-start gap-3">
            <ShoppingCart className="mt-0.5 size-6 shrink-0 text-primary" aria-hidden />
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("sell.readiness.shiftHelp")}
            </p>
          </div>
          <Button asChild className="min-h-11" data-testid="sell-readiness-open-shift">
            <Link to="/shifts">{t("sell.readiness.openShift")}</Link>
          </Button>
        </Card>
      </div>
    );
  }

  return <SellFloorPage />;
}
