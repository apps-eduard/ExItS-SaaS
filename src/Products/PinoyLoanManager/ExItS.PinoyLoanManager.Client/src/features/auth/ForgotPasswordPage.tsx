import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import { requestPasswordReset } from "@/api/platform-auth/platform-auth-client";
import { Button } from "@/components/ui/button";
import { TextField } from "@/components/ui/text-field";
import { AuthLayout } from "@/features/auth/AuthLayout";
import {
  forgotPasswordSchema,
  zodResolver,
  type ForgotPasswordValues,
} from "@/features/sign-in/sign-in-schema";
import { useI18n } from "@/i18n/I18nProvider";

export function ForgotPasswordPage() {
  const { t } = useI18n();
  const {
    register,
    handleSubmit,
    setFocus,
    formState: { errors, isSubmitting, isSubmitSuccessful },
  } = useForm<ForgotPasswordValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { usernameOrEmail: "" },
  });

  if (isSubmitSuccessful) {
    return (
      <AuthLayout title={t("auth.forgotTitle")}>
        <p className="mt-4 mb-0 text-[length:var(--exits-text-sm)]" role="status">
          {t("auth.forgotAck")}
        </p>
        <p className="mt-6 mb-0 text-[length:var(--exits-text-sm)]">
          <Link className="text-muted underline-offset-4 hover:underline" to="/sign-in">
            {t("auth.backToSignIn")}
          </Link>
        </p>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title={t("auth.forgotTitle")}>
      <form
        className="mt-6 flex flex-col gap-4"
        noValidate
        onSubmit={handleSubmit(
          async (values) => {
            await requestPasswordReset(values.usernameOrEmail);
          },
          (formErrors) => {
            const first = Object.keys(formErrors)[0] as keyof ForgotPasswordValues | undefined;
            if (first) {
              setFocus(first);
            }
          },
        )}
      >
        <TextField
          label={t("auth.usernameOrEmail")}
          autoComplete="username"
          autoCapitalize="none"
          spellCheck={false}
          error={errors.usernameOrEmail ? t("auth.fieldRequired") : undefined}
          {...register("usernameOrEmail")}
        />
        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? t("auth.sendingReset") : t("auth.sendReset")}
        </Button>
        <p className="m-0 text-[length:var(--exits-text-sm)]">
          <Link className="text-muted underline-offset-4 hover:underline" to="/sign-in">
            {t("auth.backToSignIn")}
          </Link>
        </p>
      </form>
    </AuthLayout>
  );
}
