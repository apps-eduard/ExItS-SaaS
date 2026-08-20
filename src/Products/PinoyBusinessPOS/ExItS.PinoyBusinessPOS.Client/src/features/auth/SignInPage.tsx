import { useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { TestUserSelector } from "@/features/auth/TestUserSelector";
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
    <div className="sign-in-page mx-auto flex min-h-[100dvh] w-full min-w-0 items-center justify-center px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] py-8">
      <div className="flex w-full max-w-[24rem] min-w-0 flex-col gap-5">
        <div className="flex flex-col items-center gap-3 text-center">
          <div
            className="flex size-12 items-center justify-center rounded-[var(--exits-radius-md)] bg-primary text-lg font-bold text-primary-foreground"
            aria-hidden="true"
          >
            E
          </div>
          <div className="flex flex-col gap-1">
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold tracking-wide uppercase text-muted">
              {t("app.name")}
            </p>
            <h1 className="m-0 text-[length:var(--exits-text-xl)] font-bold tracking-tight text-foreground">
              {t("signIn.title")}
            </h1>
            <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
              {t("signIn.lede")}
            </p>
          </div>
        </div>

        {expired ? (
          <Card>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("signIn.expired")}
            </p>
          </Card>
        ) : null}
        {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

        <Card className="flex flex-col gap-4">
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
            <Button type="submit" className="w-full" disabled={submitting}>
              {submitting ? t("signIn.submitting") : t("signIn.submit")}
            </Button>
          </form>
        </Card>

        <TestUserSelector
          onSelectIdentity={(value) => {
            setUsernameOrEmail(value);
            setPassword("");
            setError(null);
          }}
        />
      </div>
    </div>
  );
}
