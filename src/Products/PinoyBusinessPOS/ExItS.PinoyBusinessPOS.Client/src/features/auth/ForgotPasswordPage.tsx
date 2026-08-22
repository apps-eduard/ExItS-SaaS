import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { AuthExperienceLayout } from "@/features/auth/AuthExperienceLayout";
import { useI18n } from "@/i18n/I18nProvider";
import { requestPasswordReset } from "@/api/platform/platform-auth-client";

export function ForgotPasswordPage() {
  const { t } = useI18n();
  const [usernameOrEmail, setUsernameOrEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [isOffline, setIsOffline] = useState(
    () => typeof navigator !== "undefined" && navigator.onLine === false,
  );

  useEffect(() => {
    const syncOnline = () => setIsOffline(typeof navigator !== "undefined" && navigator.onLine === false);
    window.addEventListener("online", syncOnline);
    window.addEventListener("offline", syncOnline);
    return () => {
      window.removeEventListener("online", syncOnline);
      window.removeEventListener("offline", syncOnline);
    };
  }, []);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (isOffline) {
      setError(t("auth.forgotPasswordOffline"));
      return;
    }
    setSubmitting(true);
    setError(null);
    setInfo(null);
    const result = await requestPasswordReset(usernameOrEmail.trim());
    setSubmitting(false);
    if (!result.ok) {
      setError(result.detail);
      return;
    }
    setInfo(t("auth.forgotPasswordAck"));
  }

  return (
    <AuthExperienceLayout activeTab="sign-in" onTabChange={() => undefined}>
      <div className="flex flex-col gap-4" data-testid="forgot-password-page">
        <div>
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-bold text-foreground">
            {t("auth.forgotPasswordTitle")}
          </h2>
          <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {isOffline ? t("auth.forgotPasswordOffline") : t("auth.forgotPasswordLede")}
          </p>
        </div>
        {info ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-success" data-testid="forgot-password-info">
            {info}
          </p>
        ) : null}
        {error ? (
          <p role="alert" className="m-0 text-[length:var(--exits-text-sm)] text-destructive" data-testid="forgot-password-error">
            {error}
          </p>
        ) : null}
        <form className="flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
          <Input
            label={t("signIn.usernameLabel")}
            name="usernameOrEmail"
            autoComplete="username"
            value={usernameOrEmail}
            onChange={(event) => setUsernameOrEmail(event.target.value)}
            required
            disabled={submitting || isOffline}
          />
          <Button type="submit" className="w-full min-h-11" disabled={submitting || isOffline}>
            {submitting ? t("auth.forgotPasswordSubmitting") : t("auth.forgotPasswordSubmit")}
          </Button>
        </form>
        <Link
          to="/sign-in"
          className="text-center text-[length:var(--exits-text-sm)] font-semibold text-primary hover:underline"
        >
          {t("auth.backToSignIn")}
        </Link>
      </div>
    </AuthExperienceLayout>
  );
}
