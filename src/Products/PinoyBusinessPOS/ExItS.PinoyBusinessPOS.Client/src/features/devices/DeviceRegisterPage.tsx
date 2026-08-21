import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { redeemPosDeviceRegistrationToken } from "@/api/platform/pos-devices-client";
import { getDurableInstallationDeviceId } from "@/workspace/browser-installation-identity";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function DeviceRegisterPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const fromSell = searchParams.get("from") === "sell";
  const { boundWorkspace, workspaces, refreshPosDevice } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const boundBranchId = boundWorkspace?.branchId ?? "";
  const branchLocked = Boolean(boundBranchId);
  const [token, setToken] = useState("");
  const [deviceName, setDeviceName] = useState("Counter browser");
  const [branchId, setBranchId] = useState(boundBranchId);
  const [successName, setSuccessName] = useState<string | null>(null);
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

  const redeemMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId) {
        throw new Error(t("devices.noOrganization"));
      }
      const effectiveBranchId = branchLocked ? boundBranchId : branchId;
      if (!effectiveBranchId) {
        throw new Error(t("devices.branchRequired"));
      }
      const code = token.trim();
      if (!code) {
        throw new Error(t("devices.codeRequired"));
      }
      const identity = getDurableInstallationDeviceId();
      if (!identity.ok) {
        throw new Error(t("devices.identityUnavailable"));
      }
      const result = await redeemPosDeviceRegistrationToken(organizationId, {
        token: code,
        branchId: effectiveBranchId,
        installationDeviceId: identity.installationDeviceId,
        friendlyName: deviceName.trim() || t("devices.defaultBrowserName"),
        platform: "Browser",
      });
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("devices.redeemError"));
      }
      return { device: result.value, branchId: effectiveBranchId };
    },
    onSuccess: async ({ device, branchId: registeredBranchId }) => {
      setError(null);
      setSuccessName(device.friendlyName);
      await refreshPosDevice({ branchId: registeredBranchId });
      // Re-enter Sell gate (device → shift → floor) when coming from sell, or continue to sell.
      navigate(fromSell || Boolean(boundWorkspace) ? "/sell" : "/", { replace: true });
    },
    onError: (err: Error) => {
      setSuccessName(null);
      setError(err.message);
    },
  });

  return (
    <div data-testid="device-register-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("devices.redeemTitle")} description={t("devices.redeemLede")} />

      <Card>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("devices.redeemHint")}
        </p>
        <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("devices.codeLabel")}
          <textarea
            className="min-h-24 rounded border border-[var(--exits-border)] bg-transparent px-3 py-2"
            data-testid="device-redeem-code"
            value={token}
            onChange={(event) => setToken(event.target.value)}
            placeholder={t("devices.codePlaceholder")}
          />
        </label>
        <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("devices.deviceNameLabel")}
          <input
            className="min-h-11 rounded border border-[var(--exits-border)] bg-transparent px-3"
            data-testid="device-redeem-name"
            value={deviceName}
            onChange={(event) => setDeviceName(event.target.value)}
          />
        </label>
        {branchLocked ? (
          <div className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("devices.branchLabel")}</span>
            <p
              className="m-0 min-h-11 rounded border border-[var(--exits-border)] bg-[var(--exits-surface-muted)] px-3 py-3 font-medium"
              data-testid="device-redeem-branch-locked"
            >
              {boundBranchName}
            </p>
          </div>
        ) : (
          <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("devices.branchLabel")}
            <select
              className="min-h-11 rounded border border-[var(--exits-border)] bg-transparent px-3"
              data-testid="device-redeem-branch"
              value={branchId}
              onChange={(event) => setBranchId(event.target.value)}
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
        <Button
          type="button"
          className="mt-3 min-h-11"
          data-testid="device-redeem-submit"
          disabled={redeemMutation.isPending}
          onClick={() => redeemMutation.mutate()}
        >
          {redeemMutation.isPending ? t("devices.redeeming") : t("devices.redeemSubmit")}
        </Button>
      </Card>

      {error ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          data-testid="device-redeem-error"
        >
          {error}
        </p>
      ) : null}
      {successName ? (
        <Card data-testid="device-redeem-success">
          <p className="m-0">{t("devices.redeemSuccess")}</p>
          <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">{successName}</p>
        </Card>
      ) : null}

      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/">{t("notFound.home")}</Link>
      </Button>
    </div>
  );
}
