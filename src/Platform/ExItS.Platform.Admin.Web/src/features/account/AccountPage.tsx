import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyCredentials } from "@/api/auth/auth-client";
import type { CredentialStatus } from "@/api/auth/auth-types";
import { PlatformApiError } from "@/api/platform-http";
import { Alert } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";
import { env } from "@/lib/env";

export function AccountPage() {
  const { t } = usePreferences();
  const { session, signOut } = useSession();
  const [status, setStatus] = useState<CredentialStatus | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    void getMyCredentials(env.platformApiBaseUrl, controller.signal)
      .then((next) => {
        if (!controller.signal.aborted) {
          setStatus(next);
        }
      })
      .catch((reason: unknown) => {
        if (controller.signal.aborted) {
          return;
        }
        setError(reason instanceof PlatformApiError ? reason.message : t("account.loadError"));
      });
    return () => controller.abort();
  }, [t]);

  return (
    <Card className="border-border">
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("account.title")}</h1>
      <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">{t("account.hint")}</p>
      {error ? <Alert className="mt-4" tone="danger" title={error} /> : null}
      <dl className="mt-4 grid gap-2 text-[length:var(--exits-text-sm)]">
        <div>
          <dt className="text-muted">{t("account.displayName")}</dt>
          <dd>{session?.displayName || session?.username}</dd>
        </div>
        <div>
          <dt className="text-muted">{t("account.email")}</dt>
          <dd>{session?.email || "—"}</dd>
        </div>
        <div>
          <dt className="text-muted">{t("account.recovery.verified")}</dt>
          <dd>
            {status?.recoveryEmailVerified && status.recoveryEmail
              ? status.recoveryEmail
              : t("account.recovery.none")}
          </dd>
        </div>
        <div>
          <dt className="text-muted">{t("account.recovery.pending")}</dt>
          <dd>{status?.pendingRecoveryEmail || t("account.recovery.none")}</dd>
        </div>
      </dl>
      <div className="mt-4 flex flex-wrap gap-3 text-[length:var(--exits-text-sm)]">
        <Link className="text-primary underline-offset-4 hover:underline" to="/admin/account/change-password">
          {t("account.changePassword.title")}
        </Link>
        <Link className="text-primary underline-offset-4 hover:underline" to="/admin/account/recovery-email">
          {t("account.recovery.manage")}
        </Link>
        <Link className="text-primary underline-offset-4 hover:underline" to="/admin/workspaces">
          {t("workspaces.title")}
        </Link>
        <button type="button" className="text-primary underline-offset-4 hover:underline" onClick={() => void signOut()}>
          {t("shell.signOut")}
        </button>
      </div>
    </Card>
  );
}
