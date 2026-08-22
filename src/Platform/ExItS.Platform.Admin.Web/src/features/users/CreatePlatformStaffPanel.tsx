import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { PlatformApiError } from "@/api/platform-http";
import { createPlatformUser } from "@/api/users/user-mutations";
import { platformUserDetailHref } from "@/api/users/user-id";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";
import type { MessageKey } from "@/lib/i18n/messages";

const PLATFORM_ROLES = [
  "PlatformAdministrator",
  "BillingAdministrator",
  "PlatformSupport",
] as const;

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

export function CreatePlatformStaffPanel({ onCreated }: { onCreated?: () => void }) {
  const { t } = usePreferences();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<{ title: string; detail?: string; conflict?: boolean } | null>(
    null,
  );
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [username, setUsername] = useState("");
  const [phone, setPhone] = useState("");
  const [employeeCode, setEmployeeCode] = useState("");
  const [platformRole, setPlatformRole] = useState<(typeof PLATFORM_ROLES)[number] | "">("");
  const [requireEmailVerification, setRequireEmailVerification] = useState(false);
  const [initialPassword, setInitialPassword] = useState("");

  function resetForm() {
    setFirstName("");
    setLastName("");
    setDisplayName("");
    setEmail("");
    setUsername("");
    setPhone("");
    setEmployeeCode("");
    setPlatformRole("");
    setRequireEmailVerification(false);
    setInitialPassword("");
    setError(null);
  }

  function validateClient(): string | null {
    if (!displayName.trim()) {
      return t("users.create.validation.displayName");
    }
    if (!email.trim()) {
      return t("users.create.validation.email");
    }
    if (!firstName.trim() || !lastName.trim()) {
      return t("users.create.validation.name");
    }
    if (!platformRole) {
      return t("users.create.validation.platformRole");
    }
    if (requireEmailVerification && !initialPassword.trim()) {
      return t("users.create.validation.initialPassword");
    }
    if (phone.trim().length > 32) {
      return t("users.create.validation.phone");
    }
    if (employeeCode.trim().length > 64) {
      return t("users.create.validation.employeeCode");
    }
    return null;
  }

  async function handleCreate() {
    if (busy) {
      return;
    }
    const clientError = validateClient();
    if (clientError) {
      setError({ title: clientError });
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const created = await createPlatformUser(env.platformApiBaseUrl, {
        displayName: displayName.trim(),
        email: email.trim(),
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        username: username.trim() || null,
        phone: phone.trim() || null,
        employeeCode: employeeCode.trim() || null,
        platformRole,
        requireEmailVerification,
        sendEmailVerification: requireEmailVerification,
        initialPassword: requireEmailVerification ? initialPassword : null,
      });
      await queryClient.invalidateQueries({ queryKey: ["users", "list"] });
      onCreated?.();
      resetForm();
      setOpen(false);
      navigate(platformUserDetailHref(created.id));
    } catch (err) {
      const conflict = err instanceof PlatformApiError && err.status === 409;
      setError({
        title: conflict ? t("users.create.conflict") : t("users.create.failed"),
        detail:
          err instanceof PlatformApiError
            ? (err.problem.detail ?? err.message)
            : err instanceof Error
              ? err.message
              : undefined,
        conflict,
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="grid gap-3" data-testid="users-create-staff-panel">
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          size="sm"
          variant={open ? "outline" : "default"}
          data-testid="users-toggle-create"
          onClick={() => {
            if (open && (displayName || email || firstName || lastName)) {
              if (!window.confirm(t("users.create.discardConfirm"))) {
                return;
              }
            }
            setOpen((value) => !value);
            if (open) {
              resetForm();
            }
          }}
        >
          {open ? t("users.create.hide") : t("users.create.show")}
        </Button>
      </div>

      {open ? (
        <div
          className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4"
          data-testid="users-create-form"
        >
          <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
            {t("users.create.title")}
          </h2>
          {error ? (
            <Alert
              title={error.title}
              tone="danger"
              data-testid={error.conflict ? "users-create-conflict" : "users-create-error"}
            >
              {error.detail}
            </Alert>
          ) : null}
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="staff-first-name">
              {t("users.detail.field.firstName")}
              <Input
                id="staff-first-name"
                data-testid="users-create-first-name"
                value={firstName}
                disabled={busy}
                onChange={(event) => setFirstName(event.target.value)}
              />
            </label>
            <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="staff-last-name">
              {t("users.detail.field.lastName")}
              <Input
                id="staff-last-name"
                data-testid="users-create-last-name"
                value={lastName}
                disabled={busy}
                onChange={(event) => setLastName(event.target.value)}
              />
            </label>
            <label
              className="grid gap-1 text-[length:var(--exits-text-sm)] sm:col-span-2"
              htmlFor="staff-display-name"
            >
              {t("users.column.displayName")}
              <Input
                id="staff-display-name"
                data-testid="users-create-display-name"
                value={displayName}
                disabled={busy}
                onChange={(event) => setDisplayName(event.target.value)}
              />
            </label>
            <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="staff-email">
              {t("users.column.email")}
              <Input
                id="staff-email"
                type="email"
                data-testid="users-create-email"
                value={email}
                disabled={busy}
                onChange={(event) => setEmail(event.target.value)}
              />
            </label>
            <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="staff-username">
              {t("users.column.username")}
              <Input
                id="staff-username"
                data-testid="users-create-username"
                value={username}
                disabled={busy}
                onChange={(event) => setUsername(event.target.value)}
              />
            </label>
            <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="staff-phone">
              {t("users.detail.field.phone")}
              <Input
                id="staff-phone"
                data-testid="users-create-phone"
                value={phone}
                disabled={busy}
                onChange={(event) => setPhone(event.target.value)}
              />
            </label>
            <label
              className="grid gap-1 text-[length:var(--exits-text-sm)]"
              htmlFor="staff-employee-code"
            >
              {t("users.detail.field.employeeCode")}
              <Input
                id="staff-employee-code"
                data-testid="users-create-employee-code"
                value={employeeCode}
                disabled={busy}
                onChange={(event) => setEmployeeCode(event.target.value)}
              />
            </label>
            <label
              className="grid gap-1 text-[length:var(--exits-text-sm)] sm:col-span-2"
              htmlFor="staff-platform-role"
            >
              {t("users.create.platformRole")}
              <select
                id="staff-platform-role"
                data-testid="users-create-platform-role"
                className={controlClass}
                value={platformRole}
                disabled={busy}
                onChange={(event) =>
                  setPlatformRole(event.target.value as (typeof PLATFORM_ROLES)[number] | "")
                }
              >
                <option value="">{t("users.create.platformRole.placeholder")}</option>
                {PLATFORM_ROLES.map((role) => {
                  const key = `users.detail.role.${role}` as MessageKey;
                  return (
                    <option key={role} value={role}>
                      {t(key)}
                    </option>
                  );
                })}
              </select>
            </label>
            <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)] sm:col-span-2">
              <input
                type="checkbox"
                data-testid="users-create-require-verification"
                checked={requireEmailVerification}
                disabled={busy}
                onChange={(event) => setRequireEmailVerification(event.target.checked)}
              />
              {t("users.create.requireEmailVerification")}
            </label>
            {requireEmailVerification ? (
              <label
                className="grid gap-1 text-[length:var(--exits-text-sm)] sm:col-span-2"
                htmlFor="staff-initial-password"
              >
                {t("users.create.initialPassword")}
                <Input
                  id="staff-initial-password"
                  type="password"
                  data-testid="users-create-initial-password"
                  value={initialPassword}
                  disabled={busy}
                  autoComplete="new-password"
                  onChange={(event) => setInitialPassword(event.target.value)}
                />
              </label>
            ) : null}
          </div>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              size="sm"
              disabled={busy}
              data-testid="users-create-submit"
              onClick={() => void handleCreate()}
            >
              {busy ? t("users.create.creating") : t("users.create.submit")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
