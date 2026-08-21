import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import {
  createPosDeviceRegistrationToken,
  getPosDeviceCapacity,
  listPosDevices,
  registerPosDevice,
  revokePosDevice,
  type PosDeviceDto,
} from "@/api/platform/pos-devices-client";
import { getDurableInstallationDeviceId } from "@/workspace/browser-installation-identity";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatPosDeviceCapacity } from "@/features/devices/device-capacity";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgPosDevicesPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant, workspaces, posDevice, refreshPosDevice } = useWorkspace();
  const canManage = hasOrganizationManagementAuthority(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;
  const [deviceName, setDeviceName] = useState("This browser");
  const [registerBranchId, setRegisterBranchId] = useState(boundWorkspace?.branchId ?? "");
  const [actionError, setActionError] = useState<string | null>(null);
  const [createdToken, setCreatedToken] = useState<string | null>(null);
  const [revokeReason, setRevokeReason] = useState("");
  const [revokeTarget, setRevokeTarget] = useState<PosDeviceDto | null>(null);
  const [copiedDeviceId, setCopiedDeviceId] = useState<string | null>(null);

  const branches = useMemo(() => {
    if (!organizationId) {
      return [];
    }
    return workspaces.find((w) => w.organizationId === organizationId)?.branches ?? [];
  }, [organizationId, workspaces]);

  const branchNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const branch of branches) {
      map.set(branch.branchId, branch.name);
    }
    return map;
  }, [branches]);

  const devicesQuery = useQuery({
    queryKey: ["platform-pos-devices", organizationId],
    enabled: organizationId !== null && canManage,
    queryFn: async ({ signal }) => {
      const result = await listPosDevices(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("devices.loadError"));
      }
      return result.value;
    },
  });

  const capacityQuery = useQuery({
    queryKey: ["platform-pos-devices-capacity", organizationId],
    enabled: organizationId !== null && canManage,
    queryFn: async ({ signal }) => {
      const result = await getPosDeviceCapacity(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("devices.capacityLoadError"));
      }
      return result.value;
    },
  });

  const capacity = formatPosDeviceCapacity(capacityQuery.data);

  const registerMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId || !registerBranchId) {
        throw new Error(t("devices.branchRequired"));
      }
      const identity = getDurableInstallationDeviceId();
      if (!identity.ok) {
        throw new Error(t("devices.identityUnavailable"));
      }
      const result = await registerPosDevice(organizationId, {
        branchId: registerBranchId,
        installationDeviceId: identity.installationDeviceId,
        friendlyName: deviceName.trim() || t("devices.defaultBrowserName"),
        platform: "Browser",
      });
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("devices.registerError"));
      }
      return result.value;
    },
    onSuccess: async () => {
      setActionError(null);
      await queryClient.invalidateQueries({ queryKey: ["platform-pos-devices", organizationId] });
      await queryClient.invalidateQueries({
        queryKey: ["platform-pos-devices-capacity", organizationId],
      });
      await refreshPosDevice({ branchId: registerBranchId });
    },
    onError: (error: Error) => {
      setActionError(error.message);
    },
  });

  const tokenMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId) {
        throw new Error(t("devices.noOrganization"));
      }
      const result = await createPosDeviceRegistrationToken(organizationId);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("devices.tokenError"));
      }
      return result.value;
    },
    onSuccess: (token) => {
      setActionError(null);
      setCreatedToken(token.token);
    },
    onError: (error: Error) => {
      setActionError(error.message);
    },
  });

  const revokeMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId || !revokeTarget) {
        throw new Error(t("devices.revokeError"));
      }
      const reason = revokeReason.trim();
      if (!reason) {
        throw new Error(t("devices.revokeReasonRequired"));
      }
      const result = await revokePosDevice(organizationId, revokeTarget.id, { reason });
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("devices.revokeError"));
      }
      return result.value;
    },
    onSuccess: async () => {
      setActionError(null);
      setRevokeTarget(null);
      setRevokeReason("");
      await queryClient.invalidateQueries({ queryKey: ["platform-pos-devices", organizationId] });
      await queryClient.invalidateQueries({
        queryKey: ["platform-pos-devices-capacity", organizationId],
      });
      await refreshPosDevice();
    },
    onError: (error: Error) => {
      setActionError(error.message);
    },
  });

  async function copyInstallationId(installationDeviceId: string) {
    try {
      await navigator.clipboard.writeText(installationDeviceId);
      setCopiedDeviceId(installationDeviceId);
      window.setTimeout(() => {
        setCopiedDeviceId((current) => (current === installationDeviceId ? null : current));
      }, 2000);
    } catch {
      setActionError(t("devices.copyFailed"));
    }
  }

  if (!canManage) {
    return (
      <div data-testid="org-devices-denied" className="flex flex-col gap-3">
        <PageHeader title={t("devices.listTitle")} description={t("devices.deniedDetail")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/org">{t("devices.backOrg")}</Link>
        </Button>
      </div>
    );
  }

  return (
    <div data-testid="org-devices-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("devices.listTitle")} description={t("devices.listLede")} />

      {capacity ? (
        <Card data-testid="devices-capacity">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("devices.capacity.activeOfAllowed")
              .replace("{used}", capacity.used.toLocaleString())
              .replace("{allowed}", capacity.allowed.toLocaleString())}
          </p>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
            {t("devices.capacity.available").replace(
              "{available}",
              capacity.available.toLocaleString(),
            )}
          </p>
          <div
            className="mt-3 h-2 overflow-hidden rounded-full bg-[var(--exits-surface-muted)]"
            role="progressbar"
            aria-valuemin={0}
            aria-valuemax={capacity.allowed}
            aria-valuenow={capacity.used}
            data-testid="devices-capacity-bar"
          >
            <div
              className="h-full rounded-full bg-primary transition-[width]"
              style={{ width: `${Math.round(capacity.progressRatio * 100)}%` }}
            />
          </div>
          {capacity.atLimit ? (
            <div className="mt-3" data-testid="devices-capacity-limit">
              <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("devices.capacity.limitReached")}
              </p>
              <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                {t("devices.capacity.limitReachedDetail")}
              </p>
            </div>
          ) : null}
        </Card>
      ) : null}

      <Card data-testid="devices-this-browser">
        <p className="m-0 font-semibold">{t("devices.thisBrowserTitle")}</p>
        <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted">
          {posDevice.detail}
        </p>
        <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)]" data-testid="devices-status">
          {t("devices.statusLabel")}: {posDevice.registrationStatus}
        </p>
        <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("devices.deviceNameLabel")}
          <input
            className="min-h-11 rounded border border-[var(--exits-border)] bg-transparent px-3"
            data-testid="devices-name-input"
            value={deviceName}
            onChange={(event) => setDeviceName(event.target.value)}
          />
        </label>
        <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("devices.branchLabel")}
          <select
            className="min-h-11 rounded border border-[var(--exits-border)] bg-transparent px-3"
            data-testid="devices-branch-select"
            value={registerBranchId}
            onChange={(event) => setRegisterBranchId(event.target.value)}
          >
            <option value="">{t("devices.branchPlaceholder")}</option>
            {branches.map((branch) => (
              <option key={branch.branchId} value={branch.branchId}>
                {branch.name}
              </option>
            ))}
          </select>
        </label>
        <div className="mt-3 flex flex-wrap gap-2">
          <Button
            type="button"
            className="min-h-11"
            data-testid="devices-register-browser"
            disabled={
              registerMutation.isPending || (capacity?.kind === "finite" && capacity.atLimit)
            }
            onClick={() => registerMutation.mutate()}
          >
            {registerMutation.isPending ? t("devices.registering") : t("devices.registerBrowser")}
          </Button>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            data-testid="devices-create-code"
            disabled={tokenMutation.isPending}
            onClick={() => tokenMutation.mutate()}
          >
            {tokenMutation.isPending ? t("devices.creatingCode") : t("devices.createCode")}
          </Button>
        </div>
        {createdToken ? (
          <div className="mt-3" data-testid="devices-created-code">
            <p className="m-0 text-[length:var(--exits-text-sm)]">{t("devices.codeReady")}</p>
            <code className="mt-2 block break-all rounded bg-[var(--exits-surface-muted)] p-2 text-[length:var(--exits-text-sm)]">
              {createdToken}
            </code>
          </div>
        ) : null}
      </Card>

      {actionError ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          data-testid="devices-action-error"
        >
          {actionError}
        </p>
      ) : null}

      {devicesQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
      {devicesQuery.isError ? (
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {t("devices.loadError")}
          </p>
        </Card>
      ) : null}

      <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="devices-list">
        {(devicesQuery.data ?? []).map((device) => (
          <li key={device.id}>
            <Card data-testid={`device-row-${device.id}`}>
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div className="min-w-0">
                  <p className="m-0 font-semibold">{device.friendlyName}</p>
                  <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {branchNameById.get(device.branchId) ?? t("devices.branchFallback")}
                  </p>
                </div>
                <StatusChip tone={device.status === "Active" ? "success" : "info"}>
                  {device.status}
                </StatusChip>
              </div>
              <details className="mt-2 text-[length:var(--exits-text-xs)] text-muted">
                <summary className="cursor-pointer select-none">
                  {t("devices.installationIdDetails")}
                </summary>
                <div className="mt-2 flex flex-wrap items-center gap-2">
                  <code className="break-all rounded bg-[var(--exits-surface-muted)] px-2 py-1">
                    {device.installationDeviceId}
                  </code>
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11"
                    data-testid={`device-copy-id-${device.id}`}
                    onClick={() => void copyInstallationId(device.installationDeviceId)}
                  >
                    {copiedDeviceId === device.installationDeviceId
                      ? t("devices.copied")
                      : t("devices.copyInstallationId")}
                  </Button>
                </div>
              </details>
              {device.status === "Active" ? (
                <Button
                  type="button"
                  variant="ghost"
                  className="mt-2 min-h-11"
                  data-testid={`device-revoke-${device.id}`}
                  onClick={() => {
                    setRevokeTarget(device);
                    setRevokeReason("");
                  }}
                >
                  {t("devices.revoke")}
                </Button>
              ) : null}
            </Card>
          </li>
        ))}
      </ul>

      {(devicesQuery.data?.length ?? 0) === 0 && !devicesQuery.isLoading ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="devices-empty"
        >
          {t("devices.empty")}
        </p>
      ) : null}

      {revokeTarget ? (
        <Card data-testid="devices-revoke-panel">
          <p className="m-0 font-semibold">{t("devices.revokeTitle")}</p>
          <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted">
            {revokeTarget.friendlyName}
          </p>
          <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("devices.revokeReasonLabel")}
            <input
              className="min-h-11 rounded border border-[var(--exits-border)] bg-transparent px-3"
              data-testid="devices-revoke-reason"
              value={revokeReason}
              onChange={(event) => setRevokeReason(event.target.value)}
            />
          </label>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button
              type="button"
              className="min-h-11"
              data-testid="devices-revoke-confirm"
              disabled={revokeMutation.isPending}
              onClick={() => revokeMutation.mutate()}
            >
              {revokeMutation.isPending ? t("devices.revoking") : t("devices.revokeConfirm")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              onClick={() => setRevokeTarget(null)}
            >
              {t("devices.cancel")}
            </Button>
          </div>
        </Card>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/devices/register">{t("devices.openRedeem")}</Link>
        </Button>
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/org">{t("devices.backOrg")}</Link>
        </Button>
      </div>
    </div>
  );
}
