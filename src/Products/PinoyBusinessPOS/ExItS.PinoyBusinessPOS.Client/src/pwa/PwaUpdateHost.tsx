import { useEffect, useRef, useState } from "react";
import { applyPwaUpdateIfAllowed, canApplyPwaUpdate } from "@/pwa/apply-pwa-update";
import { PwaUpdateNotice } from "@/pwa/PwaUpdateNotice";

export const POS_PWA_NEED_REFRESH_EVENT = "exits-pos:pwa-need-refresh";

export function PwaUpdateHost() {
  const [visible, setVisible] = useState(false);
  const [listening, setListening] = useState(false);
  const [applyUpdate, setApplyUpdate] = useState<(() => void) | null>(null);
  const applyingRef = useRef(false);

  useEffect(() => {
    let cancelled = false;

    const showNotice = () => {
      if (!cancelled) {
        setVisible(true);
      }
    };

    window.addEventListener(POS_PWA_NEED_REFRESH_EVENT, showNotice);
    setListening(true);

    void import("virtual:pwa-register")
      .then(({ registerSW }) => {
        if (cancelled) {
          return;
        }
        try {
          const updateServiceWorker = registerSW({
            immediate: true,
            onNeedRefresh: showNotice,
            onRegisterError() {
              // Keep the product shell; registration failure is not fatal.
            },
          });
          setApplyUpdate(() => () => {
            void updateServiceWorker(true);
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
      window.removeEventListener(POS_PWA_NEED_REFRESH_EVENT, showNotice);
    };
  }, []);

  return (
    <>
      <span hidden data-testid="pwa-update-host" data-ready={listening ? "true" : "false"} />
      <PwaUpdateNotice
        visible={visible}
        onRefresh={() => {
          if (applyingRef.current) {
            return;
          }
          applyingRef.current = true;
          if (!applyUpdate) {
            applyingRef.current = false;
            return;
          }
          const applied = applyPwaUpdateIfAllowed(applyUpdate, canApplyPwaUpdate);
          if (!applied) {
            applyingRef.current = false;
          }
        }}
        guard={canApplyPwaUpdate}
      />
    </>
  );
}
