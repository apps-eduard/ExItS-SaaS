import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

vi.mock("virtual:pwa-register", () => ({
  registerSW: () => async () => undefined,
}));

afterEach(async () => {
  cleanup();
  // Let React Router finish any aborted navigations before the next test stubs fetch.
  await Promise.resolve();
  vi.unstubAllGlobals();
  window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  document.documentElement.dataset.theme = "system";
  document.documentElement.lang = "en";
});
