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

  // Non-secret build label for runtime/bundle verification (e2e + support).
  // Window global is DEV-only; dataset remains available in all modes.
  const buildSha = readPosBuildLabel();
  rootElement.dataset.exitsPosBuildSha = buildSha;
  document.documentElement.dataset.exitsPosBuildSha = buildSha;
  if (import.meta.env.DEV) {
    (window as Window & { __EXITS_POS_BUILD_SHA__?: string }).__EXITS_POS_BUILD_SHA__ = buildSha;
    console.info(`[ExItS POS] build ${buildSha}`);
  }

  createRoot(rootElement).render(
    <StrictMode>
      <App />
    </StrictMode>,
  );
}

void bootstrap();
