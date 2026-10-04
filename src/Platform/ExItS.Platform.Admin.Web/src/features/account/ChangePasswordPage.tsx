import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { z } from "zod";
import { changePassword } from "@/api/auth/auth-client";
import { classifyCredentialWorkflowFailure } from "@/api/auth/auth-errors";
import { PlatformApiError } from "@/api/platform-http";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";
import { buildAuthPasswordFieldSchema } from "@/lib/auth/password-policy";
import { env } from "@/lib/env";

type ChangeValues = {
  currentPassword: string;
  newPassword: string;
};

export function ChangePasswordPage() {
  const { t } = usePreferences();
  const { forgetSession } = useSession();
  const navigate = useNavigate();
  const [formError, setFormError] = useState<string | null>(null);

  const schema = useMemo(
    () =>
      z.object({
        currentPassword: z.string().min(1, t("auth.validation.passwordRequired")),
        newPassword: buildAuthPasswordFieldSchema({
          passwordRequired: t("auth.validation.passwordRequired"),
          passwordMinLength: t("auth.validation.passwordMinLength"),
          passwordUppercase: t("auth.validation.passwordUppercase"),
          passwordLowercase: t("auth.validation.passwordLowercase"),
          passwordDigit: t("auth.validation.passwordDigit"),
          passwordSpecial: t("auth.validation.passwordSpecial"),
        }),
      }),
    [t],
  );

  const form = useForm<ChangeValues>({
    defaultValues: { currentPassword: "", newPassword: "" },
    resolver: async (values, context, options) => zodResolver(schema)(values, context, options),
  });

  async function onSubmit(values: ChangeValues) {
    setFormError(null);
    try {
      await changePassword(env.platformApiBaseUrl, {
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });
      form.reset({ currentPassword: "", newPassword: "" });
      sessionStorage.setItem("exits.admin.passwordChanged", "1");
      forgetSession();
      navigate("/admin/login", { replace: true });
    } catch (error) {
      form.reset({ currentPassword: "", newPassword: "" });
      const kind = classifyCredentialWorkflowFailure(error);
      if (kind === "password_invalid" && error instanceof PlatformApiError) {
        setFormError(error.message);
        return;
      }
      if (error instanceof PlatformApiError && error.problem.detail) {
        setFormError(error.problem.detail);
        return;
      }
      setFormError(t("account.changePassword.error"));
    }
  }

  const currentError = form.formState.errors.currentPassword?.message;
  const nextError = form.formState.errors.newPassword?.message;
  const submitting = form.formState.isSubmitting;

  return (
    <Card className="border-border">
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("account.changePassword.title")}</h1>
      <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">{t("account.changePassword.hint")}</p>
      {formError ? <Alert className="mt-4" tone="danger" title={formError} /> : null}
      <form className="mt-4 grid gap-4" onSubmit={form.handleSubmit(onSubmit)} noValidate>
        <div className="grid gap-1.5">
          <Label htmlFor="current-password">{t("account.changePassword.current")}</Label>
          <Input
            id="current-password"
            type="password"
            autoComplete="current-password"
            aria-invalid={Boolean(currentError)}
            {...form.register("currentPassword")}
          />
          {currentError ? <p className="text-[length:var(--exits-text-sm)] text-danger">{currentError}</p> : null}
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="new-password">{t("account.changePassword.next")}</Label>
          <Input
            id="new-password"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(nextError)}
            {...form.register("newPassword")}
          />
          {nextError ? <p className="text-[length:var(--exits-text-sm)] text-danger">{nextError}</p> : null}
        </div>
        <Button type="submit" disabled={submitting} aria-busy={submitting}>
          {submitting ? t("account.changePassword.submitting") : t("account.changePassword.submit")}
        </Button>
      </form>
      <Link className="mt-3 inline-block text-[length:var(--exits-text-sm)] text-primary" to="/admin/account">
        {t("account.back")}
      </Link>
    </Card>
  );
}
