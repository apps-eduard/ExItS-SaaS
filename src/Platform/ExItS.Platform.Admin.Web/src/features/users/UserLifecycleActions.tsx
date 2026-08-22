import { useState } from "react";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { PlatformApiError } from "@/api/platform-http";
import type { PlatformUserDetail } from "@/api/users/user-types";
import {
  deactivatePlatformUser,
  movePlatformUserToSuspended,
  reactivatePlatformUser,
  suspendPlatformUser,
} from "@/api/users/user-mutations";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";
import type { MessageKey } from "@/lib/i18n/messages";

type LifecycleAction =
  | "suspend"
  | "globalSuspend"
  | "deactivate"
  | "reactivate"
  | "globalReactivate"
  | "reactivateFromDeactivated"
  | "moveToSuspended";

type ModalConfig = {
  action: LifecycleAction;
  titleKey: MessageKey;
  requireReason: boolean;
  requireStepUp: boolean;
  global?: boolean;
};

function isPlatformAccount(user: PlatformUserDetail): boolean {
  return user.accountClasses.some((value) => value.toLowerCase() === "platform");
}

function actionsForUser(user: PlatformUserDetail): ModalConfig[] {
  const platform = isPlatformAccount(user);
  if (user.status === "Active") {
    if (platform) {
      return [
        {
          action: "suspend",
          titleKey: "users.lifecycle.suspend",
          requireReason: false,
          requireStepUp: false,
        },
        {
          action: "deactivate",
          titleKey: "users.lifecycle.deactivate",
          requireReason: true,
          requireStepUp: true,
        },
      ];
    }
    return [
      {
        action: "globalSuspend",
        titleKey: "users.lifecycle.globalSuspend",
        requireReason: true,
        requireStepUp: false,
        global: true,
      },
    ];
  }
  if (user.status === "Suspended") {
    if (platform) {
      return [
        {
          action: "reactivate",
          titleKey: "users.lifecycle.reactivate",
          requireReason: false,
          requireStepUp: false,
        },
        {
          action: "deactivate",
          titleKey: "users.lifecycle.deactivate",
          requireReason: true,
          requireStepUp: true,
        },
      ];
    }
    return [
      {
        action: "globalReactivate",
        titleKey: "users.lifecycle.globalReactivate",
        requireReason: false,
        requireStepUp: false,
        global: true,
      },
    ];
  }
  if (user.status === "Deactivated") {
    return [
      {
        action: "reactivateFromDeactivated",
        titleKey: "users.lifecycle.reactivateFromDeactivated",
        requireReason: true,
        requireStepUp: true,
      },
      {
        action: "moveToSuspended",
        titleKey: "users.lifecycle.moveToSuspended",
        requireReason: true,
        requireStepUp: true,
      },
    ];
  }
  return [];
}

export function UserLifecycleActions({
  user,
  onUpdated,
}: {
  user: PlatformUserDetail;
  onUpdated: (next: PlatformUserDetail) => void;
}) {
  const { t } = usePreferences();
  const configs = actionsForUser(user);
  const [modal, setModal] = useState<ModalConfig | null>(null);
  const [reason, setReason] = useState("");
  const [actorPassword, setActorPassword] = useState("");
  const [mfaCode, setMfaCode] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  function closeModal() {
    setModal(null);
    setReason("");
    setActorPassword("");
    setMfaCode("");
    setError(null);
  }

  async function runSimpleConfirm(config: ModalConfig) {
    if (!window.confirm(`${t(config.titleKey)} — ${t("users.lifecycle.confirmHint")}`)) {
      return;
    }
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const next = await executeLifecycle(user.id, config, {
        reason: null,
        actorPassword: null,
        mfaCode: null,
      });
      onUpdated(next);
      setSuccess(t("users.lifecycle.success"));
    } catch (err) {
      setError(formatError(err, t("users.lifecycle.failed")));
    } finally {
      setBusy(false);
    }
  }

  async function submitModal() {
    if (!modal || busy) {
      return;
    }
    if (modal.requireReason && !reason.trim()) {
      setError(t("users.lifecycle.validation.reason"));
      return;
    }
    if (modal.requireStepUp && !actorPassword.trim()) {
      setError(t("users.lifecycle.validation.actorPassword"));
      return;
    }
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const next = await executeLifecycle(user.id, modal, {
        reason: reason.trim() || null,
        actorPassword: actorPassword.trim() || null,
        mfaCode: mfaCode.trim() || null,
      });
      onUpdated(next);
      setSuccess(t("users.lifecycle.success"));
      closeModal();
    } catch (err) {
      setError(formatError(err, t("users.lifecycle.failed")));
    } finally {
      setBusy(false);
    }
  }

  if (configs.length === 0) {
    return null;
  }

  return (
    <div className="grid gap-3" data-testid="users-lifecycle-actions">
      {success ? (
        <Alert title={success} tone="success" data-testid="users-lifecycle-success" />
      ) : null}
      {error && !modal ? (
        <Alert title={error} tone="danger" data-testid="users-lifecycle-error" />
      ) : null}
      <div className="flex flex-wrap gap-2">
        {configs.map((config) => {
          const needsModal = config.requireReason || config.requireStepUp;
          return (
            <Button
              key={config.action}
              type="button"
              size="sm"
              variant={config.action.includes("deactivate") ? "destructive" : "outline"}
              disabled={busy}
              data-testid={`users-lifecycle-${config.action}`}
              onClick={() => {
                setSuccess(null);
                if (needsModal) {
                  setModal(config);
                  setError(null);
                } else {
                  void runSimpleConfirm(config);
                }
              }}
            >
              {t(config.titleKey)}
            </Button>
          );
        })}
      </div>

      <DialogPrimitive.Root
        open={modal != null}
        onOpenChange={(open) => {
          if (!open && !busy) {
            closeModal();
          }
        }}
      >
        <DialogPrimitive.Portal>
          <DialogPrimitive.Overlay className="fixed inset-0 z-[var(--exits-z-overlay)] bg-[var(--exits-overlay)]" />
          <DialogPrimitive.Content
            className="fixed left-1/2 top-1/2 z-[var(--exits-z-drawer)] grid w-[min(28rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4 shadow-lg outline-none"
            data-testid="users-lifecycle-modal"
          >
            <DialogPrimitive.Title className="text-[length:var(--exits-text-lg)] font-bold">
              {modal ? t(modal.titleKey) : ""}
            </DialogPrimitive.Title>
            <DialogPrimitive.Description className="text-[length:var(--exits-text-sm)] text-muted">
              {t("users.lifecycle.modalHint")}
            </DialogPrimitive.Description>
            {error ? (
              <Alert title={error} tone="danger" data-testid="users-lifecycle-modal-error" />
            ) : null}
            {modal?.requireReason ? (
              <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="lifecycle-reason">
                {t("users.lifecycle.reason")}
                <Input
                  id="lifecycle-reason"
                  data-testid="users-lifecycle-reason"
                  value={reason}
                  disabled={busy}
                  onChange={(event) => setReason(event.target.value)}
                />
              </label>
            ) : null}
            {modal?.requireStepUp ? (
              <>
                <label
                  className="grid gap-1 text-[length:var(--exits-text-sm)]"
                  htmlFor="lifecycle-actor-password"
                >
                  {t("users.lifecycle.actorPassword")}
                  <Input
                    id="lifecycle-actor-password"
                    type="password"
                    data-testid="users-lifecycle-actor-password"
                    value={actorPassword}
                    disabled={busy}
                    autoComplete="current-password"
                    onChange={(event) => setActorPassword(event.target.value)}
                  />
                </label>
                <label
                  className="grid gap-1 text-[length:var(--exits-text-sm)]"
                  htmlFor="lifecycle-mfa"
                >
                  {t("users.lifecycle.mfaCode")}
                  <Input
                    id="lifecycle-mfa"
                    data-testid="users-lifecycle-mfa"
                    value={mfaCode}
                    disabled={busy}
                    onChange={(event) => setMfaCode(event.target.value)}
                  />
                  <span className="text-[length:var(--exits-text-xs)] text-muted">
                    {t("users.lifecycle.mfaHint")}
                  </span>
                </label>
              </>
            ) : null}
            <div className="flex flex-wrap justify-end gap-2">
              <Button type="button" size="sm" variant="outline" disabled={busy} onClick={closeModal}>
                {t("users.lifecycle.cancel")}
              </Button>
              <Button
                type="button"
                size="sm"
                disabled={busy}
                data-testid="users-lifecycle-confirm"
                onClick={() => void submitModal()}
              >
                {busy ? t("users.lifecycle.working") : t("users.lifecycle.confirmSubmit")}
              </Button>
            </div>
          </DialogPrimitive.Content>
        </DialogPrimitive.Portal>
      </DialogPrimitive.Root>
    </div>
  );
}

async function executeLifecycle(
  userId: string,
  config: ModalConfig,
  body: { reason: string | null; actorPassword: string | null; mfaCode: string | null },
): Promise<PlatformUserDetail> {
  const global = config.global === true;
  switch (config.action) {
    case "suspend":
    case "globalSuspend":
      return suspendPlatformUser(env.platformApiBaseUrl, userId, {
        reason: body.reason,
        global,
        actorPassword: body.actorPassword,
        mfaCode: body.mfaCode,
      });
    case "deactivate":
      return deactivatePlatformUser(env.platformApiBaseUrl, userId, {
        reason: body.reason,
        global: false,
        actorPassword: body.actorPassword,
        mfaCode: body.mfaCode,
      });
    case "reactivate":
    case "globalReactivate":
    case "reactivateFromDeactivated":
      return reactivatePlatformUser(env.platformApiBaseUrl, userId, {
        reason: body.reason,
        global,
        actorPassword: body.actorPassword,
        mfaCode: body.mfaCode,
      });
    case "moveToSuspended":
      return movePlatformUserToSuspended(env.platformApiBaseUrl, userId, {
        reason: body.reason,
        actorPassword: body.actorPassword,
        mfaCode: body.mfaCode,
      });
    default:
      throw new Error("Unsupported lifecycle action.");
  }
}

function formatError(err: unknown, fallback: string): string {
  if (err instanceof PlatformApiError) {
    return err.problem.detail ?? err.message ?? fallback;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return fallback;
}
