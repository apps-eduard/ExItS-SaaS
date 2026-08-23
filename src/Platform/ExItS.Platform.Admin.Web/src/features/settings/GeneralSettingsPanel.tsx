import { useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { updatePlatformGeneralSettings } from "@/api/settings/settings-client";
import type { PlatformGeneralSettings } from "@/api/settings/settings-types";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { Input } from "@/components/ui/input";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import {
  SettingsField,
  SettingsFieldGroup,
  SettingsFormShell,
} from "@/features/settings/SettingsFormShell";
import { isPlatformSettingsForbidden } from "@/features/settings/settings-form-utils";
import {
  platformGeneralSettingsQueryKey,
  usePlatformGeneralSettingsQuery,
} from "@/features/settings/use-settings-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";

export function GeneralSettingsPanel() {
  const { t } = usePreferences();
  const query = usePlatformGeneralSettingsQuery(true);

  if (query.isPending) {
    return <DashboardWidgetSkeleton rows={6} />;
  }

  if (query.isError && isPlatformSettingsForbidden(query.error)) {
    return <ShellNotFoundPage />;
  }

  if (query.isError) {
    return (
      <ErrorState
        diagnostic={normalizeDiagnosticError({
          error: query.error,
          operation: "Load platform general settings",
        })}
        title={t("settings.loadError")}
        onRetry={() => void query.refetch()}
      />
    );
  }

  return <GeneralSettingsForm data={query.data} />;
}

function GeneralSettingsForm({ data }: { data: PlatformGeneralSettings }) {
  const { t } = usePreferences();
  const queryClient = useQueryClient();
  const [platformDisplayName, setPlatformDisplayName] = useState(data.platformDisplayName);
  const [supportEmail, setSupportEmail] = useState(data.supportEmail ?? "");
  const [brandingLogoUrl, setBrandingLogoUrl] = useState(data.brandingLogoUrl ?? "");
  const [brandingPrimaryColor, setBrandingPrimaryColor] = useState(data.brandingPrimaryColor ?? "");
  const [brandingAccentColor, setBrandingAccentColor] = useState(data.brandingAccentColor ?? "");
  const [saving, setSaving] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const dirty = useMemo(
    () =>
      platformDisplayName !== data.platformDisplayName ||
      supportEmail !== (data.supportEmail ?? "") ||
      brandingLogoUrl !== (data.brandingLogoUrl ?? "") ||
      brandingPrimaryColor !== (data.brandingPrimaryColor ?? "") ||
      brandingAccentColor !== (data.brandingAccentColor ?? ""),
    [
      brandingAccentColor,
      brandingLogoUrl,
      brandingPrimaryColor,
      data.brandingAccentColor,
      data.brandingLogoUrl,
      data.brandingPrimaryColor,
      data.platformDisplayName,
      data.supportEmail,
      platformDisplayName,
      supportEmail,
    ],
  );

  return (
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
            const updated = await updatePlatformGeneralSettings(env.platformApiBaseUrl, {
              platformDisplayName,
              supportEmail: supportEmail.length > 0 ? supportEmail : null,
              brandingLogoUrl: brandingLogoUrl.length > 0 ? brandingLogoUrl : null,
              brandingPrimaryColor: brandingPrimaryColor.length > 0 ? brandingPrimaryColor : null,
              brandingAccentColor: brandingAccentColor.length > 0 ? brandingAccentColor : null,
              expectedVersion: data.version,
            });
            queryClient.setQueryData(platformGeneralSettingsQueryKey, updated);
            setSuccessMessage(t("settings.saveSuccess"));
          } catch (error) {
            setErrorMessage(
              error instanceof Error ? error.message : t("settings.saveError"),
            );
          } finally {
            setSaving(false);
          }
        })();
      }}
    >
      <SettingsFieldGroup
        description={t("settings.general.group.identityDescription")}
        title={t("settings.general.group.identity")}
      >
        <SettingsField
          hint={t("settings.general.field.displayNameHint")}
          htmlFor="platform-display-name"
          label={t("settings.general.field.displayName")}
        >
          <Input
            aria-describedby="platform-display-name-hint"
            id="platform-display-name"
            value={platformDisplayName}
            onChange={(event) => setPlatformDisplayName(event.target.value)}
          />
        </SettingsField>
        <SettingsField
          hint={t("settings.general.field.supportEmailHint")}
          htmlFor="support-email"
          label={t("settings.general.field.supportEmail")}
        >
          <Input
            aria-describedby="support-email-hint"
            id="support-email"
            type="email"
            value={supportEmail}
            onChange={(event) => setSupportEmail(event.target.value)}
          />
        </SettingsField>
      </SettingsFieldGroup>

      <SettingsFieldGroup
        description={t("settings.general.group.brandingDescription")}
        title={t("settings.general.group.branding")}
      >
        <SettingsField
          className="sm:col-span-2"
          hint={t("settings.general.field.logoUrlHint")}
          htmlFor="branding-logo-url"
          label={t("settings.general.field.logoUrl")}
        >
          <Input
            aria-describedby="branding-logo-url-hint"
            id="branding-logo-url"
            type="url"
            value={brandingLogoUrl}
            onChange={(event) => setBrandingLogoUrl(event.target.value)}
          />
        </SettingsField>
        <SettingsField
          hint={t("settings.general.field.primaryColorHint")}
          htmlFor="branding-primary-color"
          label={t("settings.general.field.primaryColor")}
        >
          <Input
            aria-describedby="branding-primary-color-hint"
            id="branding-primary-color"
            value={brandingPrimaryColor}
            onChange={(event) => setBrandingPrimaryColor(event.target.value)}
          />
        </SettingsField>
        <SettingsField
          hint={t("settings.general.field.accentColorHint")}
          htmlFor="branding-accent-color"
          label={t("settings.general.field.accentColor")}
        >
          <Input
            aria-describedby="branding-accent-color-hint"
            id="branding-accent-color"
            value={brandingAccentColor}
            onChange={(event) => setBrandingAccentColor(event.target.value)}
          />
        </SettingsField>
      </SettingsFieldGroup>
    </SettingsFormShell>
  );
}
