import { useEffect, useRef, useState } from "react";
import { activatePwaUpdate } from "@/pwa/activate-pwa-update";
import { PwaUpdateNotice } from "@/pwa/PwaUpdateNotice";

export const POS_PWA_NEED_REFRESH_EVENT = "exits-pos:pwa-need-refresh";

const UPDATE_CHECK_MS = 60_000;

export function PwaUpdateHost() {
  const [listening, setListening] = useState(false);
  const [updateReady, setUpdateReady] = useState(false);
  const updateRef = useRef<((reloadPage?: boolean) => Promise<void>) | null>(null);
  const pollRef = useRef<number | null>(null);

  useEffect(() => {
    let cancelled = false;
    setListening(true);

    // Never register a service worker during Vite development — a leftover
    // production/preview worker is cleared by recoverDevelopmentOriginFromStaleServiceWorker.
    if (import.meta.env.DEV) {
      return () => {
        cancelled = true;
      };
    }

    const showUpdate = () => setUpdateReady(true);
    window.addEventListener(POS_PWA_NEED_REFRESH_EVENT, showUpdate);

    void import("virtual:pwa-register")
      .then(({ registerSW }) => {
        if (cancelled) {
          return;
        }
        try {
          updateRef.current = registerSW({
            immediate: true,
            onNeedRefresh() {
              setUpdateReady(true);
            },
            onRegisteredSW(_swUrl, registration) {
              if (!registration) {
                return;
              }
              const check = () => {
                void registration.update().catch(() => undefined);
              };
              pollRef.current = window.setInterval(check, UPDATE_CHECK_MS);
            },
            onRegisterError() {
              // Keep the product shell; registration failure is not fatal.
            },
          });
        } catch {
          // App remains usable without an installable worker.
        }
      })
      .catch(() => {
        // virtual:pwa-register unavailable — continue without update prompts.
      });

    return () => {
      cancelled = true;
      window.removeEventListener(POS_PWA_NEED_REFRESH_EVENT, showUpdate);
      if (pollRef.current !== null) {
        window.clearInterval(pollRef.current);
      }
    };
  }, []);

  return (
    <>
      <span hidden data-testid="pwa-update-host" data-ready={listening ? "true" : "false"} />
      <PwaUpdateNotice
        visible={updateReady}
        onRefresh={async () => {
          const apply = updateRef.current;
          const activated = activatePwaUpdate();
          if (apply) {
            await Promise.resolve(apply(true)).catch(() => undefined);
          }
          await activated;
        }}
      />
    </>
  );
}
