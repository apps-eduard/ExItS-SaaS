import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";
import {
  dismissPersonalInstallPrompt,
  peekDeferredInstallPrompt,
  resolvePersonalInstallMode,
  runDeferredInstallPrompt,
  shouldShowAutomaticPersonalInstall,
  shouldShowPersonalMoreInstall,
  startPersonalInstallCapture,
  subscribePersonalInstallCapture,
  type PersonalInstallMode,
} from "@/pwa/personal-install-prompt";

export function PersonalInstallHomeOffer() {
  const location = useLocation();
  const state = usePersonalInstallState();
  const [awaitingBrowser, setAwaitingBrowser] = useState(false);
  if (state.mode === "installed") {
    return null;
  }
  const eligible = shouldShowAutomaticPersonalInstall({
    pathname: location.pathname,
    online: state.online,
    mode: state.mode,
  });
  if (!eligible && !awaitingBrowser) {
    return null;
  }
  return (
    <PersonalInstallCard
      mode={awaitingBrowser ? "installable" : state.mode}
      testId="personal-install-offer"
      onAccepted={() => setAwaitingBrowser(true)}
    />
  );
}

export function PersonalInstallMoreEntry() {
  const { t } = useI18n();
  const state = usePersonalInstallState();
  const [open, setOpen] = useState(false);
  if (!shouldShowPersonalMoreInstall(state.mode)) {
    return null;
  }
  return (
    <section className="catalog-form-section exits-animate-panel personal-section gap-3" data-testid="personal-more-install">
      <h2 className="catalog-form-section__title text-muted">{t("personal.install.more")}</h2>
      {open ? (
        <PersonalInstallCard
          mode={state.mode}
          testId="personal-more-install-panel"
          onFinished={() => setOpen(false)}
        />
      ) : (
        <Button type="button" data-testid="personal-more-install-open" onClick={() => setOpen(true)}>
          {t("personal.install.more")}
        </Button>
      )}
    </section>
  );
}

function PersonalInstallCard({
  mode,
  testId,
  onFinished,
  onAccepted,
}: {
  mode: PersonalInstallMode;
  testId: string;
  onFinished?: () => void;
  onAccepted?: () => void;
}) {
  const { t } = useI18n();
  const [busy, setBusy] = useState(false);
  const [outcome, setOutcome] = useState<"accepted" | "dismissed" | null>(null);
  const [hidden, setHidden] = useState(false);

  if (hidden) {
    return null;
  }

  if (mode === "ios-safari") {
    return (
      <Card className="flex flex-col gap-2 p-3" data-testid={testId}>
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{t("personal.install.iosTitle")}</p>
        <ol className="m-0 list-decimal ps-5 text-[length:var(--exits-text-xs)] text-muted">
          <li>{t("personal.install.iosStep1")}</li>
          <li>{t("personal.install.iosStep2")}</li>
          <li>{t("personal.install.iosStep3")}</li>
        </ol>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            data-testid="personal-install-got-it"
            onClick={() => {
              dismissPersonalInstallPrompt();
              onFinished?.();
              setHidden(true);
            }}
          >
            {t("personal.install.gotIt")}
          </Button>
          <Button
            type="button"
            variant="ghost"
            data-testid="personal-install-later"
            onClick={() => {
              dismissPersonalInstallPrompt();
              onFinished?.();
              setHidden(true);
            }}
          >
            {t("personal.install.later")}
          </Button>
        </div>
      </Card>
    );
  }

  if (mode !== "installable") {
    return (
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted" data-testid="personal-install-unsupported">
        {t("personal.install.unsupported")}
      </p>
    );
  }

  return (
    <Card className="flex flex-col gap-2 p-3" data-testid={testId}>
      <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{t("personal.install.title")}</p>
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("personal.install.detail")}</p>
      {outcome === "accepted" ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted" data-testid="personal-install-accepted">
          {t("personal.install.accepted")}
        </p>
      ) : null}
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          data-testid="personal-install-accept"
          disabled={busy || outcome === "accepted"}
          onClick={() => {
            onAccepted?.();
            setBusy(true);
            return (async () => {
              try {
                const result = await runDeferredInstallPrompt();
                if (result === "accepted") {
                  setOutcome("accepted");
                }
                if (result === "dismissed") {
                  setOutcome("dismissed");
                }
              } finally {
                setBusy(false);
              }
            })();
          }}
        >
          {t("personal.install.accept")}
        </Button>
        <Button
          type="button"
          variant="ghost"
          data-testid="personal-install-later"
          disabled={busy}
          onClick={() => {
            dismissPersonalInstallPrompt();
            onFinished?.();
            setHidden(true);
          }}
        >
          {t("personal.install.later")}
        </Button>
      </div>
    </Card>
  );
}

function usePersonalInstallState() {
  const [revision, setRevision] = useState(0);
  const [online, setOnline] = useState(() =>
    typeof navigator === "undefined" ? true : navigator.onLine,
  );

  useEffect(() => {
    startPersonalInstallCapture();
    return subscribePersonalInstallCapture(() => setRevision((value) => value + 1));
  }, []);

  useEffect(() => {
    function onOnline() {
      setOnline(true);
    }
    function onOffline() {
      setOnline(false);
    }
    window.addEventListener("online", onOnline);
    window.addEventListener("offline", onOffline);
    return () => {
      window.removeEventListener("online", onOnline);
      window.removeEventListener("offline", onOffline);
    };
  }, []);

  void revision;
  void peekDeferredInstallPrompt();
  return {
    online,
    mode: resolvePersonalInstallMode(),
  };
}
