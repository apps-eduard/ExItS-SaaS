import { useEffect, useState } from "react";
import {
  fetchLocalValidationIdentities,
  type QuickLoginIdentity,
} from "@/api/platform-auth/platform-auth-client";
import { isFrontendLocalValidationMode } from "@/api/platform-auth/local-validation-gate";
import { useI18n } from "@/i18n/I18nProvider";

export function TestUserSelector({
  onSelectIdentity,
}: {
  onSelectIdentity: (usernameOrEmail: string) => void;
}) {
  const { t } = useI18n();
  const [identities, setIdentities] = useState<QuickLoginIdentity[] | null>(null);

  useEffect(() => {
    if (!isFrontendLocalValidationMode()) {
      setIdentities([]);
      return;
    }
    let cancelled = false;
    void fetchLocalValidationIdentities().then((list) => {
      if (!cancelled) {
        setIdentities(list);
      }
    });
    return () => {
      cancelled = true;
    };
  }, []);

  if (!isFrontendLocalValidationMode() || !identities || identities.length === 0) {
    return null;
  }

  return (
    <div className="mt-6 border-t border-border pt-4">
      <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
        {t("auth.localValidation")}
      </p>
      <label className="mt-3 flex min-w-0 flex-col gap-1.5" htmlFor="test-user">
        <span className="text-[length:var(--exits-text-sm)] font-semibold">
          {t("auth.testUser")}
        </span>
        <select
          id="test-user"
          className="h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          defaultValue=""
          onChange={(event) => {
            const key = event.target.value;
            const identity = identities.find(
              (item) => (item.key ?? item.username ?? item.email) === key,
            );
            if (!identity) {
              return;
            }
            onSelectIdentity(identity.email || identity.username || "");
          }}
        >
          <option value="">{t("auth.selectUser")}</option>
          {identities.map((identity) => {
            const value = identity.key ?? identity.username ?? identity.email ?? "";
            return (
              <option key={value} value={value}>
                {identity.listLabel || identity.displayName || identity.username || identity.email}
              </option>
            );
          })}
        </select>
      </label>
    </div>
  );
}
