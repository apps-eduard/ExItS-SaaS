import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { LoadingState } from "@/components/exits/LoadingState";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { workspaceRouteForOutcome } from "@/workspace/workspace-resolver";

export function SessionLoading() {
  const { t } = useI18n();
  return <LoadingState label={t("session.loading")} />;
}

export function RequireSession({ children }: { children: ReactNode }) {
  const { status } = useSession();
  const location = useLocation();

  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status === "expired") {
    return <Navigate to="/sign-in" replace state={{ expired: true, from: location.pathname }} />;
  }
  if (status !== "authenticated") {
    return <Navigate to="/sign-in" replace state={{ from: location.pathname }} />;
  }
  return children;
}

export function GuestOnly({ children }: { children: ReactNode }) {
  const { status } = useSession();

  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status === "authenticated") {
    return <Navigate to="/" replace />;
  }
  return children;
}

export function RequireWorkspaceBound({ children }: { children: ReactNode }) {
  const { status, boundWorkspace, routingPlan } = useWorkspace();

  if (status === "loading" || status === "binding" || status === "idle") {
    return <SessionLoading />;
  }
  if (boundWorkspace) {
    return children;
  }
  if (routingPlan) {
    return <Navigate to={workspaceRouteForOutcome(routingPlan.outcome)} replace />;
  }
  return <Navigate to="/workspace" replace />;
}

export function WorkspaceBootGate({ children }: { children: ReactNode }) {
  const { status: sessionStatus } = useSession();
  const { status } = useWorkspace();

  if (sessionStatus === "loading") {
    return <SessionLoading />;
  }
  if (sessionStatus === "authenticated" && (status === "loading" || status === "binding")) {
    return <SessionLoading />;
  }
  return children;
}
