import { useCallback, useEffect, useRef, useState } from "react";
import { RefreshCw, RotateCcw } from "lucide-react";
import {
  fetchSupervisorHealth,
  fetchSupervisorOperation,
  isLocalValidationControlHost,
  resetLocalValidationData,
  restartAllSupervisorApps,
  restartSupervisorService,
  type LocalValidationServiceRow,
} from "@/api/local-validation-supervisor-client";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { Button } from "@/components/ui/button";
import { useToast } from "@/components/exits/ToastProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

const HEALTH_PATH = "/__dev__/port-health";
const POLL_MS = 5_000;
const BUSY_POLL_MS = 2_000;

type VitePortPayload = {
  ports: Array<{ port: number; name: string; up: boolean }>;
};

type DisplayRow = {
  key: string;
  label: string;
  port: number;
  status: string;
  restartable: boolean;
};

function mapSupervisorRows(rows: LocalValidationServiceRow[]): DisplayRow[] {
  return rows.map((row) => ({
    key: row.key,
    label: row.label,
    port: row.port,
    status: row.status,
    restartable: row.restartable,
  }));
}

function mapVitePortRows(payload: VitePortPayload): DisplayRow[] {
  return payload.ports.map((row) => ({
    key: `port-${row.port}`,
    label: row.name,
    port: row.port,
    status: row.up ? "Up" : "Down",
    restartable: false,
  }));
}

async function fetchVitePortHealth(signal?: AbortSignal): Promise<VitePortPayload | null> {
  try {
    const response = await fetch(HEALTH_PATH, {
      method: "GET",
      cache: "no-store",
      signal,
    });
    if (!response.ok) {
      return null;
    }
    const data = (await response.json()) as VitePortPayload;
    if (!Array.isArray(data?.ports)) {
      return null;
    }
    return data;
  } catch {
    return null;
  }
}

function statusTone(status: string): string {
  const normalized = status.toLowerCase();
  if (normalized === "up") return "text-[var(--exits-success)]";
  if (normalized === "down" || normalized === "failed") return "text-[var(--exits-danger)]";
  return "text-muted";
}

function statusDot(status: string): string {
  const normalized = status.toLowerCase();
  if (normalized === "up") return "bg-[var(--exits-success)]";
  if (normalized === "down" || normalized === "failed") return "bg-[var(--exits-danger)]";
  return "bg-[var(--exits-warning, #ca8a04)]";
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });
}

/** Poll until supervisor reports idle or timeout. Tolerates transient fetch failures (e.g. React POS restart). */
async function waitForSupervisorIdle(options: {
  timeoutMs: number;
  onProgress?: (message: string | null) => void;
}): Promise<boolean> {
  const deadline = Date.now() + options.timeoutMs;
  let consecutiveFailures = 0;
  while (Date.now() < deadline) {
    const op = await fetchSupervisorOperation();
    if (op) {
      consecutiveFailures = 0;
      options.onProgress?.(op.progress ?? op.operation ?? null);
      if (!op.busy) {
        return true;
      }
    } else {
      consecutiveFailures += 1;
      // Keep waiting through brief Vite/self-restart outages.
      if (consecutiveFailures > 30) {
        return false;
      }
    }
    await sleep(BUSY_POLL_MS);
  }
  return false;
}

/** Dev-only Local Validation control panel under Development Test User. */
export function DevPortHealthPanel({
  onIdentitiesRefresh,
}: {
  onIdentitiesRefresh?: () => void;
} = {}) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const controlsEnabled = isLocalValidationControlHost();
  const [rows, setRows] = useState<DisplayRow[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [unavailable, setUnavailable] = useState(false);
  const [busy, setBusy] = useState(false);
  const [progress, setProgress] = useState<string | null>(null);
  const [restartingKey, setRestartingKey] = useState<string | null>(null);
  const [resetOpen, setResetOpen] = useState(false);
  const [supervisorOnline, setSupervisorOnline] = useState(false);
  const operationActiveRef = useRef(false);

  const refresh = useCallback(
    async (signal?: AbortSignal, options?: { tolerateMiss?: boolean }) => {
      if (controlsEnabled) {
        const data = await fetchSupervisorHealth(signal);
        if (signal?.aborted) return;
        if (data) {
          setSupervisorOnline(true);
          setUnavailable(false);
          setRows(mapSupervisorRows(data.services));
          if (!operationActiveRef.current) {
            setBusy(Boolean(data.busy));
            setProgress(data.progress ?? data.operation ?? null);
          }
          setLoading(false);
          return;
        }
        if (options?.tolerateMiss || operationActiveRef.current) {
          // Keep prior rows/controls during transient outages (React POS self-restart).
          setLoading(false);
          return;
        }
        setSupervisorOnline(false);
      }

      const vite = await fetchVitePortHealth(signal);
      if (signal?.aborted) return;
      if (!vite) {
        if (options?.tolerateMiss || operationActiveRef.current) {
          setLoading(false);
          return;
        }
        setUnavailable(true);
        setRows(null);
        setLoading(false);
        return;
      }
      setUnavailable(false);
      if (!supervisorOnline) {
        setRows(mapVitePortRows(vite));
      }
      if (!operationActiveRef.current) {
        setBusy(false);
        setProgress(null);
      }
      setLoading(false);
    },
    [controlsEnabled, supervisorOnline],
  );

  useEffect(() => {
    if (!isFrontendLocalValidationMode()) {
      return;
    }
    const controller = new AbortController();
    void refresh(controller.signal);
    const timer = window.setInterval(() => {
      void refresh(undefined, { tolerateMiss: operationActiveRef.current });
    }, operationActiveRef.current ? BUSY_POLL_MS : POLL_MS);
    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [refresh]);

  if (!isFrontendLocalValidationMode()) {
    return null;
  }

  const actionsDisabled = busy || Boolean(restartingKey);
  const showControls = controlsEnabled && supervisorOnline;

  async function onRestartService(row: DisplayRow) {
    if (!controlsEnabled || !row.restartable || actionsDisabled) return;
    const isDown = row.status.toLowerCase() === "down";
    setRestartingKey(row.key);
    setBusy(true);
    operationActiveRef.current = true;
    setProgress(isDown ? `Starting ${row.label}...` : `Restarting ${row.label}...`);
    setRows((prev) =>
      prev
        ? prev.map((item) =>
            item.key === row.key
              ? { ...item, status: isDown ? "Starting" : "Restarting" }
              : item,
          )
        : prev,
    );
    try {
      const resultPromise = restartSupervisorService(row.key);
      // Long-running restart: keep polling while request is in flight.
      const result = await resultPromise;
      await waitForSupervisorIdle({
        timeoutMs: row.key === "react-pos" ? 180_000 : 120_000,
        onProgress: setProgress,
      });
      if (result.ok) {
        showToast(
          result.message ||
            (isDown
              ? `${row.label} started successfully.`
              : `${row.label} restarted successfully.`),
          "success",
        );
      } else {
        showToast(
          result.message ||
            `${row.label} failed to restart. Check the Local Validation console.`,
          "error",
        );
      }
    } catch {
      // React POS self-restart can drop the page briefly; keep polling before final failure.
      const recovered = await waitForSupervisorIdle({
        timeoutMs: 120_000,
        onProgress: setProgress,
      });
      if (recovered) {
        showToast(`${row.label} restart finished.`, "success");
      } else {
        showToast(
          `${row.label} failed to restart. Check the Local Validation console.`,
          "error",
        );
      }
    } finally {
      setRestartingKey(null);
      setBusy(false);
      operationActiveRef.current = false;
      setProgress(null);
      await refresh(undefined, { tolerateMiss: true });
    }
  }

  async function onRestartAll() {
    if (!controlsEnabled || actionsDisabled) return;
    setBusy(true);
    operationActiveRef.current = true;
    setProgress("Restarting applications...");
    try {
      const resultPromise = restartAllSupervisorApps();
      const result = await resultPromise;
      await waitForSupervisorIdle({
        timeoutMs: 300_000,
        onProgress: setProgress,
      });
      if (result.ok) {
        showToast(result.message, "success");
      } else {
        showToast(result.message, "error");
      }
    } catch {
      const recovered = await waitForSupervisorIdle({
        timeoutMs: 180_000,
        onProgress: setProgress,
      });
      if (recovered) {
        showToast(t("signIn.localValidationRestartAppsDone"), "success");
      } else {
        showToast("Restart apps failed. Check the Local Validation console.", "error");
      }
    } finally {
      setBusy(false);
      operationActiveRef.current = false;
      setProgress(null);
      await refresh(undefined, { tolerateMiss: true });
    }
  }

  async function onConfirmReset() {
    if (!controlsEnabled || actionsDisabled) return;
    setResetOpen(false);
    setBusy(true);
    operationActiveRef.current = true;
    setProgress("Resetting Local Validation...");
    try {
      const resultPromise = resetLocalValidationData();
      const result = await resultPromise;
      await waitForSupervisorIdle({
        timeoutMs: 600_000,
        onProgress: (message) =>
          setProgress(message ?? "Resetting Local Validation..."),
      });
      if (result.ok) {
        showToast(result.message, "success");
        onIdentitiesRefresh?.();
      } else {
        showToast(result.message, "error");
      }
    } catch {
      showToast("Reset failed. Check the Local Validation console.", "error");
    } finally {
      setBusy(false);
      operationActiveRef.current = false;
      setProgress(null);
      await refresh(undefined, { tolerateMiss: true });
    }
  }

  return (
    <div
      className="mt-4 flex min-w-0 flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3"
      data-testid="dev-port-health"
      data-controls={showControls ? "true" : "false"}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {t("signIn.localValidationPanel")}
        </span>
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            className="inline-flex items-center gap-1 text-[length:var(--exits-text-xs)] font-medium text-primary underline-offset-2 hover:underline disabled:opacity-50"
            data-testid="dev-port-health-refresh"
            disabled={actionsDisabled}
            title={t("signIn.portHealthRefresh")}
            onClick={() => void refresh()}
          >
            <RefreshCw className="size-3.5" aria-hidden />
            {t("signIn.portHealthRefresh")}
          </button>
          {showControls ? (
            <Button
              type="button"
              variant="secondary"
              data-testid="dev-lv-restart-apps"
              disabled={actionsDisabled}
              onClick={() => void onRestartAll()}
            >
              {t("signIn.localValidationRestartApps")}
            </Button>
          ) : null}
        </div>
      </div>

      {progress ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted" data-testid="dev-lv-progress">
          {progress}
        </p>
      ) : null}

      {loading && rows == null ? (
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

      {rows && rows.length > 0 ? (
        <ul
          className="m-0 flex list-none flex-col gap-1 p-0"
          data-testid="dev-port-health-list"
        >
          {rows.map((row) => {
            const normalized = row.status.toLowerCase();
            const statusLabel =
              normalized === "up"
                ? t("signIn.portHealthUp")
                : normalized === "down"
                  ? t("signIn.portHealthDown")
                  : row.status;
            const actionLabel =
              normalized === "down"
                ? t("signIn.localValidationStartService")
                : t("signIn.localValidationRestartService");
            return (
              <li
                key={row.key}
                className="flex min-w-0 items-center gap-2 text-[length:var(--exits-text-xs)]"
                data-testid={`dev-port-health-${row.port}`}
                data-up={normalized === "up" ? "true" : "false"}
                data-service-key={row.key}
              >
                <span
                  className={cn("inline-block size-2 shrink-0 rounded-full", statusDot(row.status))}
                  aria-hidden
                />
                <span className="min-w-0 flex-1 truncate text-foreground">{row.label}</span>
                <span className="w-12 shrink-0 text-right font-mono tabular-nums text-muted">
                  :{row.port}
                </span>
                <span
                  className={cn(
                    "w-20 shrink-0 text-right font-medium capitalize",
                    statusTone(row.status),
                  )}
                >
                  {statusLabel}
                </span>
                {showControls && row.restartable ? (
                  <button
                    type="button"
                    className="inline-flex h-6 shrink-0 items-center gap-1 rounded px-1.5 text-[length:var(--exits-text-xs)] font-medium text-primary hover:bg-border disabled:opacity-40"
                    data-testid={`dev-lv-restart-${row.key}`}
                    title={actionLabel}
                    aria-label={`${actionLabel} ${row.label}`}
                    disabled={actionsDisabled}
                    onClick={() => void onRestartService(row)}
                  >
                    <RotateCcw
                      className={cn(
                        "size-3.5",
                        restartingKey === row.key && "animate-spin",
                      )}
                      aria-hidden
                    />
                    <span>{actionLabel}</span>
                  </button>
                ) : (
                  <span className="inline-block w-16 shrink-0" aria-hidden />
                )}
              </li>
            );
          })}
        </ul>
      ) : null}

      {showControls ? (
        <div className="mt-1 flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="destructive"
            data-testid="dev-lv-reset-open"
            disabled={actionsDisabled}
            onClick={() => setResetOpen(true)}
          >
            {t("signIn.localValidationReset")}
          </Button>
        </div>
      ) : controlsEnabled ? (
        <p
          className="m-0 text-[length:var(--exits-text-xs)] text-muted"
          data-testid="dev-lv-supervisor-offline"
        >
          {t("signIn.localValidationSupervisorOffline")}
        </p>
      ) : (
        <p
          className="m-0 text-[length:var(--exits-text-xs)] text-muted"
          data-testid="dev-lv-controls-remote"
        >
          {t("signIn.localValidationControlsLocalhostOnly")}
        </p>
      )}

      {resetOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          data-testid="dev-lv-reset-modal"
          role="dialog"
          aria-modal="true"
          aria-labelledby="dev-lv-reset-title"
        >
          <div className="w-full max-w-md rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface)] p-4 shadow-lg">
            <h2
              id="dev-lv-reset-title"
              className="m-0 text-[length:var(--exits-text-md)] font-semibold text-foreground"
            >
              {t("signIn.localValidationResetTitle")}
            </h2>
            <p className="mt-2 text-[length:var(--exits-text-sm)] text-muted">
              {t("signIn.localValidationResetBody")}
            </p>
            <ul className="mt-2 list-disc pl-5 text-[length:var(--exits-text-xs)] text-muted">
              <li>{t("signIn.localValidationResetBullet1")}</li>
              <li>{t("signIn.localValidationResetBullet2")}</li>
              <li>{t("signIn.localValidationResetBullet3")}</li>
              <li>{t("signIn.localValidationResetBullet4")}</li>
            </ul>
            <p className="mt-2 text-[length:var(--exits-text-xs)] font-medium text-[var(--exits-danger)]">
              {t("signIn.localValidationResetCannotUndo")}
            </p>
            <div className="mt-4 flex justify-end gap-2">
              <Button
                type="button"
                variant="secondary"
                data-testid="dev-lv-reset-cancel"
                onClick={() => setResetOpen(false)}
              >
                {t("signIn.localValidationResetCancel")}
              </Button>
              <Button
                type="button"
                variant="destructive"
                data-testid="dev-lv-reset-confirm"
                onClick={() => void onConfirmReset()}
              >
                {t("signIn.localValidationReset")}
              </Button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
