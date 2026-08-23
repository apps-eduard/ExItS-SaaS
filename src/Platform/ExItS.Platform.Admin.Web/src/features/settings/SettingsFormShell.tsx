import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";

export function SettingsFormShell({
  children,
  dirty,
  saving,
  saveLabel,
  dirtyMessage,
  onSave,
  successMessage,
  errorMessage,
}: {
  children: ReactNode;
  dirty: boolean;
  saving: boolean;
  saveLabel: string;
  dirtyMessage?: string;
  onSave: () => void;
  successMessage?: string | null;
  errorMessage?: string | null;
}) {
  return (
    <form
      className="grid max-w-2xl gap-4 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-4"
      onSubmit={(event) => {
        event.preventDefault();
        onSave();
      }}
    >
      {children}
      {dirty && dirtyMessage ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
          {dirtyMessage}
        </p>
      ) : null}
      {successMessage ? (
        <p className="text-[length:var(--exits-text-sm)] text-foreground" role="status">
          {successMessage}
        </p>
      ) : null}
      {errorMessage ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted" role="alert">
          {errorMessage}
        </p>
      ) : null}
      <div>
        <Button disabled={!dirty || saving} type="submit">
          {saving ? "Saving…" : saveLabel}
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
}: {
  label: string;
  htmlFor: string;
  children: ReactNode;
  hint?: string;
}) {
  return (
    <label className="grid gap-1" htmlFor={htmlFor}>
      <span className="text-[length:var(--exits-text-sm)] font-medium">{label}</span>
      {children}
      {hint ? <span className="text-[length:var(--exits-text-xs)] text-muted">{hint}</span> : null}
    </label>
  );
}
