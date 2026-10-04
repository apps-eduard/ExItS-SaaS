import { useEffect, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import {
  clearRecoveryEmail,
  confirmRecoveryEmail,
  getMyCredentials,
  requestRecoveryEmail,
  skipRecoveryEmail,
} from "@/api/auth/auth-client";
import type { CredentialStatus } from "@/api/auth/auth-types";
import { PlatformApiError } from "@/api/platform-http";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";

function errorText(error: unknown, fallback: string): string {
  return error instanceof PlatformApiError && error.problem.detail ? error.problem.detail : fallback;
}

export function RecoveryEmailPromptPage() {
  const { t } = usePreferences();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const suggested = params.get("suggest")?.includes("@") ? params.get("suggest")!.trim() : "";
  const [email, setEmail] = useState(suggested);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await requestRecoveryEmail(env.platformApiBaseUrl, { recoveryEmail: email });
      setDone(true);
    } catch (reason) {
      setError(errorText(reason, t("account.recovery.error")));
    } finally {
      setBusy(false);
    }
  }

  async function onSkip() {
    setBusy(true);
    setError(null);
    try {
      await skipRecoveryEmail(env.platformApiBaseUrl);
      navigate("/admin", { replace: true });
    } catch (reason) {
      setError(errorText(reason, t("account.recovery.error")));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card className="border-border">
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("account.recovery.prompt.title")}</h1>
      <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">{t("account.recovery.prompt.hint")}</p>
      {error ? <Alert className="mt-4" tone="danger" title={error} /> : null}
      {done ? (
        <p className="mt-4 text-[length:var(--exits-text-sm)]">{t("account.recovery.prompt.sent")}</p>
      ) : (
        <form className="mt-4 grid gap-4" onSubmit={onSubmit}>
          <div className="grid gap-1.5">
            <Label htmlFor="recovery-email">{t("account.recovery.address")}</Label>
            <Input
              id="recovery-email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>
          <div className="flex flex-wrap gap-2">
            <Button type="submit" disabled={busy}>
              {t("account.recovery.request")}
            </Button>
            <Button type="button" variant="secondary" disabled={busy} onClick={() => void onSkip()}>
              {t("account.recovery.skip")}
            </Button>
          </div>
        </form>
      )}
      <Link className="mt-3 inline-block text-[length:var(--exits-text-sm)] text-primary" to="/admin">
        {t("account.recovery.continue")}
      </Link>
    </Card>
  );
}

export function ConfirmRecoveryEmailPage() {
  const { t } = usePreferences();
  const [params] = useSearchParams();
  const [token, setToken] = useState(params.get("token")?.trim() ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await confirmRecoveryEmail(env.platformApiBaseUrl, { token });
      setToken("");
      setDone(true);
    } catch (reason) {
      setError(errorText(reason, t("account.recovery.error")));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card className="border-border">
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("account.recovery.confirm.title")}</h1>
      <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">{t("account.recovery.confirm.hint")}</p>
      {error ? <Alert className="mt-4" tone="danger" title={error} /> : null}
      {done ? (
        <p className="mt-4">{t("account.recovery.confirm.done")}</p>
      ) : (
        <form className="mt-4 grid gap-4" onSubmit={onSubmit}>
          <div className="grid gap-1.5">
            <Label htmlFor="recovery-token">{t("account.recovery.confirm.token")}</Label>
            <Input
              id="recovery-token"
              autoComplete="one-time-code"
              value={token}
              onChange={(event) => setToken(event.target.value)}
            />
          </div>
          <Button type="submit" disabled={busy || token.trim().length === 0}>
            {t("account.recovery.confirm.submit")}
          </Button>
        </form>
      )}
      <Link className="mt-3 inline-block text-[length:var(--exits-text-sm)] text-primary" to="/admin">
        {t("account.recovery.continue")}
      </Link>
    </Card>
  );
}

export function RecoveryEmailAccountPage() {
  const { t } = usePreferences();
  const [status, setStatus] = useState<CredentialStatus | null>(null);
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    void getMyCredentials(env.platformApiBaseUrl, controller.signal)
      .then((next) => {
        if (controller.signal.aborted) {
          return;
        }
        setStatus(next);
        if (next.recoveryEmail) {
          setEmail(next.recoveryEmail);
        }
      })
      .catch((reason: unknown) => {
        if (!controller.signal.aborted) {
          setError(errorText(reason, t("account.loadError")));
        }
      });
    return () => controller.abort();
  }, [t]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setInfo(null);
    try {
      const ack = await requestRecoveryEmail(env.platformApiBaseUrl, { recoveryEmail: email });
      setInfo(ack.message);
      setStatus(await getMyCredentials(env.platformApiBaseUrl));
    } catch (reason) {
      setError(errorText(reason, t("account.recovery.error")));
    } finally {
      setBusy(false);
    }
  }

  async function onClear() {
    setBusy(true);
    setError(null);
    setInfo(null);
    try {
      const next = await clearRecoveryEmail(env.platformApiBaseUrl);
      setStatus(next);
      setEmail("");
      setInfo(t("account.recovery.cleared"));
    } catch (reason) {
      setError(errorText(reason, t("account.recovery.error")));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card className="border-border">
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("account.recovery.manage")}</h1>
      <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">{t("account.recovery.accountHint")}</p>
      {error ? <Alert className="mt-4" tone="danger" title={error} /> : null}
      {info ? <p className="mt-4 text-[length:var(--exits-text-sm)]" role="status">{info}</p> : null}
      <dl className="mt-4 grid gap-2 text-[length:var(--exits-text-sm)]">
        <div>
          <dt className="text-muted">{t("account.recovery.verified")}</dt>
          <dd>{status?.recoveryEmailVerified ? status.recoveryEmail : t("account.recovery.none")}</dd>
        </div>
        <div>
          <dt className="text-muted">{t("account.recovery.pending")}</dt>
          <dd>{status?.pendingRecoveryEmail || t("account.recovery.none")}</dd>
        </div>
      </dl>
      <form className="mt-4 grid gap-4" onSubmit={onSubmit}>
        <div className="grid gap-1.5">
          <Label htmlFor="account-recovery-email">{t("account.recovery.address")}</Label>
          <Input
            id="account-recovery-email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </div>
        <div className="flex flex-wrap gap-2">
          <Button type="submit" disabled={busy}>
            {t("account.recovery.request")}
          </Button>
          <Button type="button" variant="secondary" disabled={busy} onClick={() => void onClear()}>
            {t("account.recovery.clear")}
          </Button>
        </div>
      </form>
      <Link className="mt-3 inline-block text-[length:var(--exits-text-sm)] text-primary" to="/admin/recovery-email/confirm">
        {t("account.recovery.confirm.title")}
      </Link>
    </Card>
  );
}
