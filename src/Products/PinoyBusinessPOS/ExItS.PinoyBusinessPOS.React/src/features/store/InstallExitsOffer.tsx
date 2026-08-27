import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";

type BeforeInstallPromptEvent = Event & {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
};

/**
 * Optional ExItS install offer. Never blocks store access.
 * Uses beforeinstallprompt when the browser supports it.
 */
export function InstallExitsOffer() {
  const { t } = useI18n();
  const [deferred, setDeferred] = useState<BeforeInstallPromptEvent | null>(null);
  const [dismissed, setDismissed] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    function onBeforeInstall(event: Event) {
      event.preventDefault();
      setDeferred(event as BeforeInstallPromptEvent);
    }
    window.addEventListener("beforeinstallprompt", onBeforeInstall);
    return () => window.removeEventListener("beforeinstallprompt", onBeforeInstall);
  }, []);

  if (dismissed) {
    return null;
  }

  if (!deferred) {
    return (
      <p
        className="m-0 text-center text-[length:var(--exits-text-xs)] text-muted"
        data-testid="install-exits-unsupported"
      >
        {t("store.install.continueBrowser")}
      </p>
    );
  }

  return (
    <Card className="flex flex-col gap-2 p-3" data-testid="install-exits-offer">
      <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{t("store.install.title")}</p>
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("store.install.detail")}</p>
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          className="min-h-11"
          data-testid="install-exits-accept"
          disabled={busy}
          onClick={() => {
            void (async () => {
              setBusy(true);
              try {
                await deferred.prompt();
                await deferred.userChoice;
              } finally {
                setDeferred(null);
                setBusy(false);
              }
            })();
          }}
        >
          {t("store.install.accept")}
        </Button>
        <Button
          type="button"
          variant="ghost"
          className="min-h-11"
          data-testid="install-exits-dismiss"
          disabled={busy}
          onClick={() => setDismissed(true)}
        >
          {t("store.install.dismiss")}
        </Button>
      </div>
    </Card>
  );
}
