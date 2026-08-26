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
        "min-w-0 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3",
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
  className,
  fieldsClassName,
}: {
  title: string;
  description?: string;
  children: ReactNode;
  className?: string;
  fieldsClassName?: string;
}) {
  return (
    <fieldset
      className={cn(
        "grid min-w-0 gap-2 border-0 border-t border-border p-0 pt-4 first:border-t-0 first:pt-0",
        className,
      )}
    >
      <legend className="text-[length:var(--exits-text-sm)] font-semibold text-foreground">
        {title}
      </legend>
      {description ? (
        <p className="-mt-0.5 text-[length:var(--exits-text-xs)] text-muted">{description}</p>
      ) : null}
      <div className={cn("grid gap-3 sm:grid-cols-2 sm:items-start", fieldsClassName)}>
        {children}
      </div>
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
    <form
      className="grid min-w-0 gap-3"
      onSubmit={(event) => {
        event.preventDefault();
        if (saveEnabled) {
          onSave();
        }
      }}
    >
      <fieldset className="grid min-w-0 gap-3 border-0 p-0" disabled={saving}>
        {children}
      </fieldset>
      <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border pt-3">
        <div className="min-h-[1.125rem] min-w-0 flex-1">
          {statusMessage ? (
            <p
              className={cn(
                "text-[length:var(--exits-text-xs)]",
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
          className="shrink-0"
          disabled={!saveEnabled}
          size="sm"
          type="submit"
        >
          {saving ? t("settings.saving") : saveLabel}
        </Button>
      </div>
    </form>
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
    <div className={cn("grid min-w-0 gap-1 self-start", className)}>
      <label className="grid min-w-0 gap-1" htmlFor={htmlFor}>
        <span className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
          {label}
        </span>
        {children}
      </label>
      <span className="block min-h-[2.5rem]">
        {hint ? (
          <span className="text-[length:var(--exits-text-xs)] text-muted" id={hintId}>
            {hint}
          </span>
        ) : null}
      </span>
    </div>
  );
}
