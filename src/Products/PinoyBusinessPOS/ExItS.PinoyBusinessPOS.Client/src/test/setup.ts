import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

afterEach(() => {
  cleanup();
  window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  document.documentElement.dataset.theme = "system";
  document.documentElement.lang = "en";
});
