import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { CircleAlert, MonitorSmartphone } from "lucide-react";
import { getPosDeviceCapacity, registerPosDevice } from "@/api/platform/pos-devices-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  browserRegistrationMetadata,
  suggestFriendlyName,
} from "@/features/devices/device-presentation";
import { formatPosDeviceCapacity } from "@/features/devices/device-capacity";
import { getDurableInstallationDeviceId } from "@/workspace/browser-installation-identity";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";

/**
 * Direct self-registration of the current endpoint for POS sales.
 * No registration codes — Platform capacity + membership remain authoritative.
 */
export function DeviceRegisterPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const fromSell = searchParams.get("from") === "sell";
  const { boundWorkspace, workspaces, refreshPosDevice, sessionGrant } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const boundBranchId = boundWorkspace?.branchId ?? "";
  const branchLocked = Boolean(boundBranchId);
  const canManageDevices = hasOrganizationManagementAuthority(sessionGrant);
  const [deviceName, setDeviceName] = useState(() => suggestFriendlyName());
  const [branchId, setBranchId] = useState(boundBranchId);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (boundBranchId) {
      setBranchId(boundBranchId);
    }
  }, [boundBranchId]);

  const branches = useMemo(() => {
    if (!organizationId) {
      return [];
    }
    return workspaces.find((w) => w.organizationId === organizationId)?.branches ?? [];
  }, [organizationId, workspaces]);

  const boundBranchName =
    boundWorkspace?.branchName ??
    branches.find((b) => b.branchId === boundBranchId)?.name ??
    t("devices.branchFallback");

  const capacityQuery = useQuery({
    queryKey: ["platform-pos-devices-capacity", organizationId],
    enabled: Boolean(organizationId),
    queryFn: async () => {
      const result = await getPosDeviceCapacity(organizationId!);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("devices.capacityLoadError"));
      }
      return result.value;
    },
  });
  const capacity = formatPosDeviceCapacity(capacityQuery.data);
  const capacityBlocked = capacity?.kind === "finite" && capacity.atLimit;

  const registerMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId) {
        throw new Error(t("devices.noOrganization"));
      }
      const effectiveBranchId = branchLocked ? boundBranchId : branchId;
      if (!effectiveBranchId) {
        throw new Error(t("devices.branchRequired"));
      }
      if (capacityBlocked) {
        throw new Error(t("devices.capacity.limitReachedDetail"));
      }
      const identity = getDurableInstallationDeviceId();
      if (!identity.ok) {
        throw new Error(t("devices.identityUnavailable"));
      }
      const metadata = browserRegistrationMetadata();
      const result = await registerPosDevice(organizationId, {
        branchId: effectiveBranchId,
        installationDeviceId: identity.installationDeviceId,
        friendlyName: deviceName.trim() || suggestFriendlyName(),
        platform: metadata.platform,
        model: metadata.model,
        appVersion: metadata.appVersion,
      });
      if (!result.ok) {
        throw new PlatformApiError(result.status, result.body ?? {});
      }
      return { device: result.value, branchId: effectiveBranchId };
    },
    onSuccess: async ({ branchId: registeredBranchId }) => {
      setError(null);
      await refreshPosDevice({ branchId: registeredBranchId });
      navigate(fromSell || Boolean(boundWorkspace) ? "/sell" : "/", { replace: true });
    },
    onError: (err: unknown) => {
      setError(describePosApiError(err, t, "devices.registerError"));
    },
  });

  return (
    <div data-testid="device-register-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader
        title={t("devices.registerTitle")}
        description={t("devices.registerLede")}
        backTo={pageBackNav.orgDevices.to}
        backLabel={t(pageBackNav.orgDevices.labelKey)}
        backTestId="page-header-back-org"
      />

      <Card className="flex flex-col gap-3 p-4">
        <div className="flex items-start gap-3">
          <MonitorSmartphone className="mt-0.5 size-6 shrink-0 text-primary" aria-hidden />
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("devices.registerHint")}
          </p>
        </div>

        {capacity ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)]"
            data-testid="devices-register-capacity"
          >
            {t("devices.capacity.activeOfAllowed")
              .replace("{used}", capacity.used.toLocaleString())
              .replace("{allowed}", capacity.allowed.toLocaleString())}
          </p>
        ) : null}

        {capacityBlocked ? (
          <div
            role="alert"
            className="flex gap-3 rounded-[var(--exits-radius-md)] border border-destructive px-4 py-3"
            data-testid="devices-register-capacity-blocked"
          >
            <CircleAlert className="mt-0.5 size-5 shrink-0 text-destructive" aria-hidden />
            <div className="flex min-w-0 flex-col gap-1">
              <p className="m-0 font-semibold">{t("devices.capacity.limitReached")}</p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("devices.capacity.limitReachedDetail")}
              </p>
            </div>
          </div>
        ) : null}

        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("devices.deviceNameLabel")}
          <input
            className="min-h-11 rounded border border-[var(--exits-border)] bg-transparent px-3"
            data-testid="devices-name-input"
            value={deviceName}
            onChange={(event) => setDeviceName(event.target.value)}
            disabled={capacityBlocked}
          />
        </label>

        {branchLocked ? (
          <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="devices-branch-locked">
            {t("devices.branchLocked").replace("{branch}", boundBranchName)}
          </p>
        ) : (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("devices.branchLabel")}
            <select
              className="min-h-11 rounded border border-[var(--exits-border)] bg-transparent px-3"
              data-testid="devices-branch-select"
              value={branchId}
              onChange={(event) => setBranchId(event.target.value)}
              disabled={capacityBlocked}
            >
              <option value="">{t("devices.branchPlaceholder")}</option>
              {branches.map((branch) => (
                <option key={branch.branchId} value={branch.branchId}>
                  {branch.name}
                </option>
              ))}
            </select>
          </label>
        )}

        {error ? (
          <div
            role="alert"
            className="flex gap-3 rounded-[var(--exits-radius-md)] border border-destructive px-4 py-3"
            data-testid="devices-register-error"
          >
            <CircleAlert className="mt-0.5 size-5 shrink-0 text-destructive" aria-hidden />
            <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">{error}</p>
          </div>
        ) : null}

        <Button
          type="button"
          className="min-h-11"
          data-testid="devices-register-submit"
          disabled={registerMutation.isPending || capacityBlocked}
          onClick={() => registerMutation.mutate()}
        >
          {registerMutation.isPending ? t("devices.registering") : t("devices.registerThisDevice")}
        </Button>

        {canManageDevices ? (
          <Button asChild variant="ghost" className="min-h-11" data-testid="devices-open-manage">
            <Link to="/org/devices">{t("devices.manageDevices")}</Link>
          </Button>
        ) : null}
      </Card>
    </div>
  );
}
