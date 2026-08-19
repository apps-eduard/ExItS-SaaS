import { useState, type FormEvent } from "react";
import { Eye, EyeOff } from "lucide-react";
import { ApiClientError } from "@/api/http";
import { useSession } from "@/auth/SessionProvider";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { IconButton } from "@/components/ui/icon-button";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function signInErrorKey(error: unknown): MessageKey {
  if (error instanceof ApiClientError && error.status === 429) {
    return "auth.rateLimited";
  }
  if (error instanceof ApiClientError && (error.status === 401 || error.status === 403)) {
    return "auth.invalidCredentials";
  }
  return "auth.signInFailed";
}

const fieldClassName =
  "min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-radius-md)] border border-input bg-surface px-3 text-[length:var(--exits-text-md)] text-foreground outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus)]";

export function SignInPage() {
  const { t } = useI18n();
  const { signIn } = useSession();
  const [usernameOrEmail, setUsernameOrEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [errorKey, setErrorKey] = useState<MessageKey | null>(null);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorKey(null);
    setSubmitting(true);
    try {
      await signIn(usernameOrEmail.trim(), password);
    } catch (error) {
      setErrorKey(signInErrorKey(error));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-dvh flex-col bg-background pt-[env(safe-area-inset-top)] pl-[env(safe-area-inset-left)] pr-[env(safe-area-inset-right)] pb-[env(safe-area-inset-bottom)]">
      <main className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center gap-5 px-[var(--exits-page-padding)] py-6">
        <div>
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
            {t("app.name")}
          </p>
          <h1 className="m-0 mt-1 text-[length:var(--exits-text-xl)] font-bold">
            {t("auth.signInTitle")}
          </h1>
          <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("auth.signInSubtitle")}
          </p>
        </div>
        <Card>
          <form className="flex flex-col gap-4" onSubmit={(event) => void onSubmit(event)}>
            <div className="flex flex-col gap-1 text-[length:var(--exits-text-sm)] font-semibold">
              <label htmlFor="sign-in-username">{t("auth.username")}</label>
              <input
                id="sign-in-username"
                className={fieldClassName}
                autoComplete="username"
                name="username"
                value={usernameOrEmail}
                onChange={(event) => setUsernameOrEmail(event.target.value)}
                required
              />
            </div>
            <div className="flex flex-col gap-1 text-[length:var(--exits-text-sm)] font-semibold">
              <label htmlFor="sign-in-password">{t("auth.password")}</label>
              <span className="relative block">
                <input
                  id="sign-in-password"
                  className={`${fieldClassName} pr-12`}
                  autoComplete="current-password"
                  name="password"
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  required
                />
                <span className="absolute inset-y-0 right-1 flex items-center">
                  <IconButton
                    label={t(showPassword ? "auth.hidePassword" : "auth.showPassword")}
                    onClick={() => setShowPassword((current) => !current)}
                  >
                    {showPassword ? (
                      <EyeOff className="size-5" aria-hidden="true" />
                    ) : (
                      <Eye className="size-5" aria-hidden="true" />
                    )}
                  </IconButton>
                </span>
              </span>
            </div>
            {errorKey ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
                {t(errorKey)}
              </p>
            ) : null}
            <Button type="submit" disabled={submitting}>
              {t("auth.submit")}
            </Button>
          </form>
        </Card>
        <Card className="flex flex-col gap-5">
          <LanguageControl />
          <ThemeControl />
        </Card>
      </main>
    </div>
  );
}
