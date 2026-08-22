import { useState, type FormEvent } from "react";
import { LockKeyhole } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { enrollOfflinePinAndDek } from "@/offline/local-store-key";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OfflinePinEnrollPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { session } = useSession();
  const { refreshWorkspaces } = useWorkspace();
  const [pin, setPin] = useState("");
  const [confirmPin, setConfirmPin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const userId = session?.userId;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    if (!userId) {
      setError(t("offline.pin.notSignedIn"));
      return;
    }
    if (pin !== confirmPin) {
      setError(t("offline.pin.confirmMismatch"));
      return;
    }
    setSubmitting(true);
    const ok = await enrollOfflinePinAndDek(userId, pin);
    setSubmitting(false);
    if (!ok) {
      setError(t("offline.pin.invalidFormat"));
      return;
    }
    await refreshWorkspaces();
    navigate("/", { replace: true });
  }

  return (
    <div
      className="mx-auto flex min-h-[100dvh] w-full max-w-[24rem] flex-col gap-5 px-[max(var(--exits-page-padding),env(safe-area-inset-left))] py-[max(2rem,env(safe-area-inset-top))]"
      data-testid="offline-pin-setup-page"
    >
      <div className="flex items-center gap-3">
        <LockKeyhole aria-hidden className="size-6 text-primary" />
        <PageHeader
          title={t("offline.pin.enrollTitle")}
          description={t("offline.pin.enrollMessage")}
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
            data-testid="offline-pin-enroll-input"
          />
          <Input
            label={t("offline.pin.confirmLabel")}
            inputMode="numeric"
            autoComplete="off"
            type="password"
            value={confirmPin}
            onChange={(event) => setConfirmPin(event.target.value.replace(/\D/g, ""))}
            data-testid="offline-pin-enroll-confirm"
          />
          {error ? <ErrorState title={t("offline.pin.enrollTitle")} detail={error} /> : null}
          <Button type="submit" disabled={submitting} data-testid="offline-pin-enroll-submit">
            {t("offline.pin.enrollAction")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
