import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { Navigate, useLocation } from "react-router-dom";
import { ExitsLogo } from "@/components/brand/exits-logo";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PasswordInput } from "@/components/ui/password-input";
import { useSession } from "@/session/SessionProvider";
import {
  persistRememberedUsername,
  readRememberedUsername,
  readRememberMePreference,
} from "./remember-me";

function SignInPage() {
  const { t } = useTranslation();
  const { status, signIn } = useSession();
  const location = useLocation();
  const [usernameOrEmail, setUsernameOrEmail] = useState(() =>
    readRememberedUsername(),
  );
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(() => readRememberMePreference());
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (status === "authenticated") {
    const redirectTo =
      (location.state as { from?: string } | null)?.from ?? "/";
    return <Navigate to={redirectTo} replace />;
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    persistRememberedUsername(usernameOrEmail, rememberMe);
    try {
      const result = await signIn(usernameOrEmail.trim(), password);
      if (!result.ok) {
        setError(result.detail);
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-dvh items-center justify-center bg-background px-4 py-10">
      <Card className="w-full max-w-md shadow-none">
        <CardHeader className="space-y-4">
          <ExitsLogo />
          <div className="space-y-1">
            <CardTitle className="text-xl">{t("auth.signInTitle")}</CardTitle>
            <p className="text-sm text-muted-foreground">
              {t("auth.signInSubtitle")}
            </p>
          </div>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={onSubmit}>
            <div className="space-y-2">
              <Label htmlFor="v2-login">{t("auth.usernameOrEmail")}</Label>
              <Input
                id="v2-login"
                autoComplete="username"
                value={usernameOrEmail}
                onChange={(e) => setUsernameOrEmail(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="v2-password">{t("auth.password")}</Label>
              <PasswordInput
                id="v2-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <label
              htmlFor="v2-remember-me"
              className="inline-flex cursor-pointer items-center gap-2 text-sm text-foreground"
            >
              <Checkbox
                id="v2-remember-me"
                checked={rememberMe}
                disabled={submitting}
                onCheckedChange={(checked) => setRememberMe(checked === true)}
              />
              <span>{t("auth.rememberMe")}</span>
            </label>
            {error ? (
              <p className="text-sm text-destructive" role="alert">
                {error}
              </p>
            ) : null}
            <Button
              type="submit"
              className="w-full"
              loading={submitting}
              disabled={!usernameOrEmail.trim() || !password}
            >
              {t("auth.signIn")}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export { SignInPage };
