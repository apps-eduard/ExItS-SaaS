import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./i18n";
import "./index.css";
import "./styles/v1-personal/v1-personal-home-extract.css";
import "./styles/v1-personal/personal-tracker-fix.css";
import "./styles/v1-personal/personal-stores-to-pay-fix.css";
import "./styles/v1-personal/personal-quick-actions-fix.css";
import "./styles/v1-personal/personal-todo-fix.css";
import "./styles/v1-personal/personal-tracker-metric-toggle.css";
import "./styles/v1-personal/personal-needs-attention-fix.css";
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
