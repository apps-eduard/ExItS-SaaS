import { useEffect, useState } from "react";
import { getLocalValidationEnabled, listQuickLoginIdentities } from "@/api/auth/auth-client";
import type { LocalValidationIdentity } from "@/api/auth/auth-types";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { usePreferences } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import { env } from "@/lib/env";

export function DevelopmentTestUserTools({
  onSelectLogin,
}: {
  onSelectLogin: (loginId: string) => void;
}) {
  const { t } = usePreferences();
  const [identities, setIdentities] = useState<LocalValidationIdentity[]>([]);
  const allowed = areDevelopmentToolsAllowed();

  useEffect(() => {
    if (!allowed) {
      return;
    }

    const controller = new AbortController();
    void (async () => {
      try {
        const enabled = await getLocalValidationEnabled(env.platformApiBaseUrl, controller.signal);
        if (!enabled) {
          return;
        }
        const list = await listQuickLoginIdentities(env.platformApiBaseUrl, controller.signal);
        if (!controller.signal.aborted) {
          setIdentities(list);
        }
      } catch {
        if (!controller.signal.aborted) {
          setIdentities([]);
        }
      }
    })();

    return () => controller.abort();
  }, [allowed]);

  if (!allowed || identities.length === 0) {
    return null;
  }

  return (
    <div className="mt-6">
      <Separator className="mb-4" />
      <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-wide text-muted uppercase">
        {t("auth.devTools")}
      </p>
      <Label htmlFor="dev-test-user" className="mt-3 block">
        {t("auth.devTools.select")}
      </Label>
      <select
        id="dev-test-user"
        className="mt-1 h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-muted"
        defaultValue=""
        autoComplete="off"
        aria-describedby="dev-test-user-hint"
        onChange={(event) => {
          const identity = identities.find((item) => item.key === event.target.value);
          const loginId = identity?.email || identity?.username;
          if (loginId) {
            onSelectLogin(loginId);
          }
        }}
      >
        <option value="">{t("auth.devTools.placeholder")}</option>
        {identities.map((identity) => (
          <option key={identity.key} value={identity.key}>
            {identity.listLabel || identity.displayName}
          </option>
        ))}
      </select>
      <p id="dev-test-user-hint" className="mt-2 text-[length:var(--exits-text-xs)] text-muted">
        {t("auth.devTools.hint")}
      </p>
    </div>
  );
}
