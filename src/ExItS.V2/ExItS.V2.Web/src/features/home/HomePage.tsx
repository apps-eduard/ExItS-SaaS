import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { loadPersonalHome, type PersonalHomeData } from "@/api/home-personal-data";
import { toUserFacingError } from "@/api/user-facing-error";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  isOrganizationContextLocked,
  sessionAccountClass,
  V2_PERSONAL_ONLY_HOME,
} from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";
import { PersonalHomeView } from "./PersonalHomeView";

/**
 * V2 Personal-only Home — rebuilt for V1 Personal visual/information parity.
 * BusinessHomeView remains deferred (POS V2 not in scope).
 */
function HomePage() {
  const { t } = useTranslation();
  const { session } = useSession();
  const accountClass = sessionAccountClass(session);
  const staffLocked = isOrganizationContextLocked(session);
  const personalHomeAllowed = accountClass === "Personal" && !staffLocked;

  const [personal, setPersonal] = useState<PersonalHomeData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const softLoadRef = useRef(false);

  useEffect(() => {
    if (!session || !personalHomeAllowed) {
      setLoading(false);
      setPersonal(null);
      setError(null);
      return;
    }

    const controller = new AbortController();
    let active = true;
    const soft = softLoadRef.current;
    if (!soft) setLoading(true);
    setError(null);

    void loadPersonalHome(session, controller.signal)
      .then((result) => {
        if (!active || controller.signal.aborted) return;
        setPersonal(result);
        softLoadRef.current = true;
      })
      .catch((err: unknown) => {
        if (!active || controller.signal.aborted) return;
        setError(toUserFacingError(err, t("homeData.loadError")));
      })
      .finally(() => {
        if (!active || controller.signal.aborted) return;
        setLoading(false);
      });

    return () => {
      active = false;
      controller.abort();
    };
  }, [session, personalHomeAllowed, reloadKey, t]);

  useEffect(() => {
    if (!personalHomeAllowed) return;
    const onVisible = () => {
      if (document.visibilityState === "visible") {
        setReloadKey((value) => value + 1);
      }
    };
    document.addEventListener("visibilitychange", onVisible);
    return () => document.removeEventListener("visibilitychange", onVisible);
  }, [personalHomeAllowed]);

  const retry = () => setReloadKey((value) => value + 1);

  if (!personalHomeAllowed) {
    return (
      <Card className="shadow-none">
        <CardContent className="space-y-2 p-5">
          <h1 className="text-base font-semibold">{t("home.businessDeferredTitle")}</h1>
          <p className="text-sm text-muted-foreground">
            {V2_PERSONAL_ONLY_HOME
              ? t("home.businessDeferredBody")
              : t("homeData.noMetricsBody")}
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-3">
      {error ? (
        <div className="flex flex-wrap items-center gap-2 rounded-xl border border-border bg-card px-3 py-2 text-sm">
          <span className="text-destructive">{error}</span>
          <Button type="button" variant="outline" size="sm" onClick={retry}>
            {t("homeData.retry")}
          </Button>
        </div>
      ) : null}
      <PersonalHomeView
        data={personal}
        loading={loading}
        onRetry={retry}
        onGuideDismissed={retry}
      />
    </div>
  );
}

export { HomePage };
