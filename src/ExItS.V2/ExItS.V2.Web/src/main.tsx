import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./i18n";
import "./index.css";
import App from "./App";
import {
  applyDocumentAppearance,
  readStoredAppearance,
} from "./theme/appearance";

// Apply (and migrate legacy "system") before React mounts to avoid theme flash.
applyDocumentAppearance(readStoredAppearance());

const root = document.getElementById("root");
if (!root) {
  throw new Error("Root element #root not found");
}

createRoot(root).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
