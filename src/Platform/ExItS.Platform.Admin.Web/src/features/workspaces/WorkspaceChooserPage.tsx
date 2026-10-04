import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { createWebHandoff, listWebWorkspaces } from "@/api/auth/auth-client";
import type { WebWorkspaceItem } from "@/api/auth/auth-types";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { usePreferences } from "@/hooks/use-preferences";
import {
  buildSessionEstablishUrl,
  chooseAutomaticWorkspace,
  planWorkspaceLaunch,
  resolveExperienceOrigins,
} from "@/lib/auth/cutover-routing";
import { env, isLocalValidationToolsEnabled } from "@/lib/env";

function configuredOrigins() {
  const runtime = typeof window === "undefined" ? undefined : window.__EXITS_PLATFORM_ADMIN_WEB__;
  return resolveExperienceOrigins({
    organization: runtime?.organizationWebOrigin,
    personal: runtime?.personalWebOrigin,
    localValidation: isLocalValidationToolsEnabled(),
  });
}

export function WorkspaceChooserPage() {
  const { t } = usePreferences();
  const navigate = useNavigate();
  const [items, setItems] = useState<WebWorkspaceItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const controller = new AbortController();
    void listWebWorkspaces(env.platformApiBaseUrl, controller.signal)
      .then((list) => {
        if (controller.signal.aborted) {
          return;
        }
        setItems(list.workspaces);
        const automatic = chooseAutomaticWorkspace(list.workspaces);
        if (automatic) {
          void openWorkspace(automatic);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setError(t("workspaces.loadError"));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });
    return () => controller.abort();
    // openWorkspace is stable enough for the initial load.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [t]);

  async function openWorkspace(item: WebWorkspaceItem) {
    const plan = planWorkspaceLaunch(item, configuredOrigins());
    if (plan.kind === "platform") {
      navigate(plan.path, { replace: true });
      return;
    }
    if (plan.kind === "origin-missing") {
      setError(t("workspaces.originMissing"));
      return;
    }
    try {
      const created = await createWebHandoff(
        env.platformApiBaseUrl,
        plan.targetApp,
        plan.organizationId,
        plan.returnPath,
      );
      const url = buildSessionEstablishUrl(plan.origin, created.ticket, created.returnPath);
      if (!url) {
        setError(t("workspaces.originMissing"));
        return;
      }
      window.location.assign(url);
    } catch {
      setError(t("workspaces.loadError"));
    }
  }

  return (
    <Card className="border-border">
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t("workspaces.title")}</h1>
      <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">{t("workspaces.hint")}</p>
      {error ? <Alert className="mt-4" tone="danger" title={error} /> : null}
      {loading ? <p className="mt-4 text-muted">{t("workspaces.loading")}</p> : null}
      {!loading && items.length === 0 ? <p className="mt-4">{t("workspaces.empty")}</p> : null}
      <div className="mt-4 grid gap-2">
        {items.map((item) => (
          <Button
            key={`${item.app}-${item.organizationId ?? "none"}`}
            type="button"
            variant="secondary"
            onClick={() => void openWorkspace(item)}
          >
            {item.label}
            {item.roleLabel ? ` · ${item.roleLabel}` : ""}
          </Button>
        ))}
      </div>
    </Card>
  );
}
