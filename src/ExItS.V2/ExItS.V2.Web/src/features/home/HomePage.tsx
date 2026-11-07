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
 * V2 Personal-only Home.
 * BusinessHomeView / loadBusinessHome remain in the tree for future POS work
 * but are intentionally not activated here.
 */
function HomePage() {
  const { t } = useTranslation();
  const { session } = useSession();
  const accountClass = sessionAccountClass(session);
  const staffLocked = isOrganizationContextLocked(session);
  const personalHomeAllowed =
    accountClass === "Personal" && !staffLocked;

  const [personal, setPersonal] = useState<PersonalHomeData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [reveal, setReveal] = useState(true);
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
        window.setTimeout(() => setReveal(false), soft ? 0 : 420);
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
        <div className="flex items-center gap-2">
          <p className="text-sm text-destructive" role="alert">
            {error}
          </p>
          <Button type="button" size="sm" variant="outline" onClick={retry}>
            {t("homeData.retry")}
          </Button>
        </div>
      ) : null}

      <PersonalHomeView
        data={personal}
        loading={loading && !personal}
        reveal={reveal}
        onRetry={retry}
        onGuideDismissed={() =>
          setPersonal((prev) => (prev ? { ...prev, showGuide: false } : prev))
        }
      />
    </div>
  );
}

export { HomePage };
