import { useEffect, useState } from "react";
import { getLocalValidationEnabled, listQuickLoginIdentities } from "@/api/auth/auth-client";
import type { LocalValidationIdentity } from "@/api/auth/auth-types";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { usePreferences } from "@/hooks/use-preferences";
import { areTestUserToolsPermitted } from "@/lib/auth/development-tools";
import { displayPlatformApiBaseUrl, env, isLocalValidationToolsEnabled } from "@/lib/env";

type ToolsStatus =
  | { kind: "loading" }
  | { kind: "ready"; identities: LocalValidationIdentity[] }
  | { kind: "disabled" }
  | { kind: "unreachable" }
  | { kind: "empty" };

function identityHasPassword(identity: LocalValidationIdentity): boolean {
  return Object.keys(identity).some((key) => key.toLowerCase().includes("password"));
}

export function DevelopmentTestUserTools({
  onSelectLogin,
}: {
  onSelectLogin: (loginId: string) => void;
}) {
  const { t } = usePreferences();
  const [status, setStatus] = useState<ToolsStatus>({ kind: "loading" });
  const allowed = areTestUserToolsPermitted();
  const runtimeLocalValidation = isLocalValidationToolsEnabled();

  useEffect(() => {
    if (!allowed) {
      return;
    }

    const controller = new AbortController();
    void (async () => {
      try {
        const enabled = await getLocalValidationEnabled(env.platformApiBaseUrl, controller.signal);
        if (controller.signal.aborted) {
          return;
        }
        if (enabled !== true) {
          setStatus({ kind: "disabled" });
          return;
        }
        const list = await listQuickLoginIdentities(env.platformApiBaseUrl, controller.signal);
        if (controller.signal.aborted) {
          return;
        }
        const identities = Array.isArray(list)
          ? list.filter((identity) => !identityHasPassword(identity))
          : [];
        setStatus(identities.length > 0 ? { kind: "ready", identities } : { kind: "empty" });
      } catch {
        if (!controller.signal.aborted) {
          setStatus({ kind: "unreachable" });
        }
      }
    })();

    return () => controller.abort();
  }, [allowed]);

  if (!allowed) {
    return null;
  }

  if (status.kind === "ready") {
    return (
      <div className="mt-4">
        <Separator className="mb-3" />
        <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-wide text-muted uppercase">
          {runtimeLocalValidation ? t("auth.localValidationTools") : t("auth.devTools")}
        </p>
        <Label htmlFor="dev-test-user" className="mt-2 block text-muted">
          {runtimeLocalValidation
            ? t("auth.localValidationTools.select")
            : t("auth.devTools.select")}
        </Label>
        <select
          id="dev-test-user"
          className="mt-1 h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-muted"
          defaultValue=""
          autoComplete="off"
          aria-describedby="dev-test-user-hint"
          onChange={(event) => {
            const identity = status.identities.find((item) => item.key === event.target.value);
            const loginId = identity?.email || identity?.username;
            if (loginId) {
              onSelectLogin(loginId);
            }
          }}
        >
          <option value="">{t("auth.devTools.placeholder")}</option>
          {status.identities.map((identity) => (
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

  const identityStatus =
    status.kind === "loading"
      ? t("auth.devTools.status.loading")
      : status.kind === "disabled"
        ? t("auth.devTools.status.disabled")
        : status.kind === "empty"
          ? t("auth.devTools.status.empty")
          : t("auth.devTools.status.unreachable");
  const loading = status.kind === "loading";

  return (
    <div className="mt-4" data-testid="dev-test-user-diagnostic">
      <Separator className="mb-3" />
      <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-wide text-muted uppercase">
        {runtimeLocalValidation ? t("auth.localValidationTools") : t("auth.devTools")}
      </p>
      <p className="mt-2 text-[length:var(--exits-text-sm)] font-medium">
        {loading ? t("auth.devTools.loading") : t("auth.devTools.unavailable")}
      </p>
      {loading ? null : (
        <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
          {t("auth.devTools.unavailableHint")}
        </p>
      )}
      <ul className="mt-2 grid gap-0.5 text-[length:var(--exits-text-xs)] text-muted">
        <li>
          {t("auth.devTools.apiBaseUrl")}: {displayPlatformApiBaseUrl()}
        </li>
        <li>
          {t("auth.devTools.lvEnabled")}:{" "}
          {status.kind === "disabled" ? t("runtime.disabled") : t("runtime.enabled")}
        </li>
        <li>
          {t("auth.devTools.identities")}: {identityStatus}
        </li>
      </ul>
    </div>
  );
}
