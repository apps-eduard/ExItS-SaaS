import { useEffect } from "react";
import { MotionConfig } from "motion/react";
import { ToastProvider } from "./components/toast/ToastProvider";
import { Playground } from "./playground/Playground";
import { AppearanceProvider } from "./theme/AppearanceProvider";

export function App() {
  useEffect(() => {
    if (window.location.pathname !== "/design-system") {
      window.history.replaceState(null, "", `/design-system${window.location.hash}`);
    }
  }, []);

  return (
    <AppearanceProvider>
      <MotionConfig reducedMotion="user">
        <ToastProvider>
          <Playground />
        </ToastProvider>
      </MotionConfig>
    </AppearanceProvider>
  );
}
