import { useEffect, useState } from "react";
import { getLocalValidationEnabled, listQuickLoginIdentities } from "@/api/auth/auth-client";
import type { LocalValidationIdentity } from "@/api/auth/auth-types";
import { ErrorState } from "@/components/exits/ErrorState";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { usePreferences } from "@/hooks/use-preferences";
import { areTestUserToolsPermitted } from "@/lib/auth/development-tools";
import {
  buildDiagnosticEnvironmentFromPreferences,
  normalizeDiagnosticError,
} from "@/lib/diagnostics/normalize-diagnostic-error";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";
import { env, displayPlatformApiBaseUrl, isLocalValidationToolsEnabled } from "@/lib/env";

type ToolsStatus =
  | { kind: "loading" }
  | { kind: "ready"; identities: LocalValidationIdentity[] }
  | { kind: "empty" }
  | { kind: "failure"; diagnostic: DiagnosticRecord };

function identityHasPassword(identity: LocalValidationIdentity): boolean {
  return Object.keys(identity).some((key) => key.toLowerCase().includes("password"));
}

export function DevelopmentTestUserTools({
  onSelectLogin,
}: {
  onSelectLogin: (loginId: string) => void;
}) {
  const { t, language, theme, density } = usePreferences();
  const [status, setStatus] = useState<ToolsStatus>({ kind: "loading" });
  const [reloadToken, setReloadToken] = useState(0);
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
          // Backend gate closed: keep the section visible with an honest empty state.
          setStatus({ kind: "empty" });
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
      } catch (error) {
        if (controller.signal.aborted) {
          return;
        }
        setStatus({
          kind: "failure",
          diagnostic: normalizeDiagnosticError({
            error,
            operation: "Load development test users",
            environment: buildDiagnosticEnvironmentFromPreferences({
              locale: language,
              theme,
              density,
            }),
          }),
        });
      }
    })();

    return () => controller.abort();
  }, [allowed, density, language, reloadToken, theme]);

  if (!allowed) {
    return null;
  }

  const title = runtimeLocalValidation ? t("auth.localValidationTools") : t("auth.devTools");

  return (
    <div className="mt-4" data-testid="dev-test-user-tools">
      <Separator className="mb-3" />
      <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-wide text-muted uppercase">
        {title}
      </p>
      <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
        {t("auth.devTools.apiBaseUrl")}: {displayPlatformApiBaseUrl()}
      </p>

      {status.kind === "loading" ? (
        <p className="mt-2 text-[length:var(--exits-text-sm)] text-muted" role="status">
          {t("auth.devTools.loading")}
        </p>
      ) : null}

      {status.kind === "empty" ? (
        <p className="mt-2 text-[length:var(--exits-text-sm)] text-muted" role="status">
          {t("auth.devTools.empty")}
        </p>
      ) : null}

      {status.kind === "failure" ? (
        <div className="mt-2">
          <ErrorState
            diagnostic={status.diagnostic}
            title={t("auth.devTools.failureTitle")}
            description={t("auth.devTools.failure")}
            onRetry={() => {
              setStatus({ kind: "loading" });
              setReloadToken((value) => value + 1);
            }}
          />
        </div>
      ) : null}

      {status.kind === "ready" ? (
        <>
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
        </>
      ) : null}
    </div>
  );
}
