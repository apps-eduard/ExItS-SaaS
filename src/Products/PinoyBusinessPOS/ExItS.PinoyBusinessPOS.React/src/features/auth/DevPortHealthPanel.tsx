import { useCallback, useEffect, useState } from "react";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type PortRow = {
  port: number;
  name: string;
  up: boolean;
};

type HealthPayload = {
  checkedAtUtc?: string;
  ports: PortRow[];
};

const HEALTH_PATH = "/__dev__/port-health";
const POLL_MS = 8_000;

async function fetchPortHealth(signal?: AbortSignal): Promise<HealthPayload | null> {
  try {
    const response = await fetch(HEALTH_PATH, {
      method: "GET",
      cache: "no-store",
      signal,
    });
    if (!response.ok) {
      return null;
    }
    const data = (await response.json()) as HealthPayload;
    if (!Array.isArray(data?.ports)) {
      return null;
    }
    return data;
  } catch {
    return null;
  }
}

/** Dev-only: loopback port status under Development Test User (no IPs shown). */
export function DevPortHealthPanel() {
  const { t } = useI18n();
  const [ports, setPorts] = useState<PortRow[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [unavailable, setUnavailable] = useState(false);

  const refresh = useCallback(async (signal?: AbortSignal) => {
    setLoading(true);
    const data = await fetchPortHealth(signal);
    if (signal?.aborted) {
      return;
    }
    if (!data) {
      setUnavailable(true);
      setPorts(null);
      setLoading(false);
      return;
    }
    setUnavailable(false);
    setPorts(data.ports);
    setLoading(false);
  }, []);

  useEffect(() => {
    if (!isFrontendLocalValidationMode()) {
      return;
    }
    const controller = new AbortController();
    void refresh(controller.signal);
    const timer = window.setInterval(() => {
      void refresh();
    }, POLL_MS);
    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [refresh]);

  if (!isFrontendLocalValidationMode()) {
    return null;
  }

  return (
    <div
      className="mt-4 flex min-w-0 flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3"
      data-testid="dev-port-health"
    >
      <div className="flex items-center justify-between gap-2">
        <span className="text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {t("signIn.portHealth")}
        </span>
        <button
          type="button"
          className="text-[length:var(--exits-text-xs)] font-medium text-primary underline-offset-2 hover:underline"
          data-testid="dev-port-health-refresh"
          onClick={() => void refresh()}
        >
          {t("signIn.portHealthRefresh")}
        </button>
      </div>

      {loading && ports == null ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("signIn.portHealthChecking")}
        </p>
      ) : null}

      {unavailable ? (
        <p
          className="m-0 text-[length:var(--exits-text-xs)] text-[var(--exits-danger)]"
          data-testid="dev-port-health-unavailable"
        >
          {t("signIn.portHealthUnavailable")}
        </p>
      ) : null}

      {ports && ports.length > 0 ? (
        <ul
          className="m-0 flex list-none flex-col gap-1 p-0"
          data-testid="dev-port-health-list"
        >
          {ports.map((row) => (
            <li
              key={row.port}
              className="flex min-w-0 items-center gap-2 text-[length:var(--exits-text-xs)]"
              data-testid={`dev-port-health-${row.port}`}
              data-up={row.up ? "true" : "false"}
            >
              <span
                className={cn(
                  "inline-block size-2 shrink-0 rounded-full",
                  row.up ? "bg-[var(--exits-success)]" : "bg-[var(--exits-danger)]",
                )}
                aria-hidden
              />
              <span className="w-12 shrink-0 font-mono tabular-nums text-muted">
                :{row.port}
              </span>
              <span className="min-w-0 flex-1 truncate text-foreground">{row.name}</span>
              <span
                className={cn(
                  "shrink-0 font-medium",
                  row.up ? "text-[var(--exits-success)]" : "text-[var(--exits-danger)]",
                )}
              >
                {row.up ? t("signIn.portHealthUp") : t("signIn.portHealthDown")}
              </span>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
