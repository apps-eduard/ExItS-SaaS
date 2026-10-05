import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { adoptSessionToken, getMyCredentials } from "@/api/auth/auth-client";
import { Alert } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { usePreferences } from "@/hooks/use-preferences";
import { continueAfterAdminAuthentication } from "@/lib/auth/continue-after-admin-auth";
import { readExternalCallbackQuery, replaceBrowserLocation } from "@/lib/auth/cutover-routing";
import { env } from "@/lib/env";

export function ExternalLoginCallbackPage() {
  const { t } = usePreferences();
  const [params] = useSearchParams();
  const [error, setError] = useState<string | null>(null);
  const started = useRef(false);

  useEffect(() => {
    if (started.current) {
      return;
    }
    started.current = true;
    const query = readExternalCallbackQuery(params);
    const cleaned = new URLSearchParams(params);
    cleaned.delete("sessionToken");
    const nextSearch = cleaned.toString();
    window.history.replaceState(
      {},
      "",
      `${window.location.pathname}${nextSearch.length > 0 ? `?${nextSearch}` : ""}`,
    );

    if (query.unsafeReturn) {
      setError(t("external.unsafeReturn"));
      return;
    }
    if (query.sessionToken.length === 0) {
      setError(t("external.missing"));
      return;
    }

    const token = query.sessionToken;
    void (async () => {
      try {
        const session = await adoptSessionToken(env.platformApiBaseUrl, token);
        const credentials = await getMyCredentials(env.platformApiBaseUrl);
        if (credentials.needsRecoveryEmailPrompt) {
          const suggest =
            query.suggestRecoveryEmail ??
            (session.email.toLowerCase().includes("@gmail.") ? session.email : "");
          const target = suggest
            ? `/admin/recovery-email?suggest=${encodeURIComponent(suggest)}`
            : "/admin/recovery-email";
          replaceBrowserLocation(target);
          return;
        }
        const outcome = await continueAfterAdminAuthentication({
          session,
          returnQuery: query.returnPath,
          navigate: (path) => replaceBrowserLocation(path),
        });
        if (outcome === "stay") {
          replaceBrowserLocation(query.returnPath ?? "/admin");
        }
      } catch {
        setError(t("external.failed"));
      }
    })();
  }, [params, t]);

  return (
    <Card className="border-border">
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("external.title")}</h1>
      {error ? (
        <>
          <Alert className="mt-4" tone="danger" title={error} />
          <Link className="mt-3 inline-block text-primary" to="/admin/login">
            {t("auth.signIn")}
          </Link>
        </>
      ) : (
        <p className="mt-2 text-[length:var(--exits-text-sm)] text-muted">{t("external.working")}</p>
      )}
    </Card>
  );
}

export function externalSignInHref(provider: "google" | "facebook"): string {
  const returnUrl = `${window.location.origin}/admin/external-login-callback`;
  const base = env.platformApiBaseUrl.replace(/\/+$/, "");
  return `${base}/api/v1/platform/auth/external/${provider}/challenge?returnUrl=${encodeURIComponent(returnUrl)}`;
}
