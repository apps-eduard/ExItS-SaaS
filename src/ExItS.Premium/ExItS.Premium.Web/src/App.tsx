import { useEffect } from "react";
import { MotionConfig } from "motion/react";
import { ToastProvider } from "./components/toast/ToastProvider";
import { Playground } from "./playground/Playground";
import { InputMotionGallery } from "./playground/input-motion/InputMotionGallery";
import { SelectPreview } from "./playground/SelectPreview";
import { AppearanceProvider } from "./theme/AppearanceProvider";

function isInputMotionGallery(pathname: string) {
  return pathname.replace(/\/$/, "") === "/input-motion";
}

function isSelectPreview(pathname: string) {
  return pathname.replace(/\/$/, "") === "/select-preview";
}

export function App() {
  const gallery = isInputMotionGallery(window.location.pathname);
  const selectPreview = isSelectPreview(window.location.pathname);

  useEffect(() => {
    if (gallery || selectPreview) return;
    if (window.location.pathname !== "/design-system") {
      window.history.replaceState(null, "", `/design-system${window.location.hash}`);
    }
  }, [gallery, selectPreview]);

  return (
    <AppearanceProvider>
      <MotionConfig reducedMotion="user">
        <ToastProvider>{gallery ? <InputMotionGallery /> : selectPreview ? <SelectPreview /> : <Playground />}</ToastProvider>
      </MotionConfig>
    </AppearanceProvider>
  );
}
