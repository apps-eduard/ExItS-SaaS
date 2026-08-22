import { Eye, EyeOff } from "lucide-react";
import { useState, type ReactNode } from "react";
import type { UseFormRegisterReturn } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { usePreferences } from "@/hooks/use-preferences";
import { isLocalValidationToolsEnabled } from "@/lib/env";

type AuthNewPasswordFieldsProps = {
  passwordId: string;
  confirmId: string;
  passwordField: UseFormRegisterReturn;
  confirmField: UseFormRegisterReturn;
  passwordError?: string;
  confirmError?: string;
  disabled?: boolean;
  describedBy?: string;
};

export function AuthNewPasswordFields({
  passwordId,
  confirmId,
  passwordField,
  confirmField,
  passwordError,
  confirmError,
  disabled,
  describedBy,
}: AuthNewPasswordFieldsProps) {
  const { t } = usePreferences();
  const [passwordVisible, setPasswordVisible] = useState(false);
  const [confirmVisible, setConfirmVisible] = useState(false);
  const showWeakPasswordNotice = isLocalValidationToolsEnabled();

  return (
    <>
      {showWeakPasswordNotice ? (
        <p className="text-[length:var(--exits-text-xs)] text-muted">
          {t("auth.localValidation.weakPasswordNotice")}
        </p>
      ) : null}
      <PasswordInput
        id={passwordId}
        label={t("auth.newPassword")}
        autoComplete="new-password"
        visible={passwordVisible}
        onToggleVisible={() => setPasswordVisible((current) => !current)}
        field={passwordField}
        error={passwordError}
        disabled={disabled}
        describedBy={describedBy}
        showLabel={t("auth.showNewPassword")}
        hideLabel={t("auth.hideNewPassword")}
      />
      <PasswordInput
        id={confirmId}
        label={t("auth.confirmPassword")}
        autoComplete="new-password"
        visible={confirmVisible}
        onToggleVisible={() => setConfirmVisible((current) => !current)}
        field={confirmField}
        error={confirmError}
        disabled={disabled}
        showLabel={t("auth.showConfirmPassword")}
        hideLabel={t("auth.hideConfirmPassword")}
      />
    </>
  );
}

function PasswordInput({
  id,
  label,
  autoComplete,
  visible,
  onToggleVisible,
  field,
  error,
  disabled,
  describedBy,
  showLabel,
  hideLabel,
}: {
  id: string;
  label: string;
  autoComplete: string;
  visible: boolean;
  onToggleVisible: () => void;
  field: UseFormRegisterReturn;
  error?: string;
  disabled?: boolean;
  describedBy?: string;
  showLabel: string;
  hideLabel: string;
}): ReactNode {
  const describedByIds = [error ? `${id}-error` : undefined, describedBy].filter(Boolean).join(" ");

  return (
    <div className="grid gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <div className="relative">
        <Input
          id={id}
          type={visible ? "text" : "password"}
          autoComplete={autoComplete}
          disabled={disabled}
          className="pr-12"
          aria-invalid={Boolean(error)}
          aria-describedby={describedByIds.length > 0 ? describedByIds : undefined}
          {...field}
        />
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="absolute top-1/2 right-1 h-8 min-h-8 w-8 -translate-y-1/2 px-0"
          aria-pressed={visible}
          aria-label={visible ? hideLabel : showLabel}
          onClick={onToggleVisible}
        >
          {visible ? <EyeOff aria-hidden="true" size={18} /> : <Eye aria-hidden="true" size={18} />}
        </Button>
      </div>
      {error ? (
        <p id={`${id}-error`} className="text-[length:var(--exits-text-sm)] text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}
