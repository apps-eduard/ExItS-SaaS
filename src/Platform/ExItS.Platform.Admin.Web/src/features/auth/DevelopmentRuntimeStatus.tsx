import { areTestUserToolsPermitted } from "@/lib/auth/development-tools";
import { getFrontendRuntimeStatus } from "@/lib/env";
import { usePreferences } from "@/hooks/use-preferences";

export function DevelopmentRuntimeStatus({ compact = false }: { compact?: boolean }) {
  const { t } = usePreferences();
  if (!areTestUserToolsPermitted()) {
    return null;
  }

  const status = getFrontendRuntimeStatus();

  return (
    <div
      className={
        compact
          ? "grid gap-0.5 px-2 py-1.5 text-[length:var(--exits-text-xs)] text-muted"
          : "mt-3 grid gap-0.5 text-[length:var(--exits-text-xs)] text-muted"
      }
      data-testid="dev-runtime-status"
    >
      <p className="font-semibold tracking-wide uppercase">{status.app}</p>
      <p>
        {t("runtime.mode")}: {status.frontendMode}
      </p>
      <p>
        {t("runtime.build")}: {status.buildSha}
      </p>
      <p>
        {t("runtime.api")}: {status.apiBaseUrl}
      </p>
      <p>
        {t("runtime.lv")}:{" "}
        {status.localValidationToolsEnabled ? t("runtime.enabled") : t("runtime.disabled")}
      </p>
    </div>
  );
}
