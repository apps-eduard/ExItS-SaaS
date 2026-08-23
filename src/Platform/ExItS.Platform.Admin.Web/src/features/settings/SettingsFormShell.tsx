import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

export function SettingsSectionCard({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <section
      className={cn(
        "grid gap-4 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-4 sm:px-5",
        className,
      )}
    >
      {children}
    </section>
  );
}

export function SettingsFieldGroup({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <fieldset className="grid min-w-0 gap-3 border-0 p-0">
      <legend className="text-[length:var(--exits-text-sm)] font-semibold text-foreground">
        {title}
      </legend>
      {description ? (
        <p className="-mt-1 text-[length:var(--exits-text-xs)] text-muted">{description}</p>
      ) : null}
      <div className="grid gap-3 sm:grid-cols-2">{children}</div>
    </fieldset>
  );
}

export function SettingsFormShell({
  children,
  dirty,
  saving,
  saveLabel,
  dirtyMessage,
  onSave,
  canSave,
  successMessage,
  errorMessage,
}: {
  children: ReactNode;
  dirty: boolean;
  saving: boolean;
  saveLabel: string;
  dirtyMessage?: string;
  onSave: () => void;
  canSave?: boolean;
  successMessage?: string | null;
  errorMessage?: string | null;
}) {
  const { t } = usePreferences();
  const saveEnabled = (canSave ?? dirty) && !saving;
  const statusMessage = errorMessage ?? (dirty ? dirtyMessage : successMessage) ?? null;
  const statusRole = errorMessage ? "alert" : "status";

  return (
    <SettingsSectionCard>
      <form
        className="grid gap-5"
        onSubmit={(event) => {
          event.preventDefault();
          if (saveEnabled) {
            onSave();
          }
        }}
      >
        <fieldset className="grid gap-5 border-0 p-0" disabled={saving}>
          {children}
        </fieldset>
        <div className="flex flex-col gap-3 border-t border-border pt-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="min-h-[1.25rem] min-w-0">
            {statusMessage ? (
              <p
                className={cn(
                  "text-[length:var(--exits-text-sm)]",
                  errorMessage ? "text-destructive" : "text-muted",
                  successMessage && !errorMessage && !dirty ? "text-foreground" : undefined,
                )}
                role={statusRole}
                aria-live="polite"
              >
                {statusMessage}
              </p>
            ) : null}
          </div>
          <Button
            aria-busy={saving}
            className="shrink-0 sm:min-w-[9rem]"
            disabled={!saveEnabled}
            type="submit"
          >
            {saving ? t("settings.saving") : saveLabel}
          </Button>
        </div>
      </form>
    </SettingsSectionCard>
  );
}

export function SettingsField({
  label,
  htmlFor,
  children,
  hint,
  className,
}: {
  label: string;
  htmlFor?: string;
  children: ReactNode;
  hint?: string;
  className?: string;
}) {
  const hintId = htmlFor && hint ? `${htmlFor}-hint` : undefined;
  return (
    <div className={cn("grid min-w-0 gap-1 sm:col-span-2", className)}>
      <label className="grid gap-1" htmlFor={htmlFor}>
        <span className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
          {label}
        </span>
        {children}
      </label>
      {hint ? (
        <span className="text-[length:var(--exits-text-xs)] text-muted" id={hintId}>
          {hint}
        </span>
      ) : null}
    </div>
  );
}
