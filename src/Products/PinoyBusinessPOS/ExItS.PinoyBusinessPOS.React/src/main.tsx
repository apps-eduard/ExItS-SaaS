import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "@/app/App";
import { readPosBuildLabel } from "@/diagnostics/pos-build-info";
import { recoverDevelopmentOriginFromStaleServiceWorker } from "@/pwa/dev-service-worker-guard";
import "@/styles/globals.css";
import "@/styles/personal-commerce.css";

async function bootstrap() {
  const recovery = await recoverDevelopmentOriginFromStaleServiceWorker();
  if (recovery.willReload) {
    return;
  }

  const rootElement = document.getElementById("root");
  if (!rootElement) {
    throw new Error("Root element #root was not found.");
  }

  const buildSha = readPosBuildLabel();
  rootElement.dataset.exitsPosBuildSha = buildSha;
  document.documentElement.dataset.exitsPosBuildSha = buildSha;
  (window as Window & { __EXITS_POS_BUILD_SHA__?: string }).__EXITS_POS_BUILD_SHA__ = buildSha;
  if (import.meta.env.DEV) {
    console.info(`[ExItS POS] build SHA ${buildSha}`);
  }

  createRoot(rootElement).render(
    <StrictMode>
      <App />
    </StrictMode>,
  );
}

void bootstrap();
