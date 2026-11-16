import { useEffect } from "react";
import { MotionConfig } from "motion/react";
import { ToastProvider } from "./components/toast/ToastProvider";
import { Playground } from "./playground/Playground";
import { InputMotionGallery } from "./playground/input-motion/InputMotionGallery";
import { DataTablePreviewPage } from "./playground/data-table/DataTablePreviewPage";
import { DATA_TABLE_PREVIEW_PATH } from "./playground/data-table/screen-preview";
import { AppearanceProvider } from "./theme/AppearanceProvider";

function isInputMotionGallery(pathname: string) {
  return pathname.replace(/\/$/, "") === "/input-motion";
}

function isDataTablePreview(pathname: string) {
  return pathname.replace(/\/$/, "") === DATA_TABLE_PREVIEW_PATH;
}

export function App() {
  const gallery = isInputMotionGallery(window.location.pathname);
  const dataTablePreview = isDataTablePreview(window.location.pathname);

  useEffect(() => {
    if (gallery || dataTablePreview) return;
    if (window.location.pathname !== "/design-system") {
      window.history.replaceState(null, "", `/design-system${window.location.hash}`);
    }
  }, [gallery, dataTablePreview]);

  return (
    <AppearanceProvider>
      <MotionConfig reducedMotion="user">
        <ToastProvider>{gallery ? <InputMotionGallery /> : dataTablePreview ? <DataTablePreviewPage /> : <Playground />}</ToastProvider>
      </MotionConfig>
    </AppearanceProvider>
  );
}
