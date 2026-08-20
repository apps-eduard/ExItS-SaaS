import { useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

export function SignInPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { signIn } = useSession();
  const [usernameOrEmail, setUsernameOrEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const expired = Boolean((location.state as { expired?: boolean } | null)?.expired);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    const ok = await signIn(usernameOrEmail.trim(), password);
    setSubmitting(false);
    if (!ok) {
      setError(t("signIn.error"));
      return;
    }
    navigate("/", { replace: true });
  }

  return (
    <div className="mx-auto flex min-h-[100dvh] w-full max-w-md min-w-0 flex-col justify-center gap-4 px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] py-8">
      <PageHeader title={t("signIn.title")} description={t("signIn.lede")} />
      {expired ? (
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("signIn.expired")}</p>
        </Card>
      ) : null}
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}
      <Card>
        <form className="flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
          <Input
            label={t("signIn.usernameLabel")}
            name="usernameOrEmail"
            autoComplete="username"
            value={usernameOrEmail}
            onChange={(event) => setUsernameOrEmail(event.target.value)}
            required
          />
          <Input
            label={t("signIn.passwordLabel")}
            name="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
          <Button type="submit" disabled={submitting}>
            {submitting ? t("signIn.submitting") : t("signIn.submit")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
