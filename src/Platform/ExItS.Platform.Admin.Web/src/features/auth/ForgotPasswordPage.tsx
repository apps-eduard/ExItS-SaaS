import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import { z } from "zod";
import { requestPasswordReset } from "@/api/auth/auth-client";
import { classifyCredentialWorkflowFailure } from "@/api/auth/auth-errors";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ErrorState } from "@/components/exits/ErrorState";
import { MailpitConvenienceHint } from "@/features/auth/MailpitConvenienceHint";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";
import {
  buildDiagnosticEnvironmentFromPreferences,
  normalizeDiagnosticError,
} from "@/lib/diagnostics/normalize-diagnostic-error";
import { shouldShowCredentialWorkflowDiagnostic } from "@/lib/diagnostics/auth-workflow-diagnostics";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

type ForgotPasswordValues = {
  usernameOrEmail: string;
};

export function ForgotPasswordPage() {
  const { t, language, theme, density } = usePreferences();
  const [succeeded, setSucceeded] = useState(false);
  const [diagnostic, setDiagnostic] = useState<DiagnosticRecord | null>(null);

  const schema = useMemo(
    () =>
      z.object({
        usernameOrEmail: z.string().trim().min(1, t("auth.validation.usernameOrEmailRequired")),
      }),
    [t],
  );

  const form = useForm<ForgotPasswordValues>({
    defaultValues: { usernameOrEmail: "" },
    resolver: async (values, context, options) => zodResolver(schema)(values, context, options),
  });

  const fieldId = "forgot-username-or-email";
  const fieldError = form.formState.errors.usernameOrEmail?.message;
  const submitting = form.formState.isSubmitting;

  async function onSubmit(values: ForgotPasswordValues) {
    setDiagnostic(null);
    try {
      await requestPasswordReset(env.platformApiBaseUrl, {
        usernameOrEmail: values.usernameOrEmail.trim(),
      });
      setSucceeded(true);
    } catch (error) {
      const kind = classifyCredentialWorkflowFailure(error);
      if (shouldShowCredentialWorkflowDiagnostic(kind)) {
        setDiagnostic(
          normalizeDiagnosticError({
            error,
            operation: "Request password reset",
            environment: buildDiagnosticEnvironmentFromPreferences({ locale: language, theme, density }),
          }),
        );
        return;
      }
      setDiagnostic(
        normalizeDiagnosticError({
          error,
          operation: "Request password reset",
          environment: buildDiagnosticEnvironmentFromPreferences({ locale: language, theme, density }),
        }),
      );
    }
  }

  if (succeeded) {
    return (
      <Card className="border-border">
        <header className="mb-4">
          <h1 tabIndex={-1} className="text-[length:var(--exits-text-xl)] font-bold">
            {t("auth.forgot.success.title")}
          </h1>
          <p className="mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("auth.forgot.success.body")}
          </p>
        </header>
        <MailpitConvenienceHint />
        <Link
          className="mt-4 inline-block text-primary underline-offset-4 hover:underline"
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
        <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("auth.forgot.title")}</h1>
        <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("auth.forgot.hint")}
        </p>
      </header>

      {diagnostic ? (
        <div className="mb-4">
          <ErrorState
            diagnostic={diagnostic}
            onRetry={() => void form.handleSubmit(onSubmit)()}
          />
        </div>
      ) : null}

      <form className="grid gap-4" onSubmit={form.handleSubmit(onSubmit)} noValidate>
        <div className="grid gap-1.5">
          <Label htmlFor={fieldId}>{t("auth.forgot.usernameOrEmail")}</Label>
          <Input
            id={fieldId}
            type="text"
            inputMode="email"
            autoComplete="username"
            autoFocus
            disabled={submitting}
            aria-invalid={Boolean(fieldError)}
            aria-describedby={fieldError ? `${fieldId}-error` : undefined}
            {...form.register("usernameOrEmail")}
          />
          {fieldError ? (
            <p
              id={`${fieldId}-error`}
              className="text-[length:var(--exits-text-sm)] text-destructive"
            >
              {fieldError}
            </p>
          ) : null}
        </div>

        <Button type="submit" className="mt-1 w-full" disabled={submitting} aria-busy={submitting}>
          {submitting ? t("auth.forgot.submitting") : t("auth.forgot.submit")}
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
