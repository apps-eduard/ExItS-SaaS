import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";
import "@testing-library/jest-dom/vitest";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

if (typeof window.matchMedia !== "function") {
  window.matchMedia = (query: string) =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => true,
    }) as MediaQueryList;
}

if (!navigator.clipboard) {
  Object.assign(navigator, {
    clipboard: {
      writeText: async () => undefined,
    },
  });
}

afterEach(() => {
  cleanup();
  window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  document.documentElement.lang = "en";
  document.documentElement.dataset.theme = "system";
  document.documentElement.dataset.density = "comfortable";
});
