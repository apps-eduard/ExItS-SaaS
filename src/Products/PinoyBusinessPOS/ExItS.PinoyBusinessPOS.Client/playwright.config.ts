import { defineConfig, devices } from "@playwright/test";

const clientRoot = import.meta.dirname;
const prepareProductionEnv =
  process.platform === "win32"
    ? "copy /Y .env.production.example .env.production.local >nul"
    : "cp .env.production.example .env.production.local";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: "list",
  use: {
    baseURL: "http://127.0.0.1:4177",
    trace: "on-first-retry",
  },
  webServer: {
    cwd: clientRoot,
    command: `${prepareProductionEnv} && npm run build && npm run preview`,
    url: "http://127.0.0.1:4177",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
