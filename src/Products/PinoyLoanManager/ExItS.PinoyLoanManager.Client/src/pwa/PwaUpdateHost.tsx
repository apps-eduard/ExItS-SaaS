import { useEffect, useState } from "react";
import { applyPwaUpdateIfAllowed } from "@/pwa/apply-pwa-update";
import { PwaUpdateNotice } from "@/pwa/PwaUpdateNotice";

export function PwaUpdateHost() {
  const [visible, setVisible] = useState(false);
  const [applyUpdate, setApplyUpdate] = useState<(() => void) | null>(null);

  useEffect(() => {
    let cancelled = false;
    void import("virtual:pwa-register").then(({ registerSW }) => {
      if (cancelled) {
        return;
      }
      const updateServiceWorker = registerSW({
        immediate: true,
        onNeedRefresh() {
          setVisible(true);
        },
      });
      setApplyUpdate(() => () => {
        void updateServiceWorker(true);
      });
    });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <PwaUpdateNotice
      visible={visible}
      onRefresh={() => {
        if (applyUpdate) {
          applyPwaUpdateIfAllowed(applyUpdate);
        }
      }}
    />
  );
}
