import { useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { updatePlatformRegionalSettings } from "@/api/settings/settings-client";
import type { PlatformRegionalSettings } from "@/api/settings/settings-types";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import {
  SettingsField,
  SettingsFieldGroup,
  SettingsFormShell,
} from "@/features/settings/SettingsFormShell";
import {
  isPlatformSettingsForbidden,
  settingsControlClassName,
} from "@/features/settings/settings-form-utils";
import {
  platformRegionalSettingsQueryKey,
  usePlatformRegionalSettingsQuery,
} from "@/features/settings/use-settings-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";

export function RegionalSettingsPanel() {
  const { t } = usePreferences();
  const query = usePlatformRegionalSettingsQuery(true);

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
          operation: "Load platform regional settings",
        })}
        title={t("settings.loadError")}
        onRetry={() => void query.refetch()}
      />
    );
  }

  return <RegionalSettingsForm data={query.data} />;
}

function RegionalSettingsForm({ data }: { data: PlatformRegionalSettings }) {
  const { t } = usePreferences();
  const queryClient = useQueryClient();
  const [defaultTimeZoneId, setDefaultTimeZoneId] = useState(data.defaultTimeZoneId);
  const [defaultLocale, setDefaultLocale] = useState(data.defaultLocale);
  const [defaultCurrencyCode, setDefaultCurrencyCode] = useState(data.defaultCurrencyCode);
  const [defaultCountryCode, setDefaultCountryCode] = useState(data.defaultCountryCode);
  const [dateFormat, setDateFormat] = useState(data.dateFormat ?? "");
  const [timeFormat, setTimeFormat] = useState(data.timeFormat ?? "");
  const [saving, setSaving] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const dirty = useMemo(
    () =>
      defaultTimeZoneId !== data.defaultTimeZoneId ||
      defaultLocale !== data.defaultLocale ||
      defaultCurrencyCode !== data.defaultCurrencyCode ||
      defaultCountryCode !== data.defaultCountryCode ||
      dateFormat !== (data.dateFormat ?? "") ||
      timeFormat !== (data.timeFormat ?? ""),
    [
      data.dateFormat,
      data.defaultCountryCode,
      data.defaultCurrencyCode,
      data.defaultLocale,
      data.defaultTimeZoneId,
      data.timeFormat,
      dateFormat,
      defaultCountryCode,
      defaultCurrencyCode,
      defaultLocale,
      defaultTimeZoneId,
      timeFormat,
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
            const updated = await updatePlatformRegionalSettings(env.platformApiBaseUrl, {
              defaultTimeZoneId,
              defaultLocale,
              defaultCurrencyCode,
              defaultCountryCode,
              dateFormat: dateFormat.length > 0 ? dateFormat : null,
              timeFormat: timeFormat.length > 0 ? timeFormat : null,
              expectedVersion: data.version,
            });
            queryClient.setQueryData(platformRegionalSettingsQueryKey, updated);
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
        description={t("settings.regional.group.defaultsDescription")}
        title={t("settings.regional.group.defaults")}
      >
        <SettingsField
          className="sm:col-span-1"
          hint={t("settings.regional.field.timeZoneHint")}
          htmlFor="default-timezone"
          label={t("settings.regional.field.timeZone")}
        >
          <input
            aria-describedby="default-timezone-hint"
            className={settingsControlClassName}
            id="default-timezone"
            value={defaultTimeZoneId}
            onChange={(event) => setDefaultTimeZoneId(event.target.value)}
          />
        </SettingsField>
        <SettingsField
          className="sm:col-span-1"
          hint={t("settings.regional.field.localeHint")}
          htmlFor="default-locale"
          label={t("settings.regional.field.locale")}
        >
          <input
            aria-describedby="default-locale-hint"
            className={settingsControlClassName}
            id="default-locale"
            value={defaultLocale}
            onChange={(event) => setDefaultLocale(event.target.value)}
          />
        </SettingsField>
        <SettingsField
          className="sm:col-span-1"
          hint={t("settings.regional.field.currencyHint")}
          htmlFor="default-currency"
          label={t("settings.regional.field.currency")}
        >
          <input
            aria-describedby="default-currency-hint"
            className={settingsControlClassName}
            id="default-currency"
            value={defaultCurrencyCode}
            onChange={(event) => setDefaultCurrencyCode(event.target.value.toUpperCase())}
          />
        </SettingsField>
        <SettingsField
          className="sm:col-span-1"
          hint={t("settings.regional.field.countryHint")}
          htmlFor="default-country"
          label={t("settings.regional.field.country")}
        >
          <input
            aria-describedby="default-country-hint"
            className={settingsControlClassName}
            id="default-country"
            value={defaultCountryCode}
            onChange={(event) => setDefaultCountryCode(event.target.value.toUpperCase())}
          />
        </SettingsField>
        <SettingsField
          className="sm:col-span-1"
          hint={t("settings.regional.field.dateFormatHint")}
          htmlFor="date-format"
          label={t("settings.regional.field.dateFormat")}
        >
          <input
            aria-describedby="date-format-hint"
            className={settingsControlClassName}
            id="date-format"
            value={dateFormat}
            onChange={(event) => setDateFormat(event.target.value)}
          />
        </SettingsField>
        <SettingsField
          className="sm:col-span-1"
          hint={t("settings.regional.field.timeFormatHint")}
          htmlFor="time-format"
          label={t("settings.regional.field.timeFormat")}
        >
          <input
            aria-describedby="time-format-hint"
            className={settingsControlClassName}
            id="time-format"
            value={timeFormat}
            onChange={(event) => setTimeFormat(event.target.value)}
          />
        </SettingsField>
      </SettingsFieldGroup>
    </SettingsFormShell>
  );
}
