import { useEffect, useState } from "react";
import {
  fetchLocalValidationIdentities,
  type QuickLoginIdentity,
} from "@/api/platform/platform-auth-client";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { useI18n } from "@/i18n/I18nProvider";

function loginValueForIdentity(identity: QuickLoginIdentity): string {
  return (identity.email || identity.username || "").trim();
}

/** Dev/test helper: pick a seeded identity to fill username/email; password stays manual. */
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
    <div className="border-t border-border pt-4">
      <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold tracking-wide uppercase text-muted">
        {t("signIn.localValidation")}
      </p>
      <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("signIn.localValidationHint")}
      </p>
      <label className="mt-3 flex min-w-0 flex-col gap-1.5" htmlFor="pos-test-user">
        <span className="text-[length:var(--exits-text-sm)] font-semibold">
          {t("signIn.testUser")}
        </span>
        <select
          id="pos-test-user"
          className="h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground"
          defaultValue=""
          onChange={(event) => {
            const key = event.target.value;
            const identity = identities.find(
              (item) => (item.key ?? item.username ?? item.email) === key,
            );
            if (!identity) {
              return;
            }
            const login = loginValueForIdentity(identity);
            if (login) {
              onSelectIdentity(login);
            }
          }}
        >
          <option value="">{t("signIn.selectUser")}</option>
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
