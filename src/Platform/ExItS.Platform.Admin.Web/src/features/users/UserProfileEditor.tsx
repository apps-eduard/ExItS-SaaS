import { useState } from "react";
import { PlatformApiError } from "@/api/platform-http";
import type { PlatformUserDetail } from "@/api/users/user-types";
import { updatePlatformUser } from "@/api/users/user-mutations";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";

export function UserProfileEditor({
  user,
  onUpdated,
}: {
  user: PlatformUserDetail;
  onUpdated: (next: PlatformUserDetail) => void;
}) {
  const { t } = usePreferences();
  const [editing, setEditing] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState(user.displayName);
  const [email, setEmail] = useState(user.email);
  const [firstName, setFirstName] = useState(user.firstName ?? "");
  const [lastName, setLastName] = useState(user.lastName ?? "");
  const [phone, setPhone] = useState(user.phone ?? "");
  const [employeeCode, setEmployeeCode] = useState(user.employeeCode ?? "");

  function syncFromUser(next: PlatformUserDetail) {
    setDisplayName(next.displayName);
    setEmail(next.email);
    setFirstName(next.firstName ?? "");
    setLastName(next.lastName ?? "");
    setPhone(next.phone ?? "");
    setEmployeeCode(next.employeeCode ?? "");
  }

  async function handleSave() {
    if (busy) {
      return;
    }
    if (!displayName.trim()) {
      setError(t("users.profile.validation.displayName"));
      return;
    }
    if (!email.trim()) {
      setError(t("users.profile.validation.email"));
      return;
    }
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const next = await updatePlatformUser(env.platformApiBaseUrl, user.id, {
        displayName: displayName.trim(),
        email: email.trim(),
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        phone: phone.trim() || null,
        employeeCode: employeeCode.trim() || null,
      });
      onUpdated(next);
      syncFromUser(next);
      setSuccess(t("users.profile.success"));
      setEditing(false);
    } catch (err) {
      setError(
        err instanceof PlatformApiError
          ? (err.problem.detail ?? err.message)
          : err instanceof Error
            ? err.message
            : t("users.profile.failed"),
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="grid gap-3" data-testid="users-profile-editor">
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          size="sm"
          variant="outline"
          data-testid="users-profile-edit-toggle"
          disabled={busy}
          onClick={() => {
            if (editing) {
              syncFromUser(user);
              setError(null);
            }
            setEditing((value) => !value);
            setSuccess(null);
          }}
        >
          {editing ? t("users.profile.cancel") : t("users.profile.edit")}
        </Button>
      </div>
      {success ? (
        <Alert title={success} tone="success" data-testid="users-profile-success" />
      ) : null}
      {error ? <Alert title={error} tone="danger" data-testid="users-profile-error" /> : null}
      {editing ? (
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="profile-display-name">
            {t("users.column.displayName")}
            <Input
              id="profile-display-name"
              data-testid="users-profile-display-name"
              value={displayName}
              disabled={busy}
              onChange={(event) => setDisplayName(event.target.value)}
            />
          </label>
          <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="profile-email">
            {t("users.column.email")}
            <Input
              id="profile-email"
              type="email"
              data-testid="users-profile-email"
              value={email}
              disabled={busy}
              onChange={(event) => setEmail(event.target.value)}
            />
          </label>
          <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="profile-first-name">
            {t("users.detail.field.firstName")}
            <Input
              id="profile-first-name"
              data-testid="users-profile-first-name"
              value={firstName}
              disabled={busy}
              onChange={(event) => setFirstName(event.target.value)}
            />
          </label>
          <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="profile-last-name">
            {t("users.detail.field.lastName")}
            <Input
              id="profile-last-name"
              data-testid="users-profile-last-name"
              value={lastName}
              disabled={busy}
              onChange={(event) => setLastName(event.target.value)}
            />
          </label>
          <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="profile-phone">
            {t("users.detail.field.phone")}
            <Input
              id="profile-phone"
              data-testid="users-profile-phone"
              value={phone}
              disabled={busy}
              onChange={(event) => setPhone(event.target.value)}
            />
          </label>
          <label
            className="grid gap-1 text-[length:var(--exits-text-sm)]"
            htmlFor="profile-employee-code"
          >
            {t("users.detail.field.employeeCode")}
            <Input
              id="profile-employee-code"
              data-testid="users-profile-employee-code"
              value={employeeCode}
              disabled={busy}
              onChange={(event) => setEmployeeCode(event.target.value)}
            />
          </label>
          <div className="sm:col-span-2">
            <Button
              type="button"
              size="sm"
              disabled={busy}
              data-testid="users-profile-save"
              onClick={() => void handleSave()}
            >
              {busy ? t("users.profile.saving") : t("users.profile.save")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
