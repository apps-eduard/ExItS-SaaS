import { afterEach, vi } from "vitest";
import { cleanup } from "@testing-library/react";
import "@testing-library/jest-dom/vitest";

vi.mock("virtual:pwa-register", () => ({
  registerSW: () => async () => undefined,
}));

afterEach(() => {
  cleanup();
  window.localStorage.clear();
  document.documentElement.dataset.theme = "system";
  document.documentElement.lang = "en";
});
