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

/** Dev/test helper only — hidden in production builds. Fills username; never passwords. */
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
    <label className="flex min-w-0 flex-col gap-2" htmlFor="pos-test-user">
      <span className="text-[length:var(--exits-text-sm)] font-semibold text-foreground">
        {t("signIn.devTools")}
      </span>
      <select
        id="pos-test-user"
        className="exits-select"
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
              {identity.listLabel ||
                identity.displayName ||
                identity.username ||
                identity.email}
            </option>
          );
        })}
      </select>
    </label>
  );
}
