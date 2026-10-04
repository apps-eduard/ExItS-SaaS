import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { acceptOrganizationInvitation } from "@/api/auth/auth-client";
import type { AcceptInvitationResult } from "@/api/auth/auth-types";
import { PlatformApiError } from "@/api/platform-http";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";

export function AcceptOrganizationInvitationPage() {
  const { t } = usePreferences();
  const [params] = useSearchParams();
  const token = params.get("token")?.trim() ?? "";
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(token.length === 0 ? t("invitation.missing") : null);
  const [result, setResult] = useState<AcceptInvitationResult | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (token.length === 0) {
      setError(t("invitation.missing"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const accepted = await acceptOrganizationInvitation(env.platformApiBaseUrl, token, password);
      setPassword("");
      setResult(accepted);
    } catch (reason) {
      setPassword("");
      const code = reason instanceof PlatformApiError ? reason.problem.errorCode : undefined;
      if (code === "platform.invitation.expired" || code === "application.invitation.not_found") {
        setError(t("invitation.invalid"));
        return;
      }
      setError(
        reason instanceof PlatformApiError && reason.problem.detail
          ? reason.problem.detail
          : t("invitation.invalid"),
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card className="border-border">
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("invitation.title")}</h1>
      {error ? <Alert className="mt-4" tone="danger" title={error} /> : null}
      {result ? (
        <div className="mt-4 grid gap-2 text-[length:var(--exits-text-sm)]">
          <p>{t("invitation.ready")}</p>
          <p>
            {t("invitation.organization")}: <strong>{result.organizationDisplayName}</strong>
          </p>
          <p>
            {t("invitation.contact")}: <strong>{result.contactEmail}</strong>
          </p>
          <p>
            {t("invitation.staffLogin")}: <strong>{result.staffLogin}</strong>
          </p>
          <Link className="text-primary underline-offset-4 hover:underline" to="/admin/login">
            {t("auth.signIn")}
          </Link>
        </div>
      ) : (
        <form className="mt-4 grid gap-4" onSubmit={onSubmit}>
          <p className="text-[length:var(--exits-text-sm)] text-muted">{t("invitation.hint")}</p>
          <div className="grid gap-1.5">
            <Label htmlFor="invitation-password">{t("auth.password")}</Label>
            <Input
              id="invitation-password"
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </div>
          <Button type="submit" disabled={busy || token.length === 0 || password.length === 0}>
            {t("invitation.submit")}
          </Button>
        </form>
      )}
    </Card>
  );
}
