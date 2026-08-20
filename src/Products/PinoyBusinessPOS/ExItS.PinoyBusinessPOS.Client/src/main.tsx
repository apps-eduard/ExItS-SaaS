import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "@/app/App";
import { recoverDevelopmentOriginFromStaleServiceWorker } from "@/pwa/dev-service-worker-guard";
import "@/styles/globals.css";

async function bootstrap() {
  const recovery = await recoverDevelopmentOriginFromStaleServiceWorker();
  if (recovery.willReload) {
    return;
  }

  const rootElement = document.getElementById("root");
  if (!rootElement) {
    throw new Error("Root element #root was not found.");
  }

  createRoot(rootElement).render(
    <StrictMode>
      <App />
    </StrictMode>,
  );
}

void bootstrap();
