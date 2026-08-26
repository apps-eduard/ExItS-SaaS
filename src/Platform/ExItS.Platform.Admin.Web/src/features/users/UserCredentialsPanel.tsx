import { useState } from "react";
import { PlatformApiError } from "@/api/platform-http";
import {
  markPlatformUserEmailVerified,
  setPlatformUserPassword,
  unlockPlatformUserCredential,
  type PlatformCredentialStatus,
} from "@/api/users/user-mutations";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { usePlatformUserCredentialsQuery } from "@/features/users/use-user-detail-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";

function formatInstant(value: string | null | undefined, language: string): string {
  if (!value) {
    return "—";
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

export function UserCredentialsPanel({ userId }: { userId: string }) {
  const { t, language } = usePreferences();
  const query = usePlatformUserCredentialsQuery(userId);
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  async function runMutation(
    action: () => Promise<PlatformCredentialStatus>,
    successKey: "users.credentials.passwordSuccess" | "users.credentials.unlockSuccess" | "users.credentials.verifiedSuccess",
  ) {
    if (busy) {
      return;
    }
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      await action();
      await query.refetch();
      setSuccess(t(successKey));
      setPassword("");
    } catch (err) {
      setError(
        err instanceof PlatformApiError
          ? (err.problem.detail ?? err.message)
          : err instanceof Error
            ? err.message
            : t("users.credentials.failed"),
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <div data-testid="users-credentials-panel">
    <DashboardSection title={t("users.credentials.title")}>
      {query.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("users.credentials.loading")}>
          <DashboardWidgetSkeleton rows={4} />
        </div>
      ) : null}

      {query.isError ? (
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: query.error,
            operation: "Load credentials",
          })}
          title={t("users.credentials.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <div className="grid gap-3">
          {success ? (
            <Alert title={success} tone="success" data-testid="users-credentials-success" />
          ) : null}
          {error ? (
            <Alert title={error} tone="danger" data-testid="users-credentials-error" />
          ) : null}
          <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("users.credentials.hasPassword")}
              </dt>
              <dd>{query.data.hasPassword ? t("users.credentials.yes") : t("users.credentials.no")}</dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("users.credentials.emailVerified")}
              </dt>
              <dd>
                {query.data.emailVerified ? t("users.credentials.yes") : t("users.credentials.no")}
                {query.data.emailVerifiedAtUtc
                  ? ` · ${formatInstant(query.data.emailVerifiedAtUtc, language)}`
                  : ""}
              </dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("users.credentials.locked")}
              </dt>
              <dd>
                {query.data.isLockedOut ? t("users.credentials.yes") : t("users.credentials.no")}
                {query.data.lockoutEndUtc
                  ? ` · ${formatInstant(query.data.lockoutEndUtc, language)}`
                  : ""}
              </dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("users.credentials.failedAccess")}
              </dt>
              <dd>{query.data.failedAccessCount}</dd>
            </div>
          </dl>

          <div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-end">
            <label
              className="grid gap-1 text-[length:var(--exits-text-sm)]"
              htmlFor="users-set-password"
            >
              {t("users.credentials.newPassword")}
              <Input
                id="users-set-password"
                type="password"
                data-testid="users-credentials-password"
                value={password}
                disabled={busy}
                autoComplete="new-password"
                onChange={(event) => setPassword(event.target.value)}
              />
            </label>
            <Button
              type="button"
              size="sm"
              disabled={busy || !password.trim()}
              data-testid="users-credentials-set-password"
              onClick={() =>
                void runMutation(
                  () => setPlatformUserPassword(env.platformApiBaseUrl, userId, password.trim()),
                  "users.credentials.passwordSuccess",
                )
              }
            >
              {t("users.credentials.setPassword")}
            </Button>
          </div>

          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={busy || !query.data.isLockedOut}
              data-testid="users-credentials-unlock"
              onClick={() =>
                void runMutation(
                  () => unlockPlatformUserCredential(env.platformApiBaseUrl, userId),
                  "users.credentials.unlockSuccess",
                )
              }
            >
              {t("users.credentials.unlock")}
            </Button>
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={busy || query.data.emailVerified}
              data-testid="users-credentials-verify-email"
              onClick={() =>
                void runMutation(
                  () => markPlatformUserEmailVerified(env.platformApiBaseUrl, userId),
                  "users.credentials.verifiedSuccess",
                )
              }
            >
              {t("users.credentials.markVerified")}
            </Button>
          </div>
        </div>
      ) : null}
    </DashboardSection>
    </div>
  );
}
