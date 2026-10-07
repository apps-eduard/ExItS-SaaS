import { useEffect, useRef, useState, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { Alert } from "@/components/ui/alert";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";
import {
  continueAfterAdminAuthentication,
  isProductAccountClass,
} from "@/lib/auth/continue-after-admin-auth";

export function NonPlatformProductRedirect({ children }: { children?: ReactNode }) {
  const { session } = useSession();
  const { t } = usePreferences();
  const navigate = useNavigate();
  const shouldLeave = isProductAccountClass(session?.accountClass);
  const [message, setMessage] = useState<string | null>(null);
  const started = useRef(false);

  useEffect(() => {
    if (!session || !shouldLeave || started.current) {
      return;
    }
    started.current = true;
    let cancelled = false;
    void continueAfterAdminAuthentication({
      session,
      returnQuery: null,
      navigate: (path) => navigate(path, { replace: true }),
    })
      .then((outcome) => {
        if (!cancelled && outcome === "stay") {
          setMessage(t("workspaces.empty"));
        }
      })
      .catch(() => {
        if (!cancelled) {
          setMessage(t("workspaces.loadError"));
        }
      });
    return () => {
      cancelled = true;
    };
  }, [navigate, session, shouldLeave, t]);

  if (!shouldLeave) {
    return children;
  }
  if (message) {
    return <Alert tone="danger" title={message} />;
  }
  return <p className="text-muted">{t("workspaces.loading")}</p>;
}
