import { zodResolver } from "@hookform/resolvers/zod";
import { Eye, EyeOff } from "lucide-react";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { z } from "zod";
import { classifySignInFailure, type SignInFailureKind } from "@/api/auth/auth-errors";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ErrorState } from "@/components/exits/ErrorState";
import { DevelopmentTestUserTools } from "@/features/auth/DevelopmentTestUserTools";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";
import { resolvePostLoginPath } from "@/lib/auth/safe-return-path";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

type SignInValues = {
  email: string;
  password: string;
};

function failureMessageKey(kind: SignInFailureKind) {
  switch (kind) {
    case "invalid_credentials":
      return "auth.error.invalidCredentials" as const;
    case "account_locked":
      return "auth.error.accountLocked" as const;
    case "account_disabled":
      return "auth.error.accountDisabled" as const;
    case "network":
      return "auth.error.network" as const;
    default:
      return "auth.error.unknown" as const;
  }
}

export function SignInPage() {
  const { t, language, theme, density } = usePreferences();
  const { signIn } = useSession();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [passwordVisible, setPasswordVisible] = useState(false);
  const [formError, setFormError] = useState<SignInFailureKind | null>(null);
  const [diagnostic, setDiagnostic] = useState<DiagnosticRecord | null>(null);

  const schema = useMemo(
    () =>
      z.object({
        email: z.string().trim().min(1, t("auth.validation.emailRequired")),
        password: z.string().min(1, t("auth.validation.passwordRequired")),
      }),
    [t],
  );

  const form = useForm<SignInValues>({
    defaultValues: { email: "", password: "" },
    resolver: async (values, context, options) => zodResolver(schema)(values, context, options),
  });

  const emailId = "sign-in-email";
  const passwordId = "sign-in-password";
  const emailError = form.formState.errors.email?.message;
  const passwordError = form.formState.errors.password?.message;
  const submitting = form.formState.isSubmitting;
  const sessionExpired = params.get("notice") === "session-expired";

  async function onSubmit(values: SignInValues) {
    setFormError(null);
    setDiagnostic(null);
    try {
      await signIn(values.email, values.password);
      navigate(resolvePostLoginPath(params.get("return")), { replace: true });
    } catch (error) {
      const kind = classifySignInFailure(error);
      if (
        kind === "invalid_credentials" ||
        kind === "account_locked" ||
        kind === "account_disabled"
      ) {
        setFormError(kind);
        form.setValue("password", "");
        return;
      }
      setDiagnostic(
        normalizeDiagnosticError({
          error,
          operation: "Sign in",
          category: kind === "network" ? "NETWORK" : "UNKNOWN",
          environment: { locale: language, theme, density },
        }),
      );
    }
  }

  return (
    <Card className="overflow-hidden">
      <header className="mb-5">
        <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-wide text-primary uppercase">
          ExItS
        </p>
        <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("auth.product")} · {t("auth.productSubtitle")}
        </p>
        <h1 className="mt-3 text-[length:var(--exits-text-xl)] font-bold">{t("auth.signIn")}</h1>
      </header>

      {sessionExpired ? <Alert className="mb-4" title={t("auth.notice.sessionExpired")} /> : null}

      {formError ? (
        <Alert className="mb-4" tone="danger" title={t(failureMessageKey(formError))} />
      ) : null}

      {diagnostic ? (
        <div className="mb-4">
          <ErrorState
            diagnostic={diagnostic}
            description={
              diagnostic.category === "NETWORK" ? t("auth.error.network") : t("auth.error.unknown")
            }
          />
        </div>
      ) : null}

      <form className="grid gap-4" onSubmit={form.handleSubmit(onSubmit)} noValidate>
        <div className="grid gap-1.5">
          <Label htmlFor={emailId}>{t("auth.email")}</Label>
          <Input
            id={emailId}
            type="text"
            inputMode="email"
            autoComplete="username"
            autoFocus
            disabled={submitting}
            aria-invalid={Boolean(emailError)}
            aria-describedby={emailError ? `${emailId}-error` : undefined}
            {...form.register("email")}
          />
          {emailError ? (
            <p
              id={`${emailId}-error`}
              className="text-[length:var(--exits-text-sm)] text-destructive"
            >
              {emailError}
            </p>
          ) : null}
        </div>

        <div className="grid gap-1.5">
          <Label htmlFor={passwordId}>{t("auth.password")}</Label>
          <div className="relative">
            <Input
              id={passwordId}
              type={passwordVisible ? "text" : "password"}
              autoComplete="current-password"
              disabled={submitting}
              className="pr-12"
              aria-invalid={Boolean(passwordError)}
              aria-describedby={passwordError ? `${passwordId}-error` : undefined}
              {...form.register("password")}
            />
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="absolute top-1/2 right-1 min-h-11 -translate-y-1/2 px-2"
              aria-pressed={passwordVisible}
              aria-label={passwordVisible ? t("auth.hidePassword") : t("auth.showPassword")}
              onClick={() => setPasswordVisible((current) => !current)}
            >
              {passwordVisible ? (
                <EyeOff aria-hidden="true" size={18} />
              ) : (
                <Eye aria-hidden="true" size={18} />
              )}
            </Button>
          </div>
          {passwordError ? (
            <p
              id={`${passwordId}-error`}
              className="text-[length:var(--exits-text-sm)] text-destructive"
            >
              {passwordError}
            </p>
          ) : null}
        </div>

        <Button type="submit" disabled={submitting} aria-busy={submitting}>
          {submitting ? t("auth.submitting") : t("auth.signIn")}
        </Button>
      </form>

      <div className="mt-4 flex flex-col gap-2 text-[length:var(--exits-text-sm)]">
        <Link
          className="text-primary underline-offset-4 hover:underline"
          to="/admin/forgot-password"
        >
          {t("auth.forgotPassword")}
        </Link>
        <Link
          className="text-primary break-words underline-offset-4 hover:underline"
          to="/admin/register"
        >
          {t("auth.createAccount")}
        </Link>
      </div>

      <DevelopmentTestUserTools
        onSelectLogin={(loginId) => {
          form.setValue("email", loginId, { shouldValidate: true });
        }}
      />
    </Card>
  );
}
