import { useEffect, useState } from "react";
import { platformApiUrl } from "@/api/platform/browser-session";
import { resolveHandoffDestination } from "@/features/auth/session-establish-destination";

export function SessionEstablishPage() {
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const ticket = params.get("ticket")?.trim() ?? "";
    const queryReturnPath = params.get("returnPath");
    window.history.replaceState({}, "", window.location.pathname);
    if (ticket.length === 0) {
      setError("Handoff ticket is missing.");
      return;
    }

    void (async () => {
      const redeem = await fetch(platformApiUrl("/api/v1/platform/auth/web-handoff/redeem"), {
        method: "POST",
        credentials: "include",
        headers: { Accept: "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({ ticket }),
      });
      if (!redeem.ok) {
        setError("This workspace link is invalid or has expired.");
        return;
      }
      const body = (await redeem.json()) as {
        sessionToken?: string;
        returnPath?: string;
        targetApp?: string;
        accountClass?: string;
      };
      const destination = resolveHandoffDestination({
        targetApp: body.targetApp,
        accountClass: body.accountClass,
        queryReturnPath,
        serverReturnPath: body.returnPath,
      });
      if (!destination.ok) {
        setError("This workspace link is for a different account.");
        return;
      }
      const sessionToken = body.sessionToken?.trim() ?? "";
      if (sessionToken.length === 0) {
        setError("This workspace link did not establish a session.");
        return;
      }
      const me = await fetch(platformApiUrl("/api/v1/platform/auth/me"), {
        credentials: "include",
        headers: {
          Accept: "application/json",
          "X-ExItS-Session-Token": sessionToken,
        },
      });
      if (!me.ok) {
        setError("This workspace link did not establish a session.");
        return;
      }
      window.location.replace(destination.path);
    })();
  }, []);

  return (
    <main className="mx-auto max-w-md p-6">
      <h1 className="text-xl font-semibold">Opening workspace</h1>
      {error ? <p role="alert">{error}</p> : <p>Completing sign-in…</p>}
    </main>
  );
}
