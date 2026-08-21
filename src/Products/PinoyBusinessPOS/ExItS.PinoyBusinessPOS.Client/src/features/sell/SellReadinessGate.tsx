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
 * Once Sell has been entered, mid-session device/shift loss stays on SellFloor
 * (compact cart warning) instead of remounting the pre-sell gate.
 *
 * Offline, the live device authorize and shift read cannot run, so the last-good readiness
 * snapshot (written while online and ready) reopens the warm session instead of demanding a
 * device registration or a shift open that both need the server.
 */
export function SellReadinessGate() {
  const { t } = useI18n();
  const { boundWorkspace, posDevice, sessionGrant } = useWorkspace();
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
    : evaluateSellEntryReadiness({ posDevice, shiftReadiness: readiness });
  const branchLabel = boundWorkspace?.branchName ?? t("devices.branchFallback");
  const [sellEntered, setSellEntered] = useState(false);

  useEffect(() => {
    if (entry.kind === "ready") {
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
    return (
      <div className="flex min-w-0 flex-col gap-4" data-testid="sell-readiness-device">
        <PageHeader
          title={t("sell.readiness.deviceTitle")}
          description={t("sell.readiness.deviceDetail").replace("{branch}", branchLabel)}
        />
        <Card className="flex flex-col gap-3 p-4">
          <div className="flex items-start gap-3">
            <MonitorSmartphone className="mt-0.5 size-6 shrink-0 text-primary" aria-hidden />
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("sell.readiness.deviceHelp")}
            </p>
          </div>
          {sellReadiness.online ? (
            <Button asChild className="min-h-11" data-testid="sell-readiness-register">
              <Link to="/devices/register?from=sell">{t("sell.readiness.registerBrowser")}</Link>
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
              <Link to="/org/devices">{t("devices.listTitle")}</Link>
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
          {sellReadiness.online ? (
            <Button asChild className="min-h-11" data-testid="sell-readiness-open-shift">
              <Link to="/shifts/open?from=sell">{t("sell.readiness.openShift")}</Link>
            </Button>
          ) : (
            <OnlineRequiredCard
              testId="sell-readiness-offline-required"
              code={ONLINE_REQUIRED_CODES.OpenShift}
            />
          )}
        </Card>
      </div>
    );
  }

  return <SellFloorPage />;
}
