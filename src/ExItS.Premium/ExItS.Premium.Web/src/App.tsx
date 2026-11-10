import { useEffect } from "react";
import { MotionConfig } from "motion/react";
import { ToastProvider } from "./components/toast/ToastProvider";
import { Playground } from "./playground/Playground";
import { InputMotionGallery } from "./playground/input-motion/InputMotionGallery";
import { AppearanceProvider } from "./theme/AppearanceProvider";

function isInputMotionGallery(pathname: string) {
  return pathname.replace(/\/$/, "") === "/input-motion";
}

export function App() {
  const gallery = isInputMotionGallery(window.location.pathname);

  useEffect(() => {
    if (gallery) return;
    if (window.location.pathname !== "/design-system") {
      window.history.replaceState(null, "", `/design-system${window.location.hash}`);
    }
  }, [gallery]);

  return (
    <AppearanceProvider>
      <MotionConfig reducedMotion="user">
        <ToastProvider>{gallery ? <InputMotionGallery /> : <Playground />}</ToastProvider>
      </MotionConfig>
    </AppearanceProvider>
  );
}
