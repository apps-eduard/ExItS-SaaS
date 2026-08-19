import { useRegisterSW } from "virtual:pwa-register/react";
import { PwaUpdateNotice } from "@/pwa/PwaUpdateNotice";

export function PwaUpdateHost() {
  const {
    needRefresh: [needRefresh],
    updateServiceWorker,
  } = useRegisterSW({
    immediate: true,
  });

  return (
    <PwaUpdateNotice
      visible={needRefresh}
      onRefresh={() => {
        void updateServiceWorker();
      }}
    />
  );
}
