import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import { z } from "zod";
import { registerPersonalAccount } from "@/api/auth/auth-client";
import { classifyCredentialWorkflowFailure } from "@/api/auth/auth-errors";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ErrorState } from "@/components/exits/ErrorState";
import { MailpitConvenienceHint } from "@/features/auth/MailpitConvenienceHint";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

type RegisterValues = {
  displayName: string;
  email: string;
};

export function RegisterPage() {
  const { t, language, theme, density } = usePreferences();
  const [succeeded, setSucceeded] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [diagnostic, setDiagnostic] = useState<DiagnosticRecord | null>(null);

  const schema = useMemo(
    () =>
      z.object({
        displayName: z
          .string()
          .trim()
          .min(1, t("auth.validation.displayNameRequired"))
          .min(2, t("auth.validation.displayNameLength")),
        email: z
          .string()
          .trim()
          .min(1, t("auth.validation.emailRequired"))
          .email(t("auth.validation.emailInvalid")),
      }),
    [t],
  );

  const form = useForm<RegisterValues>({
    defaultValues: { displayName: "", email: "" },
    resolver: async (values, context, options) => zodResolver(schema)(values, context, options),
  });

  const displayNameId = "register-display-name";
  const emailId = "register-email";
  const displayNameError = form.formState.errors.displayName?.message;
  const emailError = form.formState.errors.email?.message;
  const submitting = form.formState.isSubmitting;

  async function onSubmit(values: RegisterValues) {
    setFormError(null);
    setDiagnostic(null);
    try {
      await registerPersonalAccount(env.platformApiBaseUrl, {
        displayName: values.displayName.trim(),
        email: values.email.trim(),
      });
      setSucceeded(true);
    } catch (error) {
      const kind = classifyCredentialWorkflowFailure(error);
      if (kind === "email_conflict") {
        setSucceeded(true);
        return;
      }
      if (kind === "invalid_display_name") {
        form.setError("displayName", { message: t("auth.validation.displayNameLength") });
        return;
      }
      if (kind === "invalid_email") {
        form.setError("email", { message: t("auth.validation.emailInvalid") });
        return;
      }
      if (kind === "network") {
        setFormError(t("auth.error.network"));
        return;
      }
      setDiagnostic(
        normalizeDiagnosticError({
          error,
          operation: "Register personal account",
          category: "API",
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
            {t("auth.register.success.title")}
          </h1>
          <p className="mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("auth.register.success.body")}
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
        <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("auth.register.title")}</h1>
        <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("auth.register.hint")}
        </p>
      </header>

      {formError ? <Alert className="mb-4" tone="danger" title={formError} /> : null}

      {diagnostic ? (
        <div className="mb-4">
          <ErrorState diagnostic={diagnostic} description={t("auth.error.generic")} />
        </div>
      ) : null}

      <form className="grid gap-4" onSubmit={form.handleSubmit(onSubmit)} noValidate>
        <div className="grid gap-1.5">
          <Label htmlFor={displayNameId}>{t("auth.displayName")}</Label>
          <Input
            id={displayNameId}
            type="text"
            autoComplete="name"
            autoFocus
            disabled={submitting}
            aria-invalid={Boolean(displayNameError)}
            aria-describedby={displayNameError ? `${displayNameId}-error` : undefined}
            {...form.register("displayName")}
          />
          {displayNameError ? (
            <p
              id={`${displayNameId}-error`}
              className="text-[length:var(--exits-text-sm)] text-destructive"
            >
              {displayNameError}
            </p>
          ) : null}
        </div>

        <div className="grid gap-1.5">
          <Label htmlFor={emailId}>{t("auth.email")}</Label>
          <Input
            id={emailId}
            type="email"
            inputMode="email"
            autoComplete="email"
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

        <Button type="submit" className="mt-1 w-full" disabled={submitting} aria-busy={submitting}>
          {submitting ? t("auth.register.submitting") : t("auth.register.submit")}
        </Button>
      </form>

      <p className="mt-3 text-[length:var(--exits-text-sm)] text-muted">
        {t("auth.register.alreadyHaveAccount")}{" "}
        <Link className="text-primary underline-offset-4 hover:underline" to="/admin/login">
          {t("auth.signIn")}
        </Link>
      </p>
    </Card>
  );
}
