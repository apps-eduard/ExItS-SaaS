import { useEffect, useState, type FormEvent } from "react";
import { KeyRound } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import {
  listEligibleOfflinePinProfiles,
  type OfflinePinProfile,
} from "@/offline/offline-pin-profiles";
import { verifyOfflinePin } from "@/offline/offline-pin";
import { useSession } from "@/session/SessionProvider";

export function OfflinePinUnlockPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { coldStartGrant, prepareOfflinePinUnlock, unlockOfflinePin, status } = useSession();
  const [profiles, setProfiles] = useState<OfflinePinProfile[]>([]);
  const [selectedProfile, setSelectedProfile] = useState<OfflinePinProfile | null>(null);
  const [pin, setPin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void listEligibleOfflinePinProfiles().then((next) => {
      if (cancelled) {
        return;
      }
      setProfiles(next);
      if (next.length === 0) {
        setSelectedProfile(null);
        return;
      }
      const preferred =
        next.find((profile) => profile.userId === coldStartGrant?.userId) ?? next[0] ?? null;
      setSelectedProfile(preferred);
      if (preferred) {
        prepareOfflinePinUnlock(preferred.grant);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [coldStartGrant?.userId, prepareOfflinePinUnlock]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    const active = selectedProfile ?? profiles[0] ?? null;
    if (!active) {
      setError(t("offline.pin.grantMissing"));
      return;
    }
    prepareOfflinePinUnlock(active.grant);
    setSubmitting(true);
    const precheck = await verifyOfflinePin(active.userId, pin);
    if (!precheck.ok) {
      setSubmitting(false);
      if (precheck.reason === "locked") {
        setError(t("offline.pin.locked"));
      } else if (precheck.reason === "invalid_format") {
        setError(t("offline.pin.invalidFormat"));
      } else {
        setError(t("offline.pin.wrong"));
      }
      return;
    }
    const ok = await unlockOfflinePin(pin);
    setSubmitting(false);
    if (!ok) {
      setError(t("offline.pin.wrong"));
      return;
    }
    navigate("/", { replace: true });
  }

  if (profiles.length === 0) {
    return (
      <div
        className="mx-auto flex min-h-[100dvh] w-full max-w-[24rem] flex-col gap-5 px-[max(var(--exits-page-padding),env(safe-area-inset-left))] py-[max(2rem,env(safe-area-inset-top))]"
        data-testid="offline-pin-unlock-page"
        data-session-status={status}
      >
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("offline.pin.grantMissing")}</p>
      </div>
    );
  }

  const activeProfile = selectedProfile ?? profiles[0]!;

  return (
    <div
      className="mx-auto flex min-h-[100dvh] w-full max-w-[24rem] flex-col gap-5 px-[max(var(--exits-page-padding),env(safe-area-inset-left))] py-[max(2rem,env(safe-area-inset-top))]"
      data-testid="offline-pin-unlock-page"
      data-session-status={status}
      data-offline-profile-count={profiles.length}
    >
      <div className="flex items-center gap-3">
        <KeyRound aria-hidden className="size-6 text-primary" />
        <PageHeader
          title={t("offline.pin.unlockTitle")}
          description={t("offline.pin.unlockSubtitle")}
        />
      </div>

      {profiles.length > 1 ? (
        <Card data-testid="offline-pin-identity-picker">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {t("offline.pin.selectIdentityTitle")}
          </p>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("offline.pin.selectIdentityHint")}
          </p>
          <div className="mt-3 flex flex-col gap-2">
            {profiles.map((profile) => (
              <button
                key={profile.userId}
                type="button"
                data-testid={`offline-pin-profile-${profile.userId}`}
                className={
                  profile.userId === activeProfile.userId
                    ? "rounded-[var(--exits-radius-md)] border border-primary bg-primary/5 px-3 py-2 text-left"
                    : "rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-left hover:bg-[var(--exits-surface-muted)]"
                }
                onClick={() => {
                  setSelectedProfile(profile);
                  prepareOfflinePinUnlock(profile.grant);
                  setPin("");
                  setError(null);
                }}
              >
                <span className="block text-[length:var(--exits-text-sm)] font-semibold text-foreground">
                  {profile.displayName}
                </span>
                <span className="block text-[length:var(--exits-text-xs)] text-muted">
                  {profile.organizationDisplayName}
                  {profile.branchName ? ` · ${profile.branchName}` : ""}
                </span>
              </button>
            ))}
          </div>
        </Card>
      ) : (
        <Card data-testid="offline-pin-identity-context">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {activeProfile.displayName}
          </p>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {activeProfile.organizationDisplayName}
          </p>
          {activeProfile.branchName ? (
            <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">{activeProfile.branchName}</p>
          ) : null}
        </Card>
      )}

      <Card>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
          <Input
            label={t("offline.pin.label")}
            inputMode="numeric"
            autoComplete="off"
            type="password"
            value={pin}
            onChange={(event) => setPin(event.target.value.replace(/\D/g, ""))}
            data-testid="offline-pin-unlock-input"
          />
          {error ? (
            <p role="alert" className="m-0 text-[length:var(--exits-text-sm)] text-destructive" data-testid="offline-pin-error">
              {error}
            </p>
          ) : null}
          <Button type="submit" disabled={submitting} data-testid="offline-pin-unlock-submit">
            {t("offline.pin.unlockAction")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
