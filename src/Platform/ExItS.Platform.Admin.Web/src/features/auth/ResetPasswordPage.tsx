import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useSearchParams } from "react-router-dom";
import { resetPassword } from "@/api/auth/auth-client";
import { classifyCredentialWorkflowFailure } from "@/api/auth/auth-errors";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { AuthNewPasswordFields } from "@/features/auth/AuthNewPasswordFields";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";
import { buildAuthNewPasswordSchema } from "@/lib/auth/password-policy";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

type ResetValues = {
  password: string;
  confirmPassword: string;
};

function readToken(raw: string | null): string {
  return raw?.trim() ?? "";
}

export function ResetPasswordPage() {
  const { t, language, theme, density } = usePreferences();
  const [params] = useSearchParams();
  const token = readToken(params.get("token"));
  const [succeeded, setSucceeded] = useState(false);
  const [tokenAlert, setTokenAlert] = useState<string | null>(
    token.length === 0 ? t("auth.reset.token.invalid") : null,
  );
  const [formError, setFormError] = useState<string | null>(null);
  const [diagnostic, setDiagnostic] = useState<DiagnosticRecord | null>(null);

  const schema = useMemo(
    () =>
      buildAuthNewPasswordSchema(
        {
          passwordRequired: t("auth.validation.passwordRequired"),
          passwordMinLength: t("auth.validation.passwordMinLength"),
          passwordUppercase: t("auth.validation.passwordUppercase"),
          passwordLowercase: t("auth.validation.passwordLowercase"),
          passwordDigit: t("auth.validation.passwordDigit"),
          passwordSpecial: t("auth.validation.passwordSpecial"),
        },
        t("auth.validation.passwordMismatch"),
        t("auth.validation.confirmPasswordRequired"),
      ),
    [t],
  );

  const form = useForm<ResetValues>({
    defaultValues: { password: "", confirmPassword: "" },
    resolver: async (values, context, options) => zodResolver(schema)(values, context, options),
  });

  const passwordError = form.formState.errors.password?.message;
  const confirmError = form.formState.errors.confirmPassword?.message;
  const submitting = form.formState.isSubmitting;
  const tokenErrorId = "reset-token-error";

  async function onSubmit(values: ResetValues) {
    setFormError(null);
    setDiagnostic(null);
    if (token.length === 0) {
      setTokenAlert(t("auth.reset.token.invalid"));
      return;
    }
    try {
      await resetPassword(env.platformApiBaseUrl, {
        token,
        newPassword: values.password,
      });
      setSucceeded(true);
    } catch (error) {
      const kind = classifyCredentialWorkflowFailure(error);
      if (kind === "expired_token") {
        setTokenAlert(t("auth.reset.token.expired"));
        form.reset({ password: "", confirmPassword: "" });
        return;
      }
      if (kind === "invalid_token") {
        setTokenAlert(t("auth.reset.token.invalid"));
        form.reset({ password: "", confirmPassword: "" });
        return;
      }
      if (kind === "password_invalid") {
        const policy =
          error instanceof Error && error.message.trim().length > 0
            ? error.message
            : t("auth.validation.passwordRequired");
        form.setError("password", { message: policy });
        return;
      }
      if (kind === "network") {
        setFormError(t("auth.error.network"));
        return;
      }
      if (kind === "service_unavailable") {
        setFormError(t("auth.error.serviceUnavailable"));
        return;
      }
      if (kind === "rate_limited") {
        setFormError(t("auth.error.rateLimited"));
        return;
      }
      setDiagnostic(
        normalizeDiagnosticError({
          error,
          operation: "Reset password",
          category: kind === "unknown" ? "UNKNOWN" : "API",
          environment: { locale: language, theme, density },
        }),
      );
    }
  }

  if (succeeded) {
    return (
      <Card className="border-border">
        <header className="mb-4">
          <h1 tabIndex={-1} className="text-[length:var(--exits-text-xl)] font-bold">
            {t("auth.reset.success.title")}
          </h1>
          <p className="mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("auth.reset.success.body")}
          </p>
        </header>
        <Link
          className="inline-block text-primary underline-offset-4 hover:underline"
          to="/admin/login"
        >
          {t("auth.signIn")}
        </Link>
      </Card>
    );
  }

  return (
    <Card className="border-border">
      <header className="mb-4">
        <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("auth.reset.title")}</h1>
        <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">{t("auth.reset.hint")}</p>
      </header>

      {tokenAlert ? (
        <Alert id={tokenErrorId} className="mb-4" tone="danger" title={tokenAlert} />
      ) : null}

      {formError ? <Alert className="mb-4" tone="danger" title={formError} /> : null}

      {diagnostic ? (
        <div className="mb-4">
          <ErrorState diagnostic={diagnostic} description={t("auth.error.generic")} />
        </div>
      ) : null}

      <form className="grid gap-4" onSubmit={form.handleSubmit(onSubmit)} noValidate>
        <AuthNewPasswordFields
          passwordId="reset-password"
          confirmId="reset-confirm-password"
          passwordField={form.register("password")}
          confirmField={form.register("confirmPassword")}
          passwordError={passwordError}
          confirmError={confirmError}
          disabled={submitting || token.length === 0}
          describedBy={tokenAlert ? tokenErrorId : undefined}
        />
        <Button
          type="submit"
          className="mt-1 w-full"
          disabled={submitting || token.length === 0}
          aria-busy={submitting}
        >
          {submitting ? t("auth.reset.submitting") : t("auth.reset.submit")}
        </Button>
      </form>

      <Link
        className="mt-3 inline-block text-[length:var(--exits-text-sm)] text-primary underline-offset-4 hover:underline"
        to="/admin/login"
      >
        {t("auth.signIn")}
      </Link>
    </Card>
  );
}
