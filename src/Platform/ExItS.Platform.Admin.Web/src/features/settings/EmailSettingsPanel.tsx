import { useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  sendPlatformEmailTest,
  updatePlatformEmailSettings,
} from "@/api/settings/settings-client";
import type { PlatformEmailSettings } from "@/api/settings/settings-types";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import {
  SettingsField,
  SettingsFieldGroup,
  SettingsFormShell,
  SettingsSectionCard,
} from "@/features/settings/SettingsFormShell";
import {
  isPlatformSettingsForbidden,
  settingsControlClassName,
} from "@/features/settings/settings-form-utils";
import {
  platformEmailSettingsQueryKey,
  usePlatformEmailSettingsQuery,
} from "@/features/settings/use-settings-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";
import { cn } from "@/lib/utils";

export function EmailSettingsPanel() {
  const { t } = usePreferences();
  const query = usePlatformEmailSettingsQuery(true);

  if (query.isPending) {
    return <DashboardWidgetSkeleton rows={8} />;
  }

  if (query.isError && isPlatformSettingsForbidden(query.error)) {
    return <ShellNotFoundPage />;
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

  return <EmailSettingsForm data={query.data} />;
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
  const [testFailed, setTestFailed] = useState(false);

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

  const canSave = dirty && (!replacePassword || smtpPassword.length > 0);

  return (
    <div className="grid min-w-0 gap-4">
      <SettingsFormShell
        canSave={canSave}
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
        <SettingsFieldGroup
          description={t("settings.email.group.providerDescription")}
          title={t("settings.email.group.provider")}
        >
          <SettingsField htmlFor="provider-mode" label={t("settings.email.field.providerMode")}>
            <select
              className={settingsControlClassName}
              id="provider-mode"
              value={providerMode}
              onChange={(event) => setProviderMode(event.target.value)}
            >
              <option value="Smtp">SMTP</option>
              <option value="Disabled">Disabled</option>
            </select>
          </SettingsField>
        </SettingsFieldGroup>

        <SettingsFieldGroup
          description={t("settings.email.group.smtpDescription")}
          title={t("settings.email.group.smtp")}
        >
          <SettingsField
            className="sm:col-span-1"
            htmlFor="smtp-host"
            label={t("settings.email.field.smtpHost")}
          >
            <input
              className={settingsControlClassName}
              id="smtp-host"
              value={smtpHost}
              onChange={(event) => setSmtpHost(event.target.value)}
            />
          </SettingsField>
          <SettingsField
            className="sm:col-span-1"
            htmlFor="smtp-port"
            label={t("settings.email.field.smtpPort")}
          >
            <input
              className={settingsControlClassName}
              id="smtp-port"
              inputMode="numeric"
              value={smtpPort}
              onChange={(event) => setSmtpPort(event.target.value)}
            />
          </SettingsField>
          <SettingsField htmlFor="smtp-username" label={t("settings.email.field.smtpUsername")}>
            <input
              className={settingsControlClassName}
              id="smtp-username"
              value={smtpUsername}
              onChange={(event) => setSmtpUsername(event.target.value)}
            />
          </SettingsField>
          <div className="grid gap-3 sm:col-span-2">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="grid gap-1">
                <p className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
                  {t("settings.email.field.smtpPassword")}
                </p>
                <p className="flex items-center gap-2" role="status">
                  <Badge tone={data.passwordConfigured ? "success" : "neutral"}>
                    {data.passwordConfigured
                      ? t("settings.email.passwordConfigured")
                      : t("settings.email.passwordNotConfigured")}
                  </Badge>
                </p>
              </div>
              {replacePassword ? (
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setReplacePassword(false);
                    setSmtpPassword("");
                  }}
                >
                  {t("settings.email.cancelReplace")}
                </Button>
              ) : (
                <Button type="button" variant="outline" onClick={() => setReplacePassword(true)}>
                  {t("settings.email.replaceAction")}
                </Button>
              )}
            </div>
            {replacePassword ? (
              <input
                autoComplete="new-password"
                className={settingsControlClassName}
                id="smtp-password"
                aria-label={t("settings.email.newPassword")}
                type="password"
                value={smtpPassword}
                onChange={(event) => setSmtpPassword(event.target.value)}
              />
            ) : null}
          </div>
        </SettingsFieldGroup>

        <SettingsFieldGroup
          description={t("settings.email.group.senderDescription")}
          title={t("settings.email.group.sender")}
        >
          <SettingsField
            className="sm:col-span-1"
            htmlFor="from-display-name"
            label={t("settings.email.field.fromDisplayName")}
          >
            <input
              className={settingsControlClassName}
              id="from-display-name"
              value={fromDisplayName}
              onChange={(event) => setFromDisplayName(event.target.value)}
            />
          </SettingsField>
          <SettingsField
            className="sm:col-span-1"
            htmlFor="from-address"
            label={t("settings.email.field.fromAddress")}
          >
            <input
              className={settingsControlClassName}
              id="from-address"
              type="email"
              value={fromAddress}
              onChange={(event) => setFromAddress(event.target.value)}
            />
          </SettingsField>
          <SettingsField
            className="sm:col-span-1"
            htmlFor="security-mode"
            label={t("settings.email.field.securityMode")}
          >
            <select
              className={settingsControlClassName}
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
            className="sm:col-span-1"
            hint={t("settings.email.field.adminPublicBaseUrlHint")}
            htmlFor="admin-public-base-url"
            label={t("settings.email.field.adminPublicBaseUrl")}
          >
            <input
              aria-describedby="admin-public-base-url-hint"
              className={settingsControlClassName}
              id="admin-public-base-url"
              type="url"
              value={adminPublicBaseUrl}
              onChange={(event) => setAdminPublicBaseUrl(event.target.value)}
            />
          </SettingsField>
        </SettingsFieldGroup>
      </SettingsFormShell>

      <SettingsSectionCard className="border-dashed bg-surface-muted/20">
        <header className="grid gap-1">
          <h3 className="text-[length:var(--exits-text-base)] font-semibold text-foreground">
            {t("settings.email.test.title")}
          </h3>
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("settings.email.test.description")}
          </p>
        </header>
        <SettingsField htmlFor="test-recipient" label={t("settings.email.test.recipient")}>
          <input
            className={settingsControlClassName}
            id="test-recipient"
            type="email"
            value={testRecipient}
            onChange={(event) => setTestRecipient(event.target.value)}
          />
        </SettingsField>
        {testMessage ? (
          <p
            className={cn(
              "text-[length:var(--exits-text-sm)]",
              testFailed ? "text-destructive" : "text-foreground",
            )}
            role={testFailed ? "alert" : "status"}
            aria-live="polite"
          >
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
                setTestFailed(false);
                try {
                  const result = await sendPlatformEmailTest(env.platformApiBaseUrl, {
                    recipientEmail: testRecipient,
                  });
                  setTestFailed(!result.succeeded);
                  setTestMessage(
                    result.message.length > 0
                      ? result.message
                      : result.succeeded
                        ? t("settings.email.test.send")
                        : t("settings.email.test.failed"),
                  );
                } catch (error) {
                  setTestFailed(true);
                  setTestMessage(
                    error instanceof Error ? error.message : t("settings.email.test.failed"),
                  );
                } finally {
                  setTesting(false);
                }
              })();
            }}
          >
            {testing ? t("settings.email.test.sending") : t("settings.email.test.send")}
          </Button>
        </div>
      </SettingsSectionCard>
    </div>
  );
}
