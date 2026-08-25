import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  CircleAlert,
  Eye,
  EyeOff,
  Gauge,
  Monitor,
  MonitorSmartphone,
  Plus,
  ShieldAlert,
  Smartphone,
  Tablet,
  type LucideIcon,
} from "lucide-react";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import {
  issuePosDeviceRevokeStepUp,
  type GovernanceStepUpFailureReason,
} from "@/api/platform/governance-step-up-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { getPlatformCredentialStatus } from "@/api/platform/platform-credentials-client";
import {
  getPosDeviceCapacity,
  listPosDevices,
  registerPosDevice,
  revokePosDevice,
  type PosDeviceDto,
} from "@/api/platform/pos-devices-client";
import {
  getDurableInstallationDeviceId,
  peekDurableInstallationDeviceId,
} from "@/workspace/browser-installation-identity";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatPosDeviceCapacity } from "@/features/devices/device-capacity";
import {
  browserRegistrationMetadata,
  deviceIconKind,
  formatDeviceModelLine,
  formatRelativeOrDate,
  isCurrentDevice,
  resolveCurrentBrowserState,
  suggestFriendlyName,
  type DeviceIconKind,
} from "@/features/devices/device-presentation";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const MIN_REVOKE_REASON_LENGTH = 8;

const DEVICE_ICONS: Record<DeviceIconKind, LucideIcon> = {
  phone: Smartphone,
  tablet: Tablet,
  desktop: Monitor,
  browser: MonitorSmartphone,
};

export function OrgPosDevicesPage() {
  const { t } = useI18n();
  const { preferences } = usePreferences();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant, workspaces, posDevice, refreshPosDevice, deviceEnforcementEnabled } =
    useWorkspace();
  const canManage = hasOrganizationManagementAuthority(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;

  const [deviceName, setDeviceName] = useState(() => suggestFriendlyName());
  const [registerBranchId, setRegisterBranchId] = useState(boundWorkspace?.branchId ?? "");
  const [registerFormOpen, setRegisterFormOpen] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [revokeTarget, setRevokeTarget] = useState<PosDeviceDto | null>(null);
  const [revokeReason, setRevokeReason] = useState("");
  const [revokePassword, setRevokePassword] = useState("");
  const [revokePasswordVisible, setRevokePasswordVisible] = useState(false);
  const [revokeError, setRevokeError] = useState<string | null>(null);
  const [revokedCurrentDevice, setRevokedCurrentDevice] = useState(false);

  const localInstallationId = useMemo(
    () => posDevice.installationDeviceId ?? peekDurableInstallationDeviceId(),
    [posDevice.installationDeviceId],
  );

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
  const capacityBlocked = capacity?.kind === "finite" && capacity.atLimit;
  const devices = useMemo(
    () => (devicesQuery.data ?? []).filter((device) => device.status === "Active"),
    [devicesQuery.data],
  );

  const currentBrowser = useMemo(
    () =>
      resolveCurrentBrowserState({
        devices,
        localInstallationId,
        registrationStatus: posDevice.registrationStatus,
      }),
    [devices, localInstallationId, posDevice.registrationStatus],
  );

  const showRegisterForm = currentBrowser.state === "unregistered" || registerFormOpen;

  function formatTimestamp(value: string | null | undefined): string | null {
    return formatRelativeOrDate(value, new Date(), preferences.locale);
  }

  function stepUpMessage(reason: GovernanceStepUpFailureReason): string {
    switch (reason) {
      case "password_required":
        return t("devices.revoke.passwordRequired");
      case "wrong_password":
        return t("devices.revoke.wrongPassword");
      case "expired":
        return t("devices.revoke.expired");
      case "consumed":
        return t("devices.revoke.consumed");
      case "invalid_scope":
        return t("devices.revoke.invalidScope");
      case "not_allowed":
        return t("devices.revoke.notAllowed");
      default:
        return t("devices.revoke.unavailable");
    }
  }

  const registerMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId || !registerBranchId) {
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
        branchId: registerBranchId,
        installationDeviceId: identity.installationDeviceId,
        friendlyName: deviceName.trim() || t("devices.defaultBrowserName"),
        platform: metadata.platform,
        model: metadata.model,
        appVersion: metadata.appVersion,
      });
      if (!result.ok) {
        throw new PlatformApiError(result.status, result.body ?? {});
      }
      return result.value;
    },
    onSuccess: async () => {
      setActionError(null);
      setRevokedCurrentDevice(false);
      setRegisterFormOpen(false);
      await queryClient.invalidateQueries({ queryKey: ["platform-pos-devices", organizationId] });
      await queryClient.invalidateQueries({
        queryKey: ["platform-pos-devices-capacity", organizationId],
      });
      await refreshPosDevice({ branchId: registerBranchId });
    },
    onError: (error: unknown) => {
      setActionError(describePosApiError(error, t, "devices.registerError"));
    },
  });

  const revokeMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId || !revokeTarget) {
        throw new Error(t("devices.revokeError"));
      }

      const reason = revokeReason.trim();
      if (reason.length < MIN_REVOKE_REASON_LENGTH) {
        throw new Error(t("devices.revoke.reasonTooShort"));
      }
      if (!revokePassword.trim()) {
        throw new Error(t("devices.revoke.passwordRequired"));
      }

      const credential = await getPlatformCredentialStatus();
      if (!credential.ok) {
        throw new Error(t("devices.revoke.credentialCheckFailed"));
      }
      if (!credential.value.hasPassword) {
        // No self-service password bootstrap exists in this client — stay honest and stop here.
        throw new Error(t("devices.revoke.noPassword"));
      }

      const stepUp = await issuePosDeviceRevokeStepUp(
        organizationId,
        revokeTarget.id,
        revokePassword,
      );
      if (!stepUp.ok) {
        throw new Error(stepUpMessage(stepUp.reason));
      }

      const wasCurrentDevice = isCurrentDevice(revokeTarget, localInstallationId);
      const result = await revokePosDevice(organizationId, revokeTarget.id, {
        reason,
        stepUpToken: stepUp.value.stepUpToken,
      });
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("devices.revokeError"));
      }
      return { wasCurrentDevice };
    },
    onSuccess: async ({ wasCurrentDevice }) => {
      setActionError(null);
      setRevokeError(null);
      setRevokeTarget(null);
      setRevokeReason("");
      // Revoking never re-registers: the owner must ask for this browser explicitly.
      setRevokedCurrentDevice(wasCurrentDevice);
      setRegisterFormOpen(false);
      await queryClient.invalidateQueries({ queryKey: ["platform-pos-devices", organizationId] });
      await queryClient.invalidateQueries({
        queryKey: ["platform-pos-devices-capacity", organizationId],
      });
      await refreshPosDevice();
    },
    onError: (error: Error) => {
      setRevokeError(error.message);
    },
    onSettled: () => {
      setRevokePassword("");
      setRevokePasswordVisible(false);
    },
  });

  function openRevoke(device: PosDeviceDto) {
    setRevokeTarget(device);
    setRevokeReason("");
    setRevokePassword("");
    setRevokePasswordVisible(false);
    setRevokeError(null);
  }

  function closeRevoke() {
    setRevokeTarget(null);
    setRevokeReason("");
    setRevokePassword("");
    setRevokePasswordVisible(false);
    setRevokeError(null);
  }

  function openRegisterForm(branchId: string | null) {
    setRegisterFormOpen(true);
    setActionError(null);
    if (branchId && !registerBranchId) {
      setRegisterBranchId(branchId);
    }
  }

  if (!canManage) {
    return (
      <div data-testid="org-devices-denied" className="flex flex-col gap-3">
        <PageHeader
          title={t("devices.listTitle")}
          description={t("devices.deniedDetail")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
      </div>
    );
  }

  const revokeTargetIsCurrent = revokeTarget
    ? isCurrentDevice(revokeTarget, localInstallationId)
    : false;
  const revokeReasonTooShort = revokeReason.trim().length < MIN_REVOKE_REASON_LENGTH;

  return (
    <div
      data-testid="org-devices-page"
      className="devices-page exits-page flex min-w-0 flex-col gap-3"
    >
      <PageHeader
        title={t("devices.listTitle")}
        description={t("devices.listLede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />

      {deviceEnforcementEnabled === false ? (
        <p
          className="m-0 rounded-md border border-border bg-muted/40 px-3 py-2 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="devices-enforcement-paused-hint"
        >
          {t("devices.enforcementPausedHint")}
        </p>
      ) : null}

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("devices.listTitle")}
        testId="devices-toolbar"
        className="exits-animate-toolbar"
        items={[
          {
            key: "register",
            label: t("devices.registerThisDevice"),
            icon: <Plus />,
            href: "/devices/register",
            testId: "devices-open-register",
            emphasis: "primary",
          },
        ]}
      />

      {capacity ? (
        <section
          className="catalog-form-section exits-animate-panel devices-capacity"
          data-testid="devices-capacity"
        >
          <div className="flex items-start gap-2.5">
            <span className="devices-panel__icon" aria-hidden>
              <Gauge />
            </span>
            <div className="min-w-0 flex-1">
              <h2 className="catalog-form-section__title">
                {t("devices.capacity.activeOfAllowed")
                  .replace("{used}", capacity.used.toLocaleString())
                  .replace("{allowed}", capacity.allowed.toLocaleString())}
              </h2>
              <p className="mb-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                {t("devices.capacity.available").replace(
                  "{available}",
                  capacity.available.toLocaleString(),
                )}
              </p>
            </div>
          </div>
          <div
            className="devices-capacity__bar"
            role="progressbar"
            aria-valuemin={0}
            aria-valuemax={capacity.allowed}
            aria-valuenow={capacity.used}
            data-testid="devices-capacity-bar"
          >
            <div
              className="devices-capacity__bar-fill"
              style={{ width: `${Math.round(capacity.progressRatio * 100)}%` }}
            />
          </div>
          {capacity.atLimit ? (
            <div className="mt-1" data-testid="devices-capacity-limit">
              <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("devices.capacity.limitReached")}
              </p>
              <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                {t("devices.capacity.limitReachedDetail")}
              </p>
            </div>
          ) : null}
        </section>
      ) : null}

      <section
        className="catalog-form-section exits-animate-panel"
        data-testid="devices-this-browser"
        data-state={currentBrowser.state}
      >
        <div className="flex items-start gap-2.5">
          <span className="devices-panel__icon" aria-hidden>
            <MonitorSmartphone />
          </span>
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="catalog-form-section__title">
                {currentBrowser.state === "active"
                  ? t("devices.currentDevice.activeTitle")
                  : currentBrowser.state === "revoked"
                    ? t("devices.currentDevice.revokedTitle")
                    : t("devices.currentDevice.unregisteredTitle")}
              </h2>
              <StatusChip tone="info">{t("devices.thisDevice")}</StatusChip>
              {currentBrowser.state === "active" ? (
                <StatusChip tone="success">{t("devices.status.active")}</StatusChip>
              ) : null}
              {currentBrowser.state === "revoked" ? (
                <StatusChip tone="warning">{t("devices.status.revoked")}</StatusChip>
              ) : null}
            </div>
            {currentBrowser.state === "active" ? (
              <p className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("devices.currentDevice.activeDetail").replace(
                  "{branch}",
                  branchNameById.get(currentBrowser.device?.branchId ?? "") ??
                    boundWorkspace?.branchName ??
                    t("devices.branchFallback"),
                )}
              </p>
            ) : null}
            {currentBrowser.state === "revoked" ? (
              <p
                className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted"
                data-testid="devices-this-browser-revoked"
              >
                {t("devices.currentDevice.revokedDetail")}
              </p>
            ) : null}
            {currentBrowser.state === "unregistered" ? (
              <p className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted">
                {posDevice.detail}
              </p>
            ) : null}
            <p
              className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted"
              data-testid="devices-status"
            >
              {t("devices.statusLabel")}: {posDevice.registrationStatus}
            </p>
            {currentBrowser.device ? (
              <p className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted">
                {formatDeviceModelLine(currentBrowser.device.platform, currentBrowser.device.model)}
              </p>
            ) : null}
          </div>
        </div>

        {revokedCurrentDevice ? (
          <p
            className="mb-0 text-[length:var(--exits-text-sm)]"
            data-testid="devices-revoked-current-notice"
          >
            {t("devices.revoke.successCurrentDevice")}
          </p>
        ) : null}

        {currentBrowser.state !== "unregistered" && !registerFormOpen ? (
          <div className="flex flex-wrap gap-2">
            {currentBrowser.state === "revoked" ? (
              <Button
                type="button"
                className="min-h-11"
                data-testid="devices-register-again"
                onClick={() => openRegisterForm(currentBrowser.device?.branchId ?? null)}
              >
                {t("devices.currentDevice.registerAgain")}
              </Button>
            ) : null}
          </div>
        ) : null}

        {showRegisterForm ? (
          <div className="flex min-w-0 flex-col gap-3" data-testid="devices-register-form">
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("devices.deviceNameLabel")}
              <input
                className="catalog-form-select"
                data-testid="devices-name-input"
                value={deviceName}
                onChange={(event) => setDeviceName(event.target.value)}
              />
            </label>
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("devices.branchLabel")}
              <select
                className="catalog-form-select"
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
            {capacityBlocked ? (
              <p
                className="mb-0 text-[length:var(--exits-text-xs)] text-muted"
                data-testid="devices-register-blocked"
              >
                {t("devices.capacity.limitReachedDetail")}
              </p>
            ) : null}
            <div className="device-register-actions">
              {registerFormOpen ? (
                <Button
                  type="button"
                  variant="outline"
                  className="device-register-actions__cancel min-h-11"
                  data-testid="devices-register-cancel"
                  onClick={() => {
                    setRegisterFormOpen(false);
                    setActionError(null);
                  }}
                >
                  {t("devices.cancel")}
                </Button>
              ) : null}
              <Button
                type="button"
                className="device-register-actions__submit min-h-11"
                data-testid="devices-register-browser"
                disabled={registerMutation.isPending || capacityBlocked}
                onClick={() => registerMutation.mutate()}
              >
                {registerMutation.isPending
                  ? t("devices.registering")
                  : t("devices.registerThisDevice")}
              </Button>
            </div>
          </div>
        ) : null}
      </section>

      {actionError ? (
        <div
          role="alert"
          className="exits-alert exits-alert--error"
          data-testid="devices-action-error"
        >
          <div className="flex gap-3">
            <CircleAlert className="mt-0.5 size-5 shrink-0 text-destructive" aria-hidden />
            <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">{actionError}</p>
          </div>
        </div>
      ) : null}

      {devicesQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
      {devicesQuery.isError ? (
        <ErrorState title={t("error.title")} detail={t("devices.loadError")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="devices-list">
        {devices.map((device) => {
          const Icon = DEVICE_ICONS[deviceIconKind(device.platform, device.model)];
          const isCurrent = isCurrentDevice(device, localInstallationId);
          const modelLine = formatDeviceModelLine(device.platform, device.model);
          const lastUsed = formatTimestamp(device.lastSeenAtUtc);
          const registered = formatTimestamp(device.registeredAtUtc);

          return (
            <li key={device.id}>
              <div
                className="exits-list__card device-row min-w-0"
                data-testid={`device-row-${device.id}`}
                data-current-device={isCurrent}
              >
                <span className="device-row__icon" aria-hidden>
                  <Icon />
                </span>
                <div className="device-row__main min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="exits-list__name m-0 truncate font-semibold">
                      {device.friendlyName}
                    </p>
                    <StatusChip tone="success">{t("devices.status.active")}</StatusChip>
                    {isCurrent ? (
                      <span data-testid={`device-this-device-${device.id}`}>
                        <StatusChip tone="info">{t("devices.thisDevice")}</StatusChip>
                      </span>
                    ) : null}
                  </div>
                  {modelLine ? (
                    <p className="mb-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                      {modelLine}
                    </p>
                  ) : null}
                  <p className="mb-0 mt-0.5 truncate text-[length:var(--exits-text-sm)] text-muted">
                    {branchNameById.get(device.branchId) ?? t("devices.branchFallback")}
                  </p>
                  <dl className="device-row__meta m-0 mt-2 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-[length:var(--exits-text-xs)] text-muted">
                    {lastUsed ? (
                      <>
                        <dt className="m-0">{t("devices.lastUsed")}</dt>
                        <dd className="m-0">{lastUsed}</dd>
                      </>
                    ) : null}
                    {registered ? (
                      <>
                        <dt className="m-0">{t("devices.registeredOn")}</dt>
                        <dd className="m-0">{registered}</dd>
                      </>
                    ) : null}
                    {device.appVersion ? (
                      <>
                        <dt className="m-0">{t("devices.appVersion")}</dt>
                        <dd className="m-0">{device.appVersion}</dd>
                      </>
                    ) : null}
                  </dl>
                </div>
                {canManage && device.status === "Active" ? (
                  <div className="device-row__aside">
                    <Button
                      type="button"
                      variant="ghost"
                      className="device-row__remove"
                      data-testid={`device-revoke-${device.id}`}
                      onClick={() => openRevoke(device)}
                    >
                      {t("devices.removeDevice")}
                    </Button>
                  </div>
                ) : null}
              </div>
            </li>
          );
        })}
      </ul>

      {devices.length === 0 && !devicesQuery.isLoading ? (
        <div data-testid="devices-empty">
          <EmptyState title={t("devices.empty")} detail="" />
        </div>
      ) : null}

      <BottomSheet
        open={revokeTarget !== null}
        onClose={closeRevoke}
        panelId="devices-revoke-panel"
        testId="devices-revoke-panel"
        title={t("devices.removeTitle")}
        closeLabel={t("devices.closeSheet")}
        panelClassName="devices-revoke-sheet"
      >
        {revokeTarget ? (
          <div className="devices-revoke-sheet__body flex min-w-0 flex-col gap-3 overflow-y-auto">
            <div className="flex items-start gap-2.5">
              <ShieldAlert
                className="mt-0.5 size-5 shrink-0 text-[var(--exits-warning)]"
                aria-hidden
              />
              <div className="min-w-0 flex-1">
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold wrap-break-word">
                  {revokeTarget.friendlyName}
                </p>
                <p
                  className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted"
                  data-testid="devices-revoke-warning"
                >
                  {revokeTargetIsCurrent
                    ? t("devices.remove.warningCurrentDevice")
                    : t("devices.remove.warning")}
                </p>
              </div>
            </div>

            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("devices.revokeReasonLabel")}
              <input
                className="catalog-form-select"
                data-testid="devices-revoke-reason"
                value={revokeReason}
                minLength={MIN_REVOKE_REASON_LENGTH}
                onChange={(event) => setRevokeReason(event.target.value)}
              />
              <span className="text-[length:var(--exits-text-xs)] font-normal text-muted">
                {t("devices.revoke.reasonHint")}
              </span>
            </label>

            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("devices.revoke.passwordLabel")}
              <div className="devices-revoke-password">
                <input
                  className="catalog-form-select devices-revoke-password__input"
                  data-testid="devices-revoke-password"
                  type={revokePasswordVisible ? "text" : "password"}
                  autoComplete="current-password"
                  value={revokePassword}
                  onChange={(event) => setRevokePassword(event.target.value)}
                />
                <button
                  type="button"
                  className="devices-revoke-password__toggle"
                  data-testid="devices-revoke-password-toggle"
                  aria-label={
                    revokePasswordVisible
                      ? t("devices.revoke.hidePassword")
                      : t("devices.revoke.showPassword")
                  }
                  onClick={() => setRevokePasswordVisible((visible) => !visible)}
                >
                  {revokePasswordVisible ? (
                    <EyeOff className="size-5" aria-hidden />
                  ) : (
                    <Eye className="size-5" aria-hidden />
                  )}
                </button>
              </div>
              <span className="text-[length:var(--exits-text-xs)] font-normal text-muted">
                {t("devices.revoke.passwordHint")}
              </span>
            </label>

            {revokeError ? (
              <p
                className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
                data-testid="devices-revoke-error"
              >
                {revokeError}
              </p>
            ) : null}

            <div className="devices-revoke-actions">
              <Button
                type="button"
                variant="outline"
                className="devices-revoke-actions__cancel min-h-11"
                data-testid="devices-revoke-cancel"
                onClick={closeRevoke}
              >
                {t("devices.cancel")}
              </Button>
              <Button
                type="button"
                variant="destructive"
                className="devices-revoke-actions__confirm min-h-11"
                data-testid="devices-revoke-confirm"
                disabled={
                  revokeMutation.isPending || revokeReasonTooShort || !revokePassword.trim()
                }
                onClick={() => revokeMutation.mutate()}
              >
                {revokeMutation.isPending ? t("devices.removing") : t("devices.removeConfirm")}
              </Button>
            </div>
          </div>
        ) : null}
      </BottomSheet>
    </div>
  );
}
