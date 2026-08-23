import { useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  sendPlatformEmailTest,
  updatePlatformEmailSettings,
} from "@/api/settings/settings-client";
import type { PlatformEmailSettings } from "@/api/settings/settings-types";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { Button } from "@/components/ui/button";
import { SettingsField, SettingsFormShell } from "@/features/settings/SettingsFormShell";
import {
  platformEmailSettingsQueryKey,
  usePlatformEmailSettingsQuery,
} from "@/features/settings/use-settings-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";

export function EmailSettingsPanel() {
  const { t } = usePreferences();
  const query = usePlatformEmailSettingsQuery(true);

  if (query.isPending) {
    return <DashboardWidgetSkeleton rows={8} />;
  }

  if (query.isError) {
    return (
      <ErrorState
        diagnostic={normalizeDiagnosticError({
          error: query.error,
          operation: "Load platform email settings",
        })}
        title={t("settings.loadError")}
        onRetry={() => void query.refetch()}
      />
    );
  }

  return <EmailSettingsForm key={query.data.version} data={query.data} />;
}

function EmailSettingsForm({ data }: { data: PlatformEmailSettings }) {
  const { t } = usePreferences();
  const queryClient = useQueryClient();
  const [providerMode, setProviderMode] = useState(data.providerMode);
  const [smtpHost, setSmtpHost] = useState(data.smtpHost ?? "");
  const [smtpPort, setSmtpPort] = useState(data.smtpPort != null ? String(data.smtpPort) : "");
  const [smtpUsername, setSmtpUsername] = useState(data.smtpUsername ?? "");
  const [replacePassword, setReplacePassword] = useState(false);
  const [smtpPassword, setSmtpPassword] = useState("");
  const [fromDisplayName, setFromDisplayName] = useState(data.fromDisplayName);
  const [fromAddress, setFromAddress] = useState(data.fromAddress);
  const [securityMode, setSecurityMode] = useState(data.securityMode);
  const [adminPublicBaseUrl, setAdminPublicBaseUrl] = useState(data.adminPublicBaseUrl ?? "");
  const [testRecipient, setTestRecipient] = useState("");
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [testMessage, setTestMessage] = useState<string | null>(null);

  const dirty = useMemo(() => {
    const port = smtpPort.length > 0 ? Number(smtpPort) : null;
    return (
      providerMode !== data.providerMode ||
      smtpHost !== (data.smtpHost ?? "") ||
      port !== data.smtpPort ||
      smtpUsername !== (data.smtpUsername ?? "") ||
      replacePassword ||
      fromDisplayName !== data.fromDisplayName ||
      fromAddress !== data.fromAddress ||
      securityMode !== data.securityMode ||
      adminPublicBaseUrl !== (data.adminPublicBaseUrl ?? "")
    );
  }, [
    adminPublicBaseUrl,
    data.adminPublicBaseUrl,
    data.fromAddress,
    data.fromDisplayName,
    data.providerMode,
    data.securityMode,
    data.smtpHost,
    data.smtpPort,
    data.smtpUsername,
    fromAddress,
    fromDisplayName,
    providerMode,
    replacePassword,
    securityMode,
    smtpHost,
    smtpPort,
    smtpUsername,
  ]);

  return (
    <div className="grid max-w-2xl gap-4">
      <SettingsFormShell
        dirty={dirty}
        dirtyMessage={t("settings.unsavedChanges")}
        errorMessage={errorMessage}
        saveLabel={t("settings.save")}
        saving={saving}
        successMessage={successMessage}
        onSave={() => {
          void (async () => {
            setSaving(true);
            setErrorMessage(null);
            setSuccessMessage(null);
            try {
              const updated = await updatePlatformEmailSettings(env.platformApiBaseUrl, {
                providerMode,
                smtpHost: smtpHost.length > 0 ? smtpHost : null,
                smtpPort: smtpPort.length > 0 ? Number(smtpPort) : null,
                smtpUsername: smtpUsername.length > 0 ? smtpUsername : null,
                replacePassword,
                smtpPassword: replacePassword ? smtpPassword : null,
                fromDisplayName,
                fromAddress,
                securityMode,
                adminPublicBaseUrl: adminPublicBaseUrl.length > 0 ? adminPublicBaseUrl : null,
                expectedVersion: data.version,
              });
              queryClient.setQueryData(platformEmailSettingsQueryKey, updated);
              setReplacePassword(false);
              setSmtpPassword("");
              setSuccessMessage(t("settings.saveSuccess"));
            } catch (error) {
              setErrorMessage(error instanceof Error ? error.message : t("settings.saveError"));
            } finally {
              setSaving(false);
            }
          })();
        }}
      >
        <SettingsField htmlFor="provider-mode" label={t("settings.email.field.providerMode")}>
          <select
            className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
            id="provider-mode"
            value={providerMode}
            onChange={(event) => setProviderMode(event.target.value)}
          >
            <option value="Smtp">SMTP</option>
            <option value="Disabled">Disabled</option>
          </select>
        </SettingsField>
        <SettingsField htmlFor="smtp-host" label={t("settings.email.field.smtpHost")}>
          <input
            className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
            id="smtp-host"
            value={smtpHost}
            onChange={(event) => setSmtpHost(event.target.value)}
          />
        </SettingsField>
        <SettingsField htmlFor="smtp-port" label={t("settings.email.field.smtpPort")}>
          <input
            className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
            id="smtp-port"
            inputMode="numeric"
            value={smtpPort}
            onChange={(event) => setSmtpPort(event.target.value)}
          />
        </SettingsField>
        <SettingsField htmlFor="smtp-username" label={t("settings.email.field.smtpUsername")}>
          <input
            className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
            id="smtp-username"
            value={smtpUsername}
            onChange={(event) => setSmtpUsername(event.target.value)}
          />
        </SettingsField>
        <div className="grid gap-2">
          <p className="text-[length:var(--exits-text-sm)] font-medium">
            {t("settings.email.field.smtpPassword")}
          </p>
          <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
            {data.passwordConfigured
              ? t("settings.email.passwordConfigured")
              : t("settings.email.passwordNotConfigured")}
          </p>
          <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <input
              checked={replacePassword}
              type="checkbox"
              onChange={(event) => setReplacePassword(event.target.checked)}
            />
            {t("settings.email.replacePassword")}
          </label>
          {replacePassword ? (
            <input
              aria-label={t("settings.email.newPassword")}
              className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
              type="password"
              value={smtpPassword}
              onChange={(event) => setSmtpPassword(event.target.value)}
            />
          ) : null}
        </div>
        <SettingsField htmlFor="from-display-name" label={t("settings.email.field.fromDisplayName")}>
          <input
            className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
            id="from-display-name"
            value={fromDisplayName}
            onChange={(event) => setFromDisplayName(event.target.value)}
          />
        </SettingsField>
        <SettingsField htmlFor="from-address" label={t("settings.email.field.fromAddress")}>
          <input
            className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
            id="from-address"
            type="email"
            value={fromAddress}
            onChange={(event) => setFromAddress(event.target.value)}
          />
        </SettingsField>
        <SettingsField htmlFor="security-mode" label={t("settings.email.field.securityMode")}>
          <select
            className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
            id="security-mode"
            value={securityMode}
            onChange={(event) => setSecurityMode(event.target.value)}
          >
            <option value="None">None</option>
            <option value="StartTls">STARTTLS</option>
            <option value="Ssl">SSL/TLS</option>
          </select>
        </SettingsField>
        <SettingsField
          htmlFor="admin-public-base-url"
          label={t("settings.email.field.adminPublicBaseUrl")}
        >
          <input
            className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
            id="admin-public-base-url"
            value={adminPublicBaseUrl}
            onChange={(event) => setAdminPublicBaseUrl(event.target.value)}
          />
        </SettingsField>
      </SettingsFormShell>

      <section className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-4">
        <h2 className="text-[length:var(--exits-text-base)] font-semibold">
          {t("settings.email.test.title")}
        </h2>
        <SettingsField htmlFor="test-recipient" label={t("settings.email.test.recipient")}>
          <input
            className="w-full rounded-[var(--exits-density-radius)] border border-border bg-background px-3 py-2 text-[length:var(--exits-text-sm)]"
            id="test-recipient"
            type="email"
            value={testRecipient}
            onChange={(event) => setTestRecipient(event.target.value)}
          />
        </SettingsField>
        {testMessage ? (
          <p className="text-[length:var(--exits-text-sm)]" role="status">
            {testMessage}
          </p>
        ) : null}
        <div>
          <Button
            disabled={testing || testRecipient.length === 0}
            type="button"
            variant="secondary"
            onClick={() => {
              void (async () => {
                setTesting(true);
                setTestMessage(null);
                try {
                  const result = await sendPlatformEmailTest(env.platformApiBaseUrl, {
                    recipientEmail: testRecipient,
                  });
                  setTestMessage(result.message);
                } catch (error) {
                  setTestMessage(error instanceof Error ? error.message : t("settings.email.test.failed"));
                } finally {
                  setTesting(false);
                }
              })();
            }}
          >
            {testing ? t("settings.email.test.sending") : t("settings.email.test.send")}
          </Button>
        </div>
      </section>
    </div>
  );
}
