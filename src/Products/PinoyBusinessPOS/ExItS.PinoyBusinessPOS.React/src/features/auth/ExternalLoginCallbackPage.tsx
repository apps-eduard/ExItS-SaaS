import { useEffect, useState } from "react";
import { platformApiUrl, toBrowserSessionSnapshot, type PlatformLoginWire } from "@/api/platform/browser-session";
import { stripSessionTokenFromLocation } from "@/api/platform/external-auth-flow";
import { clearPersonalConnectIntent } from "@/features/personal/social/personal-connect-intent";
import { landingPathAfterExternalAuth } from "@/features/store/store-acquisition";
import { ensurePersonalSessionProfile } from "@/session/ensure-personal-profile";

export function ExternalLoginCallbackPage() {
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const { sessionToken, nextUrl } = stripSessionTokenFromLocation(
      window.location.pathname,
      window.location.search,
    );
    window.history.replaceState({}, "", nextUrl);
    const landingPath = landingPathAfterExternalAuth(
      nextUrl.includes("?") ? nextUrl.slice(nextUrl.indexOf("?")) : "",
    );
    if (sessionToken.length === 0) {
      setError("Google sign-in did not return a session.");
      return;
    }

    void (async () => {
      const me = await fetch(platformApiUrl("/api/v1/platform/auth/me"), {
        credentials: "include",
        headers: {
          Accept: "application/json",
          "X-ExItS-Session-Token": sessionToken,
        },
      });
      if (!me.ok) {
        setError("Google sign-in could not be completed.");
        return;
      }
      const body = (await me.json()) as PlatformLoginWire;
      const session = toBrowserSessionSnapshot(body);
      const ensured = await ensurePersonalSessionProfile({
        session,
        refreshSession: async () => undefined,
      });
      if (!ensured.ok) {
        setError(ensured.detail);
        return;
      }
      if (landingPath.startsWith("/connect/")) {
        clearPersonalConnectIntent();
      }
      window.location.replace(landingPath);
    })();
  }, []);

  return (
    <main className="mx-auto max-w-md p-6" data-testid="external-login-callback">
      <h1 className="text-xl font-semibold">Continue with Google</h1>
      {error ? <p role="alert">{error}</p> : <p>Completing sign-in…</p>}
    </main>
  );
}
