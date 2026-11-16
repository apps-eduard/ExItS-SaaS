import { useEffect } from "react";
import { MotionConfig } from "motion/react";
import { AuthExperience } from "./auth/AuthExperience";
import { SessionProvider, useSession } from "./auth/SessionProvider";
import { AUTH_PREVIEW_PATH, accountHome, appSurface } from "./auth/model";
import { ToastProvider } from "./components/toast/ToastProvider";
import { PersonalHomeScreen } from "./personal/PersonalHome";
import { WorkspaceUnavailable } from "./personal/WorkspaceUnavailable";
import { Playground } from "./playground/Playground";
import { InputMotionGallery } from "./playground/input-motion/InputMotionGallery";
import { AppearanceProvider } from "./theme/AppearanceProvider";

function AccountRoutes() {
  const surface = appSurface(window.location.pathname);
  const { status, session, reload, logout } = useSession();
  const destination = session ? accountHome(session.accountClass) : null;
  const guarded = surface === "personal-home" || surface === "workspace";

  useEffect(() => {
    if (surface !== "design-system") return;
    if (window.location.pathname !== "/design-system") {
      window.history.replaceState(null, "", `/design-system${window.location.hash}`);
    }
  }, [surface]);

  useEffect(() => {
    if (status === "anonymous" && guarded) {
      window.location.replace(AUTH_PREVIEW_PATH);
      return;
    }
    if (status !== "authenticated" || !destination) return;
    if (surface === "authentication" || (guarded && window.location.pathname.replace(/\/$/, "") !== destination)) {
      window.location.replace(destination);
    }
  }, [surface, status, destination, guarded]);

  if (guarded && status === "loading") return <p role="status">Checking your session.</p>;
  if (guarded && status === "unreachable") {
    return (
      <p role="alert">
        The account service is unreachable. <button type="button" onClick={() => void reload()}>Try again</button>
      </p>
    );
  }
  if (guarded && status !== "authenticated") return <p role="status">Sending you to sign in.</p>;
  if (guarded && destination && window.location.pathname.replace(/\/$/, "") !== destination) {
    return <p role="status">Opening the right workspace.</p>;
  }

  return (
    <>
      {surface === "input-motion" ? <InputMotionGallery /> : null}
      {surface === "authentication" ? <AuthExperience frame="page" live /> : null}
      {surface === "personal-home" && session ? <PersonalHomeScreen /> : null}
      {surface === "workspace" && session ? (
        <WorkspaceUnavailable
          session={session}
          onLogout={() => {
            void logout().finally(() => window.location.replace(AUTH_PREVIEW_PATH));
          }}
        />
      ) : null}
      {surface === "design-system" ? <Playground /> : null}
    </>
  );
}

export function App() {
  return (
    <AppearanceProvider>
      <SessionProvider>
        <MotionConfig reducedMotion="user">
          <ToastProvider>
            <AccountRoutes />
          </ToastProvider>
        </MotionConfig>
      </SessionProvider>
    </AppearanceProvider>
  );
}
