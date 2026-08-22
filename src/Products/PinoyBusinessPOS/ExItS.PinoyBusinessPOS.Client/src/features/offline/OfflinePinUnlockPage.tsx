import { useState, type FormEvent } from "react";
import { KeyRound } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { verifyOfflinePin } from "@/offline/offline-pin";
import { useSession } from "@/session/SessionProvider";

export function OfflinePinUnlockPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { coldStartGrant, unlockOfflinePin, status } = useSession();
  const [pin, setPin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    const userId = coldStartGrant?.userId;
    if (!userId) {
      setError(t("offline.pin.grantMissing"));
      return;
    }
    setSubmitting(true);
    const precheck = await verifyOfflinePin(userId, pin);
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

  return (
    <div
      className="mx-auto flex min-h-[100dvh] w-full max-w-[24rem] flex-col gap-5 px-[max(var(--exits-page-padding),env(safe-area-inset-left))] py-[max(2rem,env(safe-area-inset-top))]"
      data-testid="offline-pin-unlock-page"
      data-session-status={status}
    >
      <div className="flex items-center gap-3">
        <KeyRound aria-hidden className="size-6 text-primary" />
        <PageHeader
          title={t("offline.pin.unlockTitle")}
          description={t("offline.pin.unlockSubtitle")}
        />
      </div>
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
          {error ? <ErrorState title={t("offline.pin.unlockTitle")} detail={error} /> : null}
          <Button type="submit" disabled={submitting} data-testid="offline-pin-unlock-submit">
            {t("offline.pin.unlockAction")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
